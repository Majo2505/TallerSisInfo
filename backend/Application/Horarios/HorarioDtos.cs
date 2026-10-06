namespace Breaku.Application.Horarios;

/// <summary>Horas en formato "HH:mm".</summary>
public record GuardarHorarioRequest(string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula);

public record HorarioDto(int Id, string Materia, int DiaSemana, string HoraInicio, string HoraFin, string? Aula, string Origen);

/// <summary>Error de negocio con un código que la Api traduce a HTTP.</summary>
public class ReglaNegocioException : Exception
{
    public string Codigo { get; }   // VALIDACION | SOLAPAMIENTO | NO_ENCONTRADO
    public ReglaNegocioException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}
