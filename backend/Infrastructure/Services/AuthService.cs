using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs.Auth;
using Breaku.Domain;
using Breaku.Domain.Rules;
using Breaku.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace Breaku.Infrastructure.Services;   
public class AuthService : IAuthService
{
    private const string DominioInstitucional = "@ucb.edu.bo";

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHasherContrasenas _hasher;
    private readonly IVerificadorGoogle _google;

    public AuthService(AppDbContext context, IConfiguration configuration, IHasherContrasenas hasher, IVerificadorGoogle google)
    {
        _context = context;
        _configuration = configuration;
        _hasher = hasher;
        _google = google;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        ValidarCorreoInstitucional(request.Email);

        var existe = await _context.Usuarios.AnyAsync(u => u.Email == request.Email);
        if (existe)
        {
            throw new InvalidOperationException("El correo electrónico ya está registrado.");
        }

        var error = ReglasCuenta.ValidarNueva(request.Password, request.Password);
        if (error is not null) throw new ArgumentException(error);
        var passwordHash = _hasher.Cifrar(request.Password);

        var usuario = new Usuario
        {
            Nombre = request.Nombre,
            Email = request.Email,
            PasswordHash = passwordHash,
            Rol = "ESTUDIANTE",
            Activo = true,
            CorreoVerificado = true
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var token = GenerarJwtToken(usuario);

        return new AuthResponseDto
        {
            Token = token,
            UserId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        ValidarCorreoInstitucional(request.Email);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
        // Sin contraseña (entra solo con Google) o desactivada: mismo mensaje, para no revelar qué cuentas existen.
        // Antes, una cuenta sin contraseña hacía fallar BCrypt y el login respondía 500.
        if (usuario == null || !usuario.Activo || !_hasher.Verificar(request.Password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        return Respuesta(usuario);
    }

    public async Task<AuthResponseDto> LoginConGoogleAsync(string? credencial, CancellationToken ct = default)
    {
        if (!_google.Configurado)
            throw new InvalidOperationException("El inicio de sesión con Google no está configurado.");

        var identidad = await _google.VerificarAsync(credencial ?? "", ct)
                        ?? throw new UnauthorizedAccessException("No pudimos verificar tu cuenta de Google. Inténtalo de nuevo.");

        for (var intento = 0; ; intento++)
        {
            var porSub = await _context.Usuarios.FirstOrDefaultAsync(u => u.GoogleSub == identidad.Sub, ct);
            var porCorreo = porSub is null ? await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == identidad.Correo, ct) : null;
            var decision = ReglasCuenta.DecidirGoogle(identidad, Cuenta(porSub), Cuenta(porCorreo));

            Usuario usuario;
            switch (decision.Accion)
            {
                case ReglasCuenta.Accion.Rechazar:
                    throw new ArgumentException(decision.Mensaje);
                case ReglasCuenta.Accion.Entrar:
                    usuario = porSub!;
                    break;
                case ReglasCuenta.Accion.Enlazar:
                    usuario = porCorreo!;
                    usuario.GoogleSub = identidad.Sub;
                    usuario.CorreoVerificado = true;
                    usuario.UpdatedAt = DateTime.UtcNow;
                    break;
                default:
                    usuario = new Usuario
                    {
                        Nombre = ReglasCuenta.NombreParaCuentaNueva(identidad),
                        Email = identidad.Correo.Trim(),
                        GoogleSub = identidad.Sub,
                        PasswordHash = null,
                        Rol = "ESTUDIANTE",
                        Activo = true,
                        CorreoVerificado = true
                    };
                    _context.Usuarios.Add(usuario);
                    break;
            }

            try
            {
                await _context.SaveChangesAsync(ct);
                return Respuesta(usuario);
            }
            catch (DbUpdateException) when (intento == 0)
            {
                // Dos pestañas a la vez: la otra ya creó o enlazó la cuenta. Se vuelve a decidir con lo guardado.
                _context.ChangeTracker.Clear();
            }
        }
    }

    private static ReglasCuenta.CuentaExistente? Cuenta(Usuario? u)
        => u is null ? null : new ReglasCuenta.CuentaExistente(u.Id, u.Rol, u.Activo, u.GoogleSub);

    private AuthResponseDto Respuesta(Usuario usuario) => new()
    {
        Token = GenerarJwtToken(usuario),
        UserId = usuario.Id,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol
    };

    private static void ValidarCorreoInstitucional(string? correo)
    {
        var valor = correo?.Trim() ?? string.Empty;
        var usuario = valor.Length > DominioInstitucional.Length
            ? valor[..^DominioInstitucional.Length]
            : string.Empty;

        if (usuario.Length == 0 || usuario.Contains('@')
            || !valor.EndsWith(DominioInstitucional, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Solo se permite el acceso con un correo institucional (@ucb.edu.bo)");
        }
    }

    private string GenerarJwtToken(Usuario usuario)
    {
        var jwtSecret = _configuration["JWT_SECRET"] 
            ?? throw new InvalidOperationException("Falta la configuración 'JWT_SECRET'.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("nombre", usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT_ISSUER"] ?? "BreakuApi",
            audience: _configuration["JWT_AUDIENCE"] ?? "BreakuApp",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}