using Breaku.Application.Admin;
using Breaku.Application.Horarios;
using Breaku.Application.Reportes;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

/// <summary>Catálogo de lugares: qué se puede guardar.</summary>
public class ReglasLugarTests
{
    // Campus UCB Cochabamba (centro y radio de 500 m).
    private static readonly CampusRef Cochabamba = new("Campus UCB Cochabamba", -17.3716m, -66.1439m, 500);

    private static DatosLugar Lugar(string? nombre = "Biblioteca Central", decimal? lat = -17.3716m, decimal? lng = -66.1439m,
        int? duracion = 30, TimeOnly? abre = null, TimeOnly? cierra = null, int? aPie = 4, string? descripcion = "Sala de lectura")
        => new(nombre, descripcion, 2, lat, lng, duracion, abre, cierra, aPie);

    private static string? Validar(DatosLugar d) => ReglasLugar.Validar(d, [Cochabamba]);

    [Fact]
    public void Un_lugar_completo_dentro_del_campus_es_valido()
        => Assert.Null(Validar(Lugar(abre: new TimeOnly(7, 30), cierra: new TimeOnly(20, 0))));

    [Fact]
    public void Sin_horario_tambien_es_valido() => Assert.Null(Validar(Lugar()));

    [Theory]
    [InlineData("ab")]
    [InlineData("   ")]
    [InlineData(null)]
    public void El_nombre_es_obligatorio_y_de_3_caracteres_o_mas(string? nombre)
        => Assert.Equal(ReglasLugar.MensajeNombre, Validar(Lugar(nombre: nombre)));

    [Fact]
    public void El_nombre_se_limpia_de_espacios_de_mas()
        => Assert.Equal("Biblioteca Central", ReglasLugar.LimpiarNombre("  Biblioteca    Central "));

    [Fact]
    public void Un_lugar_a_mas_de_500_metros_del_campus_no_se_acepta()
        => Assert.Equal(ReglasLugar.MensajeFueraDelCampus, Validar(Lugar(lat: -17.3666m)));   // ~550 m al norte

    [Fact]
    public void Sin_coordenadas_no_se_acepta() => Assert.Equal(ReglasLugar.MensajeUbicacion, Validar(Lugar(lat: null)));

    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    [InlineData(null)]
    public void La_duracion_va_de_1_a_240_minutos(int? duracion)
        => Assert.Equal(ReglasLugar.MensajeDuracion, Validar(Lugar(duracion: duracion)));

    [Fact]
    public void El_horario_va_completo_y_cierra_despues_de_abrir()
    {
        Assert.Equal(ReglasLugar.MensajeHorario, Validar(Lugar(abre: new TimeOnly(8, 0))));
        Assert.Equal(ReglasLugar.MensajeHorario, Validar(Lugar(abre: new TimeOnly(18, 0), cierra: new TimeOnly(8, 0))));
    }

    [Fact]
    public void Los_minutos_a_pie_van_de_0_a_60() => Assert.Equal(ReglasLugar.MensajeMinutosAPie, Validar(Lugar(aPie: 61)));

    [Fact]
    public void La_descripcion_tiene_hasta_500_caracteres()
        => Assert.Equal(ReglasLugar.MensajeDescripcion, Validar(Lugar(descripcion: new string('a', 501))));
}

/// <summary>Reportes del estudiante: qué se puede enviar.</summary>
public class ReglasReporteNuevoTests
{
    [Theory]
    [InlineData("ERROR_APP", null, "La app se cierra al abrir el horario")]
    [InlineData("sugerencia", null, "Estaría bueno ver los puentes en el calendario")]
    [InlineData("LUGAR_INCORRECTO", 4, "El horario dice que abre a las 7, pero abre a las 9")]
    public void Un_reporte_bien_armado_es_valido(string motivo, int? lugar, string descripcion)
        => Assert.Null(ReglasReporte.ValidarNuevo(motivo, lugar, descripcion));

    [Theory]
    [InlineData("OTRA_COSA", null, "Una descripción bastante larga", ReglasReporte.MensajeMotivo)]
    [InlineData("COMUNIDAD_INAPROPIADA", null, "Una descripción bastante larga", ReglasReporte.MensajeMotivo)]
    [InlineData("LUGAR_INCORRECTO", null, "Una descripción bastante larga", ReglasReporte.MensajeFaltaLugar)]
    [InlineData("ERROR_APP", 4, "Una descripción bastante larga", ReglasReporte.MensajeLugarDeMas)]
    [InlineData("ERROR_APP", null, "  corto   ", ReglasReporte.MensajeDescripcionCorta)]
    public void Un_reporte_mal_armado_dice_que_falta(string motivo, int? lugar, string descripcion, string mensaje)
        => Assert.Equal(mensaje, ReglasReporte.ValidarNuevo(motivo, lugar, descripcion));

    [Fact]
    public void La_descripcion_tiene_hasta_500_caracteres()
        => Assert.Equal(ReglasReporte.MensajeDescripcionLarga, ReglasReporte.ValidarNuevo("SUGERENCIA", null, new string('a', 501)));
}

