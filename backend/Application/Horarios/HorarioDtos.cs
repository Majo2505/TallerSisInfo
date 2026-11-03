namespace Breaku.Application.Horarios;

public record GuardarHorarioRequest(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, bool? Asiste = null, bool? AutorizacionCarrera = null);

public record CoincidenciaDto(int Id, string Materia, string HoraInicio, string HoraFin, bool Asiste);

public record CambiarAsistenciaRequest(bool Asiste, bool? AutorizacionCarrera = null);

public record AsistenciaBloqueRequest(int Id, bool Asiste, bool? AutorizacionCarrera = null);

public record CambiarAsistenciasRequest(List<AsistenciaBloqueRequest> Bloques);

public record HorarioDto(
    int Id, string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, string Origen,
    bool Asiste, bool AutorizacionCarrera, List<CoincidenciaDto> Coincide, List<string> Advertencias);

public class ReglaNegocioException : Exception
{
    public string Codigo { get; }
    public ReglaNegocioException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}
