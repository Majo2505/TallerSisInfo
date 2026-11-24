using System.Diagnostics;
using System.Text.Json;
using Breaku.Application.Actividades;
using Breaku.Application.Horarios;
using Breaku.Application.Notificaciones;
using Breaku.Application.Recomendaciones;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Breaku.Application.Eventos;

/// <summary>HU24: lee un flyer (JPG, PNG, PDF) o un texto pegado y devuelve un borrador de evento. No crea el evento (HU23).</summary>
public class ExtraccionEventoService
{
    public const long BytesMaximos = 8 * 1024 * 1024;
    public const int PaginasMaximas = 2;
    public const int LadoMaximoPx = 1600;
    public const int CalidadJpeg = 85;
    public const int BytesMaximosImagenEvento = 2 * 1024 * 1024;   // ck_evimg_bytes

    /// <summary>Si la página 1 no cabe en 2 MB, se vuelve a renderizar con menos resolución y calidad.</summary>
    public static readonly (int Lado, int Calidad)[] Reducciones = [(1200, 75), (900, 65), (700, 55)];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILectorEventoFlyer _lector;
    private readonly IConvertidorPdf _pdf;
    private readonly IExtraccionRepository _repo;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ExtraccionEventoService>? _logger;
    private readonly IAvisosAdmin? _avisos;

