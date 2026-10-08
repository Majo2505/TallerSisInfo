namespace Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs.Auth;
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
}