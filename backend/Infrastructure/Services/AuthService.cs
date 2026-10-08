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

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
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
            Rol = "Estudiante",
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
            Email = usuario.Email
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        var token = GenerarJwtToken(usuario);

        return new AuthResponseDto
        {
            Token = token,
            UserId = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email
        };
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
            new Claim("nombre", usuario.Nombre)
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