    public ExtraccionEventoService(ILectorEventoFlyer lector, IConvertidorPdf pdf, IExtraccionRepository repo,
        TimeProvider? reloj = null, ILogger<ExtraccionEventoService>? logger = null, IAvisosAdmin? avisos = null)
    {
        _avisos = avisos;
        _lector = lector;
        _pdf = pdf;
        _repo = repo;
        _reloj = reloj ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<RespuestaExtraccionEventoDto> ExtraerAsync(int usuarioId, byte[]? archivo, string? texto, CancellationToken ct = default)
    {
        var hayArchivo = archivo is { Length: > 0 };
        var hayTexto = !string.IsNullOrWhiteSpace(texto);
        if (hayArchivo && hayTexto)
            throw new ReglaNegocioException("VALIDACION", "Envía el afiche o el texto, no los dos a la vez.");
        if (!hayArchivo && !hayTexto)
            throw new ReglaNegocioException("VALIDACION", "Adjunta el afiche en el campo «archivo» o pega el texto del evento en «texto».");

        var ahoraUtc = _reloj.GetUtcNow().UtcDateTime;
        var ahoraLocal = ReglasNotificacion.AHoraLocal(ahoraUtc);

        string? textoLimpio = null;
        var imagenes = new List<ImagenFlyer>();
        byte[]? paginaUno = null;
        string origen;

        if (hayTexto)
        {
            textoLimpio = LimpiarTexto(texto!);
            origen = "TEXTO";
            var rechazo = ReglasSeguridadIa.Evaluar(textoLimpio);
            if (rechazo?.Categoria is ReglasSeguridadIa.CategoriaInyeccion or ReglasSeguridadIa.CategoriaFueraDeAlcance)
            {
                var log = NuevoLog(usuarioId, textoLimpio, ahoraUtc);
                log.Estado = LogAuditoriaRag.EstadoRechazadoSeguridad;
                log.MotivoRechazo = rechazo.Categoria;
                await _repo.GuardarLogAsync(log, ct);
                // HU32: texto escrito por el estudiante: cuenta para la alerta al admin.
                if (_avisos is not null) await _avisos.IntentoBloqueadoAsync(usuarioId, ct);
                throw new ReglaNegocioException(RecomendacionService.CodigoSolicitudNoPermitida, rechazo.Mensaje);
            }
        }
        else
        {
            var bytes = archivo!;
            if (bytes.Length > BytesMaximos) throw new ReglaNegocioException("VALIDACION", "El archivo no puede superar los 8 MB.");
            switch (DetectarFormato(bytes))
            {
                case Formato.Jpeg: imagenes.Add(new ImagenFlyer(bytes, "image/jpeg")); origen = "JPG"; break;
                case Formato.Png: imagenes.Add(new ImagenFlyer(bytes, "image/png")); origen = "PNG"; break;
                case Formato.Pdf:
                    origen = "PDF";
                    var paginas = _pdf.Renderizar(bytes, PaginasMaximas, LadoMaximoPx, CalidadJpeg);
                    imagenes.AddRange(paginas.Jpeg.Select(p => new ImagenFlyer(p, "image/jpeg")));
                    paginaUno = paginas.Jpeg.FirstOrDefault();
                    if (imagenes.Count == 0) throw new ReglaNegocioException("PDF_INVALIDO", "No pudimos leer el PDF. Prueba con otro archivo.");
                    break;
                default:
                    throw new ReglaNegocioException("ARCHIVO_NO_ADMITIDO", "El archivo tiene que ser JPG, PNG o PDF.");
            }
        }

        var instrucciones = PromptFlyer.Construir(ahoraLocal);
        var registro = NuevoLog(usuarioId, textoLimpio, ahoraUtc);
        registro.PromptEnviado = instrucciones;

        var reloj = Stopwatch.StartNew();
        LecturaEvento lectura;
        try
        {
            lectura = await _lector.LeerAsync(new SolicitudLecturaEvento(instrucciones, imagenes, textoLimpio), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger?.LogWarning(ex, "No se pudo leer el afiche: {Motivo}", ex.Message);
            registro.Estado = LogAuditoriaRag.EstadoError;
            registro.LatenciaMs = (int)reloj.ElapsedMilliseconds;
            await _repo.GuardarLogAsync(registro, ct);
            if (ex is ReglaNegocioException regla) throw new ReglaNegocioException(regla.Codigo, regla.Message);
            throw new ReglaNegocioException("LECTOR_NO_DISPONIBLE",
                "No pudimos leer el afiche en este momento. Inténtalo de nuevo en unos minutos o llena el formulario a mano.");
        }

        registro.Modelo = lectura.Modelo.Length > 40 ? lectura.Modelo[..40] : lectura.Modelo;
        registro.RespuestaLlm = lectura.TextoCrudo;
        registro.LatenciaMs = (int)reloj.ElapsedMilliseconds;

        var resultado = ReglasFlyer.Interpretar(lectura.Evento, ahoraLocal);
        var dto = ADto(resultado);

        registro.Estado = resultado.Estado switch
        {
            ReglasExtraccion.EstadoCompleto => LogAuditoriaRag.EstadoOk,
            ReglasExtraccion.EstadoIncompleto => LogAuditoriaRag.EstadoIncompleto,
            _ => LogAuditoriaRag.EstadoSinResultados
        };
        registro.DatosExtraidos = JsonSerializer.Serialize(new
        {
            origen,
            evento = dto.Evento,
            faltantes = dto.Faltantes,
            advertencias = dto.Advertencias,
            sesiones = dto.Sesiones
        }, Json);
        await _repo.GuardarLogAsync(registro, ct);

        // Solo un PDF aporta imagen: la foto (JPG/PNG) ya la tiene el estudiante en su dispositivo.
        var imagen = paginaUno is null ? null : PrepararImagen(archivo!, paginaUno);
        return dto with { ConsultaId = registro.Id, Imagen = imagen };
    }

    private ImagenEventoDto? PrepararImagen(byte[] pdf, byte[] paginaUno)
    {
        if (paginaUno.Length <= BytesMaximosImagenEvento) return ComoImagen(paginaUno);
        foreach (var (lado, calidad) in Reducciones)
        {
            try
            {
                var menor = _pdf.Renderizar(pdf, PaginasMaximas, lado, calidad).Jpeg.FirstOrDefault();
                if (menor is { Length: > 0 } && menor.Length <= BytesMaximosImagenEvento) return ComoImagen(menor);
            }
            catch (ReglaNegocioException) { return null; }
        }
        return null;
    }

    private static ImagenEventoDto ComoImagen(byte[] jpeg) => new("image/jpeg", Convert.ToBase64String(jpeg), jpeg.Length);

    private static LogAuditoriaRag NuevoLog(int usuarioId, string? entrada, DateTime ahoraUtc) => new()
    {
        UsuarioId = usuarioId,
        Tipo = LogAuditoriaRag.TipoExtraccionEvento,
        EntradaUsuario = entrada,
        CreatedAt = ahoraUtc
    };

    private static string LimpiarTexto(string texto)
    {
        var limpio = texto.Trim();
        if (limpio.Length > ReglasFlyer.MaximoLargoTextoPegado)
            throw new ReglaNegocioException("VALIDACION", $"El texto puede tener como máximo {ReglasFlyer.MaximoLargoTextoPegado} caracteres.");
        var saneado = ReglasSeguridadIa.Sanear(limpio);
        return saneado.Length == 0 ? throw new ReglaNegocioException("VALIDACION", "Pega el texto del evento.") : saneado;
    }

    private enum Formato { Desconocido, Jpeg, Png, Pdf }

    /// <summary>La firma real del archivo; el ContentType que declara el navegador no cuenta.</summary>
    private static Formato DetectarFormato(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return Formato.Jpeg;
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return Formato.Png;
        if (b.Length >= 5 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46 && b[4] == 0x2D) return Formato.Pdf;
        return Formato.Desconocido;
    }

    private static string? Formatear(DateTime? fecha) => fecha?.ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);

    private static RespuestaExtraccionEventoDto ADto(ResultadoFlyer r) => new(0, r.Estado, r.Mensaje,
        new BorradorEventoDto(r.Evento.Titulo, r.Evento.Descripcion,
            Formatear(r.Evento.Inicio), Formatear(r.Evento.Fin),
            r.Evento.LugarTexto, r.Evento.Asistencia, r.Evento.VisitaMin, r.Evento.VisitaMax),
        r.Faltantes.ToList(), r.Advertencias.ToList(),
        (r.Sesiones ?? []).Select(s => new SesionEventoDto(Formatear(s.Inicio)!, Formatear(s.Fin))).ToList(),
        null);
}
