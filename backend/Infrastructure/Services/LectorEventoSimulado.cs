using System.Globalization;
using System.Text.RegularExpressions;
using Breaku.Application.Eventos;
using Breaku.Domain.Rules;

namespace Breaku.Infrastructure.Services;

/// <summary>
/// Lector sin red para pruebas y desarrollo local (no se registra en la app). Del texto lee líneas «Título:», «Lugar:»,
/// «Fecha: dd/MM/yyyy», «Hora: HH:mm-HH:mm». Una imagen no se interpreta: responde que no es un evento.
/// </summary>
public class LectorEventoSimulado : ILectorEventoFlyer
{
    public const string NombreModelo = "simulado";

    public Task<LecturaEvento> LeerAsync(SolicitudLecturaEvento solicitud, CancellationToken ct = default)
    {
        var texto = solicitud.Texto ?? "";
        string? Campo(string nombre) => Regex.Match(texto, $@"(?:^|\s){nombre}:\s*(.+?)(?=\s+(?:Título|Lugar|Fecha|Hora|Descripción):|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)) is { Success: true } m ? m.Groups[1].Value.Trim() : null;

        var titulo = Campo("Título");
        var hora = Campo("Hora");
        var h = hora is null ? null : Regex.Match(hora, @"^(\d{1,2}:\d{2})\s*-\s*(\d{1,2}:\d{2})$");

        // HU34: «Fecha:» puede traer varias fechas del mismo evento («03/12/2026, 05/12/2026 y 07/12/2026»).
        var sesiones = new List<SesionLeida>();
        foreach (var fecha in (Campo("Fecha") ?? "").Split([",", " y "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (h is not { Success: true } ||
                !DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia)) continue;
            sesiones.Add(new SesionLeida(
                $"{dia:yyyy-MM-dd}T{TimeOnly.Parse(h.Groups[1].Value, CultureInfo.InvariantCulture):HH:mm}",
                $"{dia:yyyy-MM-dd}T{TimeOnly.Parse(h.Groups[2].Value, CultureInfo.InvariantCulture):HH:mm}"));
        }
        var primera = sesiones.FirstOrDefault();

        var evento = titulo is null
            ? new EventoLeido(false, true, null, null, null, null, null, null, null, null, false, [], [])
            : new EventoLeido(true, true, titulo, Campo("Descripción"), primera?.Inicio, primera?.Fin, Campo("Lugar"), null, null, null, false, [], sesiones);
        return Task.FromResult(new LecturaEvento(NombreModelo, "{}", evento));
    }
}
