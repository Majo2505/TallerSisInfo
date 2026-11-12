using Breaku.Application.Common.Interfaces;
using Breaku.Domain.Rules;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;

namespace Breaku.Infrastructure.Services;

public sealed class HasherBCrypt : IHasherContrasenas
{
    public string Cifrar(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena);

    public bool Verificar(string contrasena, string? hash)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(contrasena)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(contrasena, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

/// <summary>
/// Verifica el «ID token» de Google Identity Services con las claves públicas de Google (las descarga y las cachea la
/// librería oficial). Solo necesita el Client ID: este flujo no usa client secret.
/// </summary>
public sealed class VerificadorGoogle : IVerificadorGoogle
{
    private readonly string? _clientId;
    private readonly ILogger<VerificadorGoogle> _logger;

    public VerificadorGoogle(string? clientId, ILogger<VerificadorGoogle> logger)
    {
        _clientId = string.IsNullOrWhiteSpace(clientId) ? null : clientId.Trim();
        _logger = logger;
    }

    public bool Configurado => _clientId is not null;

    public async Task<ReglasCuenta.IdentidadGoogle?> VerificarAsync(string credencial, CancellationToken ct = default)
    {
        if (_clientId is null || string.IsNullOrWhiteSpace(credencial)) return null;
        try
        {
            var datos = await GoogleJsonWebSignature.ValidateAsync(credencial,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_clientId] });
            return new ReglasCuenta.IdentidadGoogle(datos.Subject, datos.Email ?? "", datos.EmailVerified, datos.Name);
        }
        // Un token mal formado no lanza InvalidJwtException sino FormatException o JsonException: también es «no válido».
        catch (Exception ex) when (ex is InvalidJwtException or FormatException or ArgumentException or System.Text.Json.JsonException
                                       or Newtonsoft.Json.JsonException)
        {
            _logger.LogInformation("Token de Google rechazado: {Motivo}", ex.Message);
            return null;
        }
    }
}
