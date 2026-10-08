namespace Breaku.Application.Puentes;

public record PuenteDto(
    int Id, 
    int DiaSemana, 
    string HoraInicio, 
    string HoraFin
);