using System.Security.Claims;
using Breaku.Api.Hubs;
using Breaku.Application.Comunidades;
using Breaku.Application.Notificaciones;
using Breaku.Application.TiempoReal;
using Breaku.Domain;

namespace Breaku.Tests;

/// <summary>HU18: quién recibe qué en vivo, para que la app se actualice sin recargar.</summary>
public class TiempoRealTests
{
    // ---------- a quién le llegan las comunidades nuevas de una sede

    private static Usuario Estudiante(bool visible = true, bool conZona = true, bool zonaActiva = true) => new()
    {
        Id = 7, Nombre = "Estudiante Dev", Activo = true, VisibleMatching = visible,
        ZonaActualId = conZona ? 2 : null,
        ZonaActual = conZona ? new Zona { Id = 2, Nombre = "Campus UCB Cochabamba", Activa = zonaActiva } : null
    };

    private static Task<int?> Zona(Usuario? usuario)
        => new DescubrirComunidadesService(new SoloUsuario(usuario)).ZonaParaSeguirAsync(7);

    [Fact]
    public async Task Quien_ve_las_comunidades_de_otros_sigue_su_sede()
        => Assert.Equal(2, await Zona(Estudiante()));

    [Fact]
    public async Task Sin_matching_no_sigue_ninguna_sede()
        => Assert.Null(await Zona(Estudiante(visible: false)));

    [Fact]
    public async Task Sin_sede_o_con_la_sede_apagada_no_sigue_ninguna()
    {
        Assert.Null(await Zona(Estudiante(conZona: false)));
        Assert.Null(await Zona(Estudiante(zonaActiva: false)));
        Assert.Null(await Zona(null));
    }

    // ---------- avisos de puente: llegan a la campanita al momento

    [Fact]
    public async Task Los_avisos_de_puente_nuevos_se_mandan_en_vivo_a_sus_estudiantes()
    {
        var repo = new Avisos
        {
            PorAvisar =
            [
                new Puente { UsuarioId = 3, DiaSemana = 1, HoraInicio = new TimeOnly(10, 0), HoraFin = new TimeOnly(12, 0), Tipo = Puente.TipoEntreClases },
                new Puente { UsuarioId = 5, DiaSemana = 1, HoraInicio = new TimeOnly(10, 0), HoraFin = new TimeOnly(11, 0), Tipo = Puente.TipoEntreClases }
            ],
            YaAvisados = [5]
        };
        var envivo = new TiempoRealFalso();

        var enviados = await new NotificacionService(repo, envivo).EnviarAvisosAsync(new DateTime(2026, 11, 2, 14, 30, 0, DateTimeKind.Utc));

        Assert.Equal(1, enviados);
        Assert.Equal([3], Assert.Single(envivo.Avisos));
    }

    // ---------- el id del estudiante para Clients.User

    [Theory]
    [InlineData(ClaimTypes.NameIdentifier, "7", 7)]
    [InlineData("sub", "12", 12)]
    [InlineData("sub", "no-es-un-numero", null)]
    public void El_id_sale_del_token(string tipo, string valor, int? esperado)
        => Assert.Equal(esperado, UsuarioIdProvider.Leer(new ClaimsPrincipal(new ClaimsIdentity([new Claim(tipo, valor)], "jwt"))));

    [Fact]
    public void Sin_sesion_no_hay_id()
        => Assert.Null(UsuarioIdProvider.Leer(null));

    // ---------- falsos

    private sealed class TiempoRealFalso : ITiempoReal
    {
        public List<int[]> Avisos { get; } = [];

        public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default)
        {
            Avisos.Add([.. usuarioIds]);
            return Task.CompletedTask;
        }

        public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SoloUsuario(Usuario? usuario) : IDescubrirComunidadesRepository
    {
        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(usuario);

        public Task<List<ComunidadActiva>> ListarActivasAsync(int usuarioId, int? deOtrosEnZona, DateTime ahoraUtc, DateTime hastaUtc, int maximo,
            CancellationToken ct = default) => throw new NotSupportedException();

        public Task<bool> PuedeVerAsync(int usuarioId, int comunidadId, int? deOtrosEnZona, DateTime ahoraUtc, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class Avisos : INotificacionRepository
    {
        public List<Puente> PorAvisar { get; init; } = [];
        public List<int> YaAvisados { get; init; } = [];

        public Task<List<Puente>> ListarPuentesPorAvisarAsync(int diaSemana, DateOnly fecha, TimeOnly ahora, CancellationToken ct = default)
            => Task.FromResult(PorAvisar);

        // Un aviso repetido no se guarda (uq_notif_puente): no hay nada nuevo que mandar.
        public Task<bool> AgregarAsync(Notificacion notificacion, CancellationToken ct = default)
            => Task.FromResult(!YaAvisados.Contains(notificacion.UsuarioId));

        public Task<List<Notificacion>> ListarActivasAsync(int usuarioId, int maximo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<Notificacion>> ListarActivasDePuenteAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DesactivarAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> MarcarLeidaAsync(long id, int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool?> ObtenerPreferenciaAsync(int usuarioId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task GuardarPreferenciaAsync(int usuarioId, bool activas, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
