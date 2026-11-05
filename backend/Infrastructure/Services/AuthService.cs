using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs.Auth;
using Breaku.Domain;
using Breaku.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace Breaku.Infrastructure.Services;   
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IGoogleTokenValidator _google;

    public AuthService(AppDbContext context, IConfiguration configuration, IGoogleTokenValidator google)
    {
        _context = context;
        _configuration = configuration;
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

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

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
        if (usuario == null || usuario.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

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

    public async Task<AuthResponseDto> LoginConGoogleAsync(GoogleLoginRequestDto request, CancellationToken ct = default)
    {
        if (!_google.Configurado)
            throw new InvalidOperationException("El acceso con Google todavía no está disponible.");
        if (string.IsNullOrWhiteSpace(request.IdToken))
            throw new ArgumentException("No recibimos la credencial de Google.");

        var identidad = await _google.ValidarAsync(request.IdToken, ct)
            ?? throw new UnauthorizedAccessException("No pudimos verificar tu cuenta de Google.");
        if (!identidad.CorreoVerificado)
            throw new UnauthorizedAccessException("Tu correo de Google no está verificado.");

        var correo = identidad.Email.Trim().ToLowerInvariant();
        ValidarCorreoInstitucional(correo);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == correo, ct);
        if (usuario is null)
        {
            usuario = new Usuario
            {
                Nombre = string.IsNullOrWhiteSpace(identidad.Nombre) ? correo[..correo.IndexOf('@')] : identidad.Nombre.Trim(),
                Email = correo,
                Rol = "ESTUDIANTE",
                Activo = true,
                CorreoVerificado = true
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync(ct);
        }
        else if (!usuario.Activo)
        {
            throw new UnauthorizedAccessException("Tu cuenta está desactivada.");
        }
        else if (!usuario.CorreoVerificado)
        {
            usuario.CorreoVerificado = true;
            await _context.SaveChangesAsync(ct);
        }

        return new AuthResponseDto
        {
            Token = GenerarJwtToken(usuario),
            UserId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }

    private static void ValidarCorreoInstitucional(string? correo)
    {
        if (!ReglasCorreoInstitucional.EsInstitucional(correo))
            throw new ArgumentException(ReglasCorreoInstitucional.MensajeNoInstitucional);
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