/// <summary>Casos de uso del catálogo (admin) con un repositorio falso.</summary>
public class CatalogoAdminServiceTests
{
    private sealed class RepositorioFalso : IAdminRepository
    {
        public HashSet<string> Nombres { get; } = ["Biblioteca Central"];
        public HashSet<int> EnUso { get; } = [1];
        public HashSet<int> Existentes { get; } = [1, 2];
        public List<(string Accion, DatosLugar? Datos)> Llamadas { get; } = [];

        public Task<List<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken ct = default)
            => Task.FromResult(new List<CategoriaAdminDto> { new(1, "Comida"), new(2, "Estudio") });
        public Task<List<CampusAdminDto>> ListarCampusActivosAsync(CancellationToken ct = default)
            => Task.FromResult(new List<CampusAdminDto> { new(3, "Campus UCB Cochabamba", -17.3716m, -66.1439m, 500) });
        public Task<LugarDetalleAdminDto?> ObtenerLugarAsync(int lugarId, CancellationToken ct = default)
            => Task.FromResult(Existentes.Contains(lugarId)
                ? new LugarDetalleAdminDto(lugarId, "x", null, 2, -17.3716m, -66.1439m, 30, null, null, null, "ACTIVO", true) : null);
        public Task<bool> NombreEnUsoAsync(string nombre, int? excluirId, CancellationToken ct = default)
            => Task.FromResult(Nombres.Contains(nombre) && excluirId != 1);   // el lugar 1 se llama «Biblioteca Central»
        public Task<int> CrearLugarAsync(DatosLugar datos, int adminId, CancellationToken ct = default)
        {
            Llamadas.Add(("crear", datos));
            return Task.FromResult(99);
        }
        public Task<bool> EditarLugarAsync(int lugarId, DatosLugar datos, int adminId, CancellationToken ct = default)
        {
            Llamadas.Add(("editar", datos));
            return Task.FromResult(Existentes.Contains(lugarId));
        }
        public Task<bool> LugarEnUsoAsync(int lugarId, CancellationToken ct = default) => Task.FromResult(EnUso.Contains(lugarId));
        public Task<bool> EliminarLugarAsync(int lugarId, int adminId, CancellationToken ct = default)
        {
            Llamadas.Add(("eliminar", null));
            return Task.FromResult(Existentes.Contains(lugarId));
        }

        public Task<ResumenAdminDto> ResumenAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<LugarAdminDto>> ListarLugaresAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> CambiarEstadoLugarAsync(int lugarId, string estado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool?> CambiarValidacionLugarAsync(int lugarId, bool validado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ReporteAdminDto>> ListarReportesAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> ResolverReporteAsync(int reporteId, string estado, int adminId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<List<ConsultaRagAdminDto>> ListarConsultasRagAsync(int limite, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MetricasAdminDto> MetricasAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static GuardarLugarDto Pedido(string nombre = "Cafetería del Bloque C", int categoria = 1, string? abre = "07:30", string? cierra = "20:00")
        => new(nombre, "  Café y masitas  ", categoria, -17.3716m, -66.1439m, 20, abre, cierra, 3);

    private static (AdminService Servicio, RepositorioFalso Repo) Crear()
    {
        var repo = new RepositorioFalso();
        return (new AdminService(repo), repo);
    }

    [Fact]
    public async Task Crear_un_lugar_valido_lo_guarda_limpio()
    {
        var (servicio, repo) = Crear();

        var creado = await servicio.CrearLugarAsync(Pedido(nombre: "  Cafetería   del Bloque C "), adminId: 7);

        Assert.Equal(99, creado.Id);
        var (accion, d) = Assert.Single(repo.Llamadas);
        Assert.Equal("crear", accion);
        Assert.Equal(("Cafetería del Bloque C", "Café y masitas", new TimeOnly(7, 30), new TimeOnly(20, 0)),
            (d!.Nombre, d.Descripcion, d.HoraApertura, d.HoraCierre));
    }

    [Fact]
    public async Task Un_nombre_que_ya_existe_no_se_repite()
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearLugarAsync(Pedido(nombre: "Biblioteca Central"), 7));
        Assert.Equal("NOMBRE_REPETIDO", ex.Codigo);
        Assert.Empty(repo.Llamadas);
    }

    [Fact]
    public async Task Editar_un_lugar_puede_conservar_su_propio_nombre()
    {
        var (servicio, repo) = Crear();
        await servicio.EditarLugarAsync(1, Pedido(nombre: "Biblioteca Central", categoria: 2), 7);
        Assert.Equal("editar", Assert.Single(repo.Llamadas).Accion);
    }

    [Theory]
    [InlineData(9, "07:30", "20:00")]   // categoría que no existe
    [InlineData(1, "25:00", "20:00")]   // hora ilegible
    [InlineData(1, "07:30", null)]      // horario a medias
    public async Task Datos_invalidos_no_llegan_a_la_base(int categoria, string? abre, string? cierra)
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearLugarAsync(Pedido(categoria: categoria, abre: abre, cierra: cierra), 7));
        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Empty(repo.Llamadas);
    }

