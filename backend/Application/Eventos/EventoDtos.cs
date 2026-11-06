namespace Breaku.Application.Eventos;

public sealed record EventoRecomendadoDto(
    long Id, string Titulo, string Descripcion, string Inicio, string Fin, string? Lugar, string FuenteUrl);

public sealed record EventoImportado(
    string FuenteUrl, string Titulo, string Descripcion, DateTime Inicio, DateTime Fin, string? Lugar);
