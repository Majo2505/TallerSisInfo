using Breaku.Application.Common.Interfaces;
using Breaku.Application.Horarios;
using Breaku.Application.Perfil;
using Breaku.Domain;
using Breaku.Domain.Rules;
using static Breaku.Domain.Rules.ReglasCuenta;

namespace Breaku.Tests;

/// <summary>Cambiar la contraseña desde el perfil y entrar con Google.</summary>
public class CuentaTests
{
    // ---------- reglas de la contraseña nueva

    [Theory]
    [InlineData("breaku2026", null)]
    [InlineData("Ñandú-2026", null)]
    [InlineData("corta1", "La contraseña tiene que tener al menos 8 caracteres.")]
    [InlineData("soloLetras", "La contraseña tiene que tener al menos una letra y un número.")]
    [InlineData("12345678", "La contraseña tiene que tener al menos una letra y un número.")]
    [InlineData(" breaku2026", "La contraseña no puede empezar ni terminar con espacios.")]
    [InlineData("", "Escribe la nueva contraseña.")]
    public void La_contrasena_nueva_tiene_reglas(string nueva, string? error)
        => Assert.Equal(error, ValidarNueva(nueva, nueva));

    [Fact]
    public void La_confirmacion_tiene_que_coincidir()
        => Assert.Equal(MensajeNoCoinciden, ValidarNueva("breaku2026", "breaku2027"));

    [Fact]
    public void BCrypt_solo_usa_72_bytes_asi_que_no_se_acepta_mas()
    {
        Assert.Null(ValidarNueva("a1" + new string('x', 70), "a1" + new string('x', 70)));
        Assert.NotNull(ValidarNueva("a1" + new string('x', 71), "a1" + new string('x', 71)));
        Assert.NotNull(ValidarNueva("a1" + new string('ñ', 36), "a1" + new string('ñ', 36))); // 74 bytes: la ñ ocupa 2
    }

    // ---------- quién entra con Google

    private static IdentidadGoogle G(string correo = "ana.quiroga@ucb.edu.bo", bool verificado = true, string sub = "g-ana")
        => new(sub, correo, verificado, "Ana Quiroga");

    private static CuentaExistente C(int id = 3, string rol = "ESTUDIANTE", bool activa = true, string? sub = null)
        => new(id, rol, activa, sub);

    [Fact]
    public void Sin_cuenta_se_crea_una_de_estudiante()
        => Assert.Equal(Accion.Crear, DecidirGoogle(G(), null, null).Accion);

    [Fact]
    public void Con_el_mismo_correo_se_enlaza_a_la_cuenta_que_ya_tenia()
        => Assert.Equal(new Decision(Accion.Enlazar, 3), DecidirGoogle(G(), null, C()));

    [Fact]
    public void Ya_enlazada_entra_directo()
        => Assert.Equal(new Decision(Accion.Entrar, 3), DecidirGoogle(G(), C(sub: "g-ana"), null));

    [Theory]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@ucb.edu.bo.evil.com")]
    [InlineData("ana@est.ucb.edu.bo")]
    [InlineData("a@b@ucb.edu.bo")]
    public void Solo_cuentas_institucionales(string correo)
        => Assert.Equal(MensajeSoloInstitucional, DecidirGoogle(G(correo), null, null).Mensaje);

    [Fact]
    public void El_correo_tiene_que_estar_verificado_por_google()
        => Assert.Equal(MensajeCorreoSinVerificar, DecidirGoogle(G(verificado: false), null, null).Mensaje);

    [Fact]
    public void El_administrador_no_entra_con_google()
    {
        Assert.Equal(MensajeAdmin, DecidirGoogle(G("dev.admin@ucb.edu.bo"), null, C(2, "ADMIN")).Mensaje);
        Assert.Equal(MensajeAdmin, DecidirGoogle(G(), C(2, "ADMIN", sub: "g-ana"), null).Mensaje);
    }

    [Fact]
    public void Una_cuenta_desactivada_no_entra()
        => Assert.Equal(MensajeInactiva, DecidirGoogle(G(), null, C(activa: false)).Mensaje);

    [Fact]
    public void No_se_roba_una_cuenta_ya_enlazada_a_otro_google()
        => Assert.Equal(MensajeOtraCuentaGoogle, DecidirGoogle(G(sub: "g-otro"), null, C(sub: "g-ana")).Mensaje);

