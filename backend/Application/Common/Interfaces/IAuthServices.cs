namespace Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs.Auth;
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>Entra (o se registra) con el token de Google. Ver <c>ReglasCuenta.DecidirGoogle</c>.</summary>
    Task<AuthResponseDto> LoginConGoogleAsync(string? credencial, CancellationToken ct = default);
}