namespace Breaku.Application.Puentes;

/// <summary>Puente detectado: hueco libre de 45 minutos o más entre dos clases del mismo día.</summary>
/// <param name="Id" example="5">Identificador del puente.</param>
/// <param name="DiaSemana" example="1">Día de la semana: 1 = lunes … 6 = sábado.</param>
/// <param name="HoraInicio" example="09:30:00">Hora en que termina la clase anterior (<c>HH:mm:ss</c>, 24 h).</param>
/// <param name="HoraFin" example="10:30:00">Hora en que empieza la clase siguiente (<c>HH:mm:ss</c>, 24 h).</param>
public record PuenteDto(
    int Id,
    int DiaSemana,
    string HoraInicio,
    string HoraFin
);
