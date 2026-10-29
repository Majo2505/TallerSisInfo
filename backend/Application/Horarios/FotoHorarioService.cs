using Breaku.Domain.Rules;

namespace Breaku.Application.Horarios;

/// <summary>
/// Registro del horario con una foto (HU2): valida el archivo, se lo pasa al lector (modelo de visión) y filtra
/// lo que devuelve con las mismas reglas de un alta manual. No guarda: el estudiante revisa y confirma con
/// <see cref="HorarioService.CrearLoteAsync"/>.
/// </summary>
public class FotoHorarioService
{
    public const long BytesMaximos = 8 * 1024 * 1024;   // 8 MB
    public static readonly string[] TiposPermitidos = ["image/jpeg", "image/png", "image/webp"];
    private const int LargoMaximoAula = 30;             // horario.aula es VARCHAR(30)
    private static readonly string[] Formatos = ["HH:mm", "HH:mm:ss", "H:mm"];

    // Pausas que algunos horarios imprimen como si fueran una fila más: nunca son clases.
    private static readonly string[] Pausas = ["receso", "recreo", "descanso", "almuerzo", "break", "intermedio"];

    private static bool EsPausa(string materia)
    {
        var texto = materia.Trim().ToLowerInvariant();
        return Pausas.Any(p => texto == p || texto.StartsWith(p + " ") || texto.StartsWith(p + ":"));
    }

    private readonly ILectorHorarioFoto _lector;
    public FotoHorarioService(ILectorHorarioFoto lector) => _lector = lector;

    public async Task<LecturaFotoHorarioDto> LeerAsync(byte[] imagen, string? tipoMime, CancellationToken ct = default)
    {
        if (imagen.Length == 0) throw new ReglaNegocioException("VALIDACION", "La foto está vacía.");
        if (imagen.Length > BytesMaximos) throw new ReglaNegocioException("VALIDACION", "La foto no puede superar los 8 MB.");
        var tipo = tipoMime?.ToLowerInvariant() ?? "";
        if (!TiposPermitidos.Contains(tipo))
            throw new ReglaNegocioException("VALIDACION", "La foto tiene que ser JPG, PNG o WebP.");

        var leidas = await _lector.LeerAsync(imagen, tipo, ct);

        var validas = new List<ClaseLeidaDto>();
        foreach (var c in leidas)
        {
            if (!TimeOnly.TryParseExact(c.HoraInicio?.Trim(), Formatos, out var ini) ||
                !TimeOnly.TryParseExact(c.HoraFin?.Trim(), Formatos, out var fin)) continue;
            if (ReglasHorario.ValidarBloque(c.Materia, c.DiaSemana, ini, fin) is not null) continue;
            if (EsPausa(c.Materia)) continue;

            var materia = c.Materia.Trim();
            var aula = string.IsNullOrWhiteSpace(c.Aula) ? null : c.Aula.Trim();
            if (aula is { Length: > LargoMaximoAula }) aula = aula[..LargoMaximoAula];

            var clase = new ClaseLeidaDto(materia, c.DiaSemana, ini.ToString("HH:mm"), fin.ToString("HH:mm"), aula);
            if (validas.Any(v => v.DiaSemana == clase.DiaSemana && v.HoraInicio == clase.HoraInicio &&
                                 string.Equals(v.Materia, clase.Materia, StringComparison.OrdinalIgnoreCase))) continue;
            validas.Add(clase);
        }

        var ordenadas = validas.OrderBy(c => c.DiaSemana).ThenBy(c => c.HoraInicio, StringComparer.Ordinal).ToList();
        return new LecturaFotoHorarioDto(ordenadas, leidas.Count - ordenadas.Count);
    }
}