    [Fact]
    public void El_nombre_de_la_cuenta_nueva_sale_de_google_o_del_correo()
    {
        Assert.Equal("Ana Quiroga", NombreParaCuentaNueva(G()));
        Assert.Equal("ana.quiroga", NombreParaCuentaNueva(new IdentidadGoogle("s", "ana.quiroga@ucb.edu.bo", true, null)));
    }

    // ---------- cambiar la contraseña desde el perfil

    private static (PerfilService, Repo) Armar(string? hash = "h:Breaku-Dev-2026!", string? googleSub = null)
    {
        var repo = new Repo
        {
            Usuario = new Usuario { Id = 7, Nombre = "Mateo", Email = "mateo@ucb.edu.bo", Rol = "ESTUDIANTE", Activo = true, PasswordHash = hash, GoogleSub = googleSub }
        };
        return (new PerfilService(repo, new HasherFalso()), repo);
    }

    [Fact]
    public async Task Con_la_actual_correcta_se_cambia_y_queda_en_el_historial_sin_la_contrasena()
    {
        var (servicio, repo) = Armar();

        await servicio.CambiarContrasenaAsync(7, new CambiarContrasenaRequest("Breaku-Dev-2026!", "nueva2026", "nueva2026"));

        Assert.Equal("h:nueva2026", repo.Usuario!.PasswordHash);
        var (antes, despues) = Assert.Single(repo.Historial);
        Assert.DoesNotContain("nueva2026", antes + despues);
        Assert.Contains("cambiada", despues);
    }

    [Fact]
    public async Task Con_la_actual_incorrecta_no_se_cambia()
    {
        var (servicio, repo) = Armar();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CambiarContrasenaAsync(7, new CambiarContrasenaRequest("otra", "nueva2026", "nueva2026")));

        Assert.Equal("CONTRASENA_INCORRECTA", ex.Codigo);
        Assert.Equal("h:Breaku-Dev-2026!", repo.Usuario!.PasswordHash);
        Assert.Empty(repo.Historial);
    }

    [Fact]
    public async Task Sin_la_actual_no_se_cambia()
    {
        var (servicio, _) = Armar();

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CambiarContrasenaAsync(7, new CambiarContrasenaRequest(null, "nueva2026", "nueva2026")));

        Assert.Equal(MensajeFaltaActual, ex.Message);
    }

    [Fact]
    public async Task La_nueva_no_puede_ser_la_misma()
    {
        var (servicio, _) = Armar(hash: "h:breaku2026");

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.CambiarContrasenaAsync(7, new CambiarContrasenaRequest("breaku2026", "breaku2026", "breaku2026")));

        Assert.Equal(MensajeIgualALaActual, ex.Message);
    }

    [Fact]
    public async Task Quien_entra_solo_con_google_crea_su_contrasena_sin_la_actual()
    {
        var (servicio, repo) = Armar(hash: null, googleSub: "g-mateo");

        await servicio.CambiarContrasenaAsync(7, new CambiarContrasenaRequest(null, "nueva2026", "nueva2026"));

        Assert.Equal("h:nueva2026", repo.Usuario!.PasswordHash);
        Assert.Contains("creada", Assert.Single(repo.Historial).Despues);
    }

    [Fact]
    public async Task El_perfil_dice_si_tiene_contrasena_y_si_esta_enlazado_con_google()
    {
        var (soloGoogle, _) = Armar(hash: null, googleSub: "g-mateo");
        var (ambas, _) = Armar(googleSub: "g-mateo");

        var a = await soloGoogle.ObtenerAsync(7);
        var b = await ambas.ObtenerAsync(7);

        Assert.False(a.TieneContrasena);
        Assert.True(a.ConGoogle);
        Assert.True(b.TieneContrasena);
    }

    private sealed class HasherFalso : IHasherContrasenas
    {
        public string Cifrar(string contrasena) => "h:" + contrasena;
        public bool Verificar(string contrasena, string? hash) => hash is not null && hash == "h:" + contrasena;
    }

    private sealed class Repo : IPerfilRepository
    {
        public Usuario? Usuario { get; set; }
        public List<(string Antes, string Despues)> Historial { get; } = [];

        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult(Usuario?.Id == usuarioId ? Usuario : null);

        public Task GuardarConHistorialAsync(int usuarioId, string valoresAnteriores, string valoresNuevos, CancellationToken ct = default)
        {
            Historial.Add((valoresAnteriores, valoresNuevos));
            return Task.CompletedTask;
        }
    }
}
