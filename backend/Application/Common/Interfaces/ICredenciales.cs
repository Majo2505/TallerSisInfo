using Breaku.Domain.Rules;

namespace Breaku.Application.Common.Interfaces;

/// <summary>Cifra y verifica contraseñas (lo implementa Infrastructure con BCrypt).</summary>
public interface IHasherContrasenas
{
    string Cifrar(string contrasena);

    /// <summary>False si no coincide o si la cuenta no tiene contraseña (entra solo con Google).</summary>
    bool Verificar(string contrasena, string? hash);
}

/// <summary>Verifica el token que Google le da al frontend (firma, emisor, audiencia = nuestro Client ID y vencimiento).</summary>
public interface IVerificadorGoogle
{
    /// <summary>False si falta <c>GOOGLE_CLIENT_ID</c>: el inicio con Google está apagado.</summary>
    bool Configurado { get; }

    /// <summary>Null si el token no es válido.</summary>
    Task<ReglasCuenta.IdentidadGoogle?> VerificarAsync(string credencial, CancellationToken ct = default);
}