    [Fact]
    public async Task Editar_un_lugar_que_no_existe_responde_no_encontrado()
    {
        var (servicio, _) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.EditarLugarAsync(50, Pedido(), 7));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }

    [Fact]
    public async Task Un_lugar_que_ya_se_uso_no_se_elimina()
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.EliminarLugarAsync(1, 7));
        Assert.Equal(("EN_USO", ReglasLugar.MensajeEnUso), (ex.Codigo, ex.Message));
        Assert.Empty(repo.Llamadas);
    }

    [Fact]
    public async Task Un_lugar_sin_uso_se_elimina()
    {
        var (servicio, repo) = Crear();
        await servicio.EliminarLugarAsync(2, 7);
        Assert.Equal("eliminar", Assert.Single(repo.Llamadas).Accion);
    }

    [Fact]
    public async Task Eliminar_uno_que_no_existe_responde_no_encontrado()
    {
        var (servicio, _) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.EliminarLugarAsync(50, 7));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
    }
}

/// <summary>Reportes del estudiante con un repositorio falso.</summary>
public class ReporteServiceTests
{
    private sealed class RepositorioFalso : IReporteRepository
    {
        public int Pendientes { get; set; }
        public HashSet<int> LugaresReportados { get; } = [];
        public List<(string Motivo, int? Lugar, string Descripcion)> Creados { get; } = [];

        public Task<List<LugarReportableDto>> ListarLugaresAsync(CancellationToken ct = default)
            => Task.FromResult(new List<LugarReportableDto> { new(4, "Biblioteca Central") });
        public Task<bool> LugarExisteAsync(int lugarId, CancellationToken ct = default) => Task.FromResult(lugarId == 4);
        public Task<int> ContarPendientesAsync(int usuarioId, CancellationToken ct = default) => Task.FromResult(Pendientes);
        public Task<bool> TienePendienteDelLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default)
            => Task.FromResult(LugaresReportados.Contains(lugarId));
        public Task<int> CrearAsync(int usuarioId, string motivo, int? lugarId, string descripcion, CancellationToken ct = default)
        {
            Creados.Add((motivo, lugarId, descripcion));
            return Task.FromResult(12);
        }
    }

    private static (ReporteService Servicio, RepositorioFalso Repo) Crear()
    {
        var repo = new RepositorioFalso();
        return (new ReporteService(repo), repo);
    }

    [Fact]
    public async Task Un_reporte_de_un_lugar_queda_pendiente()
    {
        var (servicio, repo) = Crear();

        var r = await servicio.CrearAsync(1, new NuevoReporteDto("lugar_incorrecto", 4, "  Dice que abre a las 7 y abre a las 9  "));

        Assert.Equal((12, "PENDIENTE"), (r.Id, r.Estado));
        Assert.Equal(("LUGAR_INCORRECTO", (int?)4, "Dice que abre a las 7 y abre a las 9"), Assert.Single(repo.Creados));
    }

    [Fact]
    public async Task Un_reporte_general_va_sin_lugar()
    {
        var (servicio, repo) = Crear();
        await servicio.CrearAsync(1, new NuevoReporteDto("ERROR_APP", null, "La app se cierra al abrir el horario"));
        Assert.Null(Assert.Single(repo.Creados).Lugar);
    }

    [Fact]
    public async Task Un_lugar_que_no_existe_responde_no_encontrado()
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.CrearAsync(1, new NuevoReporteDto("LUGAR_INCORRECTO", 77, "Una descripción suficiente")));
        Assert.Equal("NO_ENCONTRADO", ex.Codigo);
        Assert.Empty(repo.Creados);
    }

    [Fact]
    public async Task El_mismo_lugar_no_se_reporta_dos_veces_mientras_esta_pendiente()
    {
        var (servicio, repo) = Crear();
        repo.LugaresReportados.Add(4);
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.CrearAsync(1, new NuevoReporteDto("LUGAR_INCORRECTO", 4, "Una descripción suficiente")));
        Assert.Equal("YA_REPORTADO", ex.Codigo);
    }

    [Fact]
    public async Task Con_3_pendientes_no_se_aceptan_mas()
    {
        var (servicio, repo) = Crear();
        repo.Pendientes = 3;
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            servicio.CrearAsync(1, new NuevoReporteDto("SUGERENCIA", null, "Una sugerencia bastante útil")));
        Assert.Equal("DEMASIADOS_PENDIENTES", ex.Codigo);
        Assert.Empty(repo.Creados);
    }

    [Fact]
    public async Task Un_reporte_invalido_no_llega_a_la_base()
    {
        var (servicio, repo) = Crear();
        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.CrearAsync(1, new NuevoReporteDto("SUGERENCIA", null, "corto")));
        Assert.Equal("VALIDACION", ex.Codigo);
        Assert.Empty(repo.Creados);
    }
}
