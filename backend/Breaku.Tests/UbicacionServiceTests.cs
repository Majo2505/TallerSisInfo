using Breaku.Application.Ubicacion;
using Breaku.Domain;

namespace Breaku.Tests;

public sealed class UbicacionServiceTests
{
    private sealed class RepositorioFalso : IUbicacionRepository
    {
        public List<Zona> Zonas { get; } =
        [
            new Zona { Id = 3, Nombre = "Campus central", Latitud = -17.37m, Longitud = -66.16m, RadioMaxM = 2000 },
            new Zona { Id = 7, Nombre = "Sede norte", Latitud = -16.5m, Longitud = -68.1m, RadioMaxM = 2000 },
        ];
        public Usuario Usuario { get; } = new() { Id = 7 };
        public int VecesGuardado { get; private set; }

        public Task<List<Zona>> ListarZonasAsync(CancellationToken ct = default) => Task.FromResult(Zonas.ToList());

        public Task<Zona?> ObtenerZonaAsync(int zonaId, CancellationToken ct = default)
            => Task.FromResult(Zonas.FirstOrDefault(z => z.Id == zonaId));

        public Task<Usuario?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
            => Task.FromResult<Usuario?>(usuarioId == Usuario.Id ? Usuario : null);

        public Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            VecesGuardado++;
            return Task.CompletedTask;
        }
    }

    private static (UbicacionService Servicio, RepositorioFalso Repo) CrearServicio()
    {
        var repo = new RepositorioFalso();
        return (new UbicacionService(repo), repo);
    }

    [Fact]
    public async Task Compartir_la_ubicacion_guarda_solo_la_zona_y_el_consentimiento()
    {
        var (servicio, repo) = CrearServicio();

        var estado = await servicio.CompartirAsync(7, new CompartirUbicacionRequest(-17.37m, -66.16m));

        Assert.True(estado.ConsentimientoActivo);
        Assert.Equal(3, estado.ZonaActual?.Id);
        Assert.Equal(3, repo.Usuario.ZonaActualId);
        Assert.True(repo.Usuario.ConsentimientoUbicacion);
        Assert.NotNull(repo.Usuario.FechaConsentimiento);
        Assert.Equal(1, repo.VecesGuardado);
    }

    [Fact]
    public async Task Una_ubicacion_fuera_de_las_zonas_no_guarda_nada_y_deja_elegir_a_mano()
    {
        var (servicio, repo) = CrearServicio();

        var error = await Assert.ThrowsAsync<UbicacionFueraDeZonaException>(
            () => servicio.CompartirAsync(7, new CompartirUbicacionRequest(0m, 0m)));

        Assert.Contains("seleccionar tu campus manualmente", error.Message);
        Assert.Null(repo.Usuario.ZonaActualId);
        Assert.False(repo.Usuario.ConsentimientoUbicacion);
        Assert.Equal(0, repo.VecesGuardado);
    }

    [Fact]
    public async Task Elegir_el_campus_a_mano_guarda_la_zona_sin_consentimiento_de_ubicacion()
    {
        var (servicio, repo) = CrearServicio();

        var estado = await servicio.SeleccionarZonaAsync(7, 7);

        Assert.False(estado.ConsentimientoActivo);
        Assert.Equal(7, estado.ZonaActual?.Id);
        Assert.False(repo.Usuario.ConsentimientoUbicacion);
        Assert.Null(repo.Usuario.FechaConsentimiento);
        Assert.Equal(1, repo.VecesGuardado);
    }

    [Fact]
    public async Task Revocar_borra_la_zona_y_el_consentimiento_en_cualquier_momento()
    {
        var (servicio, repo) = CrearServicio();
        await servicio.CompartirAsync(7, new CompartirUbicacionRequest(-17.37m, -66.16m));

        await servicio.RevocarAsync(7);

        Assert.Null(repo.Usuario.ZonaActualId);
        Assert.False(repo.Usuario.ConsentimientoUbicacion);
        Assert.Null(repo.Usuario.FechaConsentimiento);
        Assert.Equal(2, repo.VecesGuardado);
        var estado = await servicio.ObtenerEstadoAsync(7);
        Assert.False(estado.ConsentimientoActivo);
        Assert.Null(estado.ZonaActual);
    }
}
