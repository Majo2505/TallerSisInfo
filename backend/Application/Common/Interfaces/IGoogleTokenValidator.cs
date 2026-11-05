namespace Breaku.Application.Common.Interfaces;

public sealed record IdentidadGoogle(string Email, string Nombre, bool CorreoVerificado);

public interface IGoogleTokenValidator
{
    bool Configurado { get; }

    Task<IdentidadGoogle?> ValidarAsync(string idToken, CancellationToken ct = default);
}
