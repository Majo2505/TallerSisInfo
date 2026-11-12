namespace Breaku.Application.DTOs.Auth;

/// <param name="Credencial">El ID token que Google Identity Services le da al frontend (campo <c>credential</c>).</param>
public record LoginGoogleRequest(string? Credencial);
