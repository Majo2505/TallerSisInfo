using Breaku.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Breaku.Infrastructure.Services;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly string? _clientId;

    public GoogleTokenValidator(IConfiguration configuration)
        => _clientId = configuration["GOOGLE_CLIENT_ID"]?.Trim();

    public bool Configurado => !string.IsNullOrEmpty(_clientId);

    public async Task<IdentidadGoogle?> ValidarAsync(string idToken, CancellationToken ct = default)
    {
        if (!Configurado) return null;

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId!] });
            return new IdentidadGoogle(payload.Email ?? string.Empty, payload.Name ?? string.Empty, payload.EmailVerified);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
