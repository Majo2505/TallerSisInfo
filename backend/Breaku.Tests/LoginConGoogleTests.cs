using Breaku.Application.Common.Interfaces;
using Breaku.Application.DTOs.Auth;
using Breaku.Domain;
using Breaku.Infrastructure.Persistence;
using Breaku.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Breaku.Tests;

public class LoginConGoogleTests
{
    private sealed class GoogleFalso(IdentidadGoogle? identidad, bool configurado = true) : IGoogleTokenValidator
    {
        public bool Configurado => configurado;

        public Task<IdentidadGoogle?> ValidarAsync(string idToken, CancellationToken ct = default) => Task.FromResult(identidad);
    }

    private static (AuthService Servicio, AppDbContext Db) Crear(IdentidadGoogle? identidad, bool configurado = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SECRET"] = new string('k', 40) }).Build();
        return (new AuthService(db, config, new GoogleFalso(identidad, configurado)), db);
    }

    private static GoogleLoginRequestDto Pedido(string token = "token") => new() { IdToken = token };

    [Fact]
    public async Task Un_estudiante_de_la_universidad_entra_y_se_crea_su_cuenta()
    {
        var (servicio, db) = Crear(new IdentidadGoogle("Ana.Perez@ucb.edu.bo", "Ana Pérez", true));

        var respuesta = await servicio.LoginConGoogleAsync(Pedido());

        Assert.False(string.IsNullOrEmpty(respuesta.Token));
        Assert.Equal("ana.perez@ucb.edu.bo", respuesta.Email);
        Assert.Equal("ESTUDIANTE", respuesta.Rol);
        var usuario = await db.Usuarios.SingleAsync();
        Assert.Null(usuario.PasswordHash);
        Assert.True(usuario.CorreoVerificado);
        Assert.Equal("Ana Pérez", usuario.Nombre);
    }

    [Fact]
    public async Task Si_la_cuenta_ya_existe_entra_a_esa_misma_cuenta()
    {
        var (servicio, db) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", true));
        db.Usuarios.Add(new Usuario { Nombre = "Ana Registrada", Email = "ana@ucb.edu.bo", PasswordHash = "hash", Rol = "ESTUDIANTE", Activo = true });
        await db.SaveChangesAsync();

        var respuesta = await servicio.LoginConGoogleAsync(Pedido());

        Assert.Equal("Ana Registrada", respuesta.Nombre);
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    [Theory]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@ucb.edu.bo.falso.com")]
    public async Task Un_correo_de_otro_dominio_se_rechaza_con_el_mensaje_institucional(string correo)
    {
        var (servicio, db) = Crear(new IdentidadGoogle(correo, "Ana", true));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => servicio.LoginConGoogleAsync(Pedido()));

        Assert.Equal(ReglasCorreoInstitucional.MensajeNoInstitucional, ex.Message);
        Assert.Empty(db.Usuarios);
    }

    [Fact]
    public async Task Un_correo_de_Google_sin_verificar_se_rechaza()
    {
        var (servicio, db) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", false));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.LoginConGoogleAsync(Pedido()));
        Assert.Empty(db.Usuarios);
    }

    [Fact]
    public async Task Un_token_que_Google_no_valida_se_rechaza()
    {
        var (servicio, _) = Crear(null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.LoginConGoogleAsync(Pedido()));
    }

    [Fact]
    public async Task Una_cuenta_desactivada_no_entra()
    {
        var (servicio, db) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", true));
        db.Usuarios.Add(new Usuario { Nombre = "Ana", Email = "ana@ucb.edu.bo", Rol = "ESTUDIANTE", Activo = false });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.LoginConGoogleAsync(Pedido()));
    }

    [Fact]
    public async Task Sin_configurar_Google_el_servicio_no_esta_disponible()
    {
        var (servicio, _) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", true), configurado: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.LoginConGoogleAsync(Pedido()));
    }

    [Fact]
    public async Task Sin_credencial_se_pide_enviarla()
    {
        var (servicio, _) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", true));

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.LoginConGoogleAsync(Pedido("  ")));
    }

    [Fact]
    public async Task Quien_entro_con_Google_no_puede_entrar_con_contrasena_vacia()
    {
        var (servicio, db) = Crear(new IdentidadGoogle("ana@ucb.edu.bo", "Ana", true));
        await servicio.LoginConGoogleAsync(Pedido());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => servicio.LoginAsync(new LoginRequestDto { Email = "ana@ucb.edu.bo", Password = "cualquiera" }));
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }
}
