using Breaku.Application.Horarios;
using Breaku.Application.Lugares;
using Breaku.Domain.Rules;

namespace Breaku.Tests;

public class FotosLugarTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8, .. "VP8 "u8];

    [Fact]
    public void Reconoce_jpg_png_y_webp_por_su_contenido()
    {
        Assert.Equal("image/jpeg", ReglasFotoLugar.TipoReal(Jpeg));
        Assert.Equal("image/png", ReglasFotoLugar.TipoReal(Png));
        Assert.Equal("image/webp", ReglasFotoLugar.TipoReal(Webp));
    }

    [Theory]
    [InlineData("GIF89a")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("%PDF-1.7")]
    [InlineData("RIFF....AVI ")]
    public void Un_archivo_que_no_es_jpg_png_ni_webp_se_rechaza_aunque_diga_ser_imagen(string contenido)
        => Assert.Equal(ReglasFotoLugar.MensajeTipo, ReglasFotoLugar.Validar(System.Text.Encoding.ASCII.GetBytes(contenido)).Error);

    [Fact]
    public void Sin_foto_pide_elegir_una()
    {
        Assert.Equal(ReglasFotoLugar.MensajeVacia, ReglasFotoLugar.Validar(null).Error);
        Assert.Equal(ReglasFotoLugar.MensajeVacia, ReglasFotoLugar.Validar([]).Error);
    }

    [Fact]
    public void Dos_megas_justos_entran_y_un_byte_mas_no()
    {
        var justa = new byte[ReglasFotoLugar.BytesMaximos];
        Jpeg.CopyTo(justa, 0);
        var grande = new byte[ReglasFotoLugar.BytesMaximos + 1];
        Jpeg.CopyTo(grande, 0);

        Assert.Null(ReglasFotoLugar.Validar(justa).Error);
        Assert.Equal(ReglasFotoLugar.MensajeGrande, ReglasFotoLugar.Validar(grande).Error);
    }

    [Fact]
    public void Sin_foto_no_hay_url_y_la_ilustracion_queda()
        => Assert.Null(ReglasFotoLugar.Url(12, null));

    [Fact]
    public void La_url_lleva_la_version_y_cambia_cuando_cambia_la_foto()
    {
        var antes = ReglasFotoLugar.Url(12, new DateTime(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc));
        var despues = ReglasFotoLugar.Url(12, new DateTime(2026, 11, 7, 10, 0, 1, DateTimeKind.Utc));

        Assert.StartsWith("/api/lugares/12/foto?v=", antes);
        Assert.NotEqual(antes, despues);
    }

    [Fact]
    public void La_version_se_redondea_al_microsegundo_como_la_guarda_la_base()
    {
        var ahora = new DateTime(639_000_000_000_000_007, DateTimeKind.Utc);

        Assert.Equal(0, ReglasFotoLugar.Version(ahora).Ticks % 10);
    }

    private static readonly DateTime Ahora = new(2026, 11, 7, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Subir_guarda_el_tipo_real_y_devuelve_la_url_con_la_version()
    {
        var repo = new FotosFalsas();
        var servicio = new FotoLugarService(repo, () => Ahora);

        var guardada = await servicio.SubirAsync(12, Png, adminId: 1);

        Assert.Equal($"/api/lugares/12/foto?v={Ahora.Ticks}", guardada.FotoUrl);
        Assert.Equal("image/png", repo.Fotos[12].TipoMime);
    }

    [Fact]
    public async Task Subir_un_archivo_que_no_es_imagen_no_toca_la_base()
    {
        var repo = new FotosFalsas();

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            new FotoLugarService(repo).SubirAsync(12, "hola"u8.ToArray(), 1));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Empty(repo.Fotos);
    }

    [Fact]
    public async Task Subir_a_un_lugar_que_no_existe_da_no_encontrado()
    {
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            new FotoLugarService(new FotosFalsas()).SubirAsync(999, Jpeg, 1));

        Assert.Equal("NO_ENCONTRADO", error.Codigo);
    }

    [Fact]
    public async Task Pedir_la_foto_de_un_lugar_sin_foto_da_no_encontrado()
    {
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => new FotoLugarService(new FotosFalsas()).ObtenerAsync(12));

        Assert.Equal(ReglasFotoLugar.MensajeSinFoto, error.Message);
    }

    [Fact]
    public async Task Quitar_la_foto_la_borra_y_quitarla_de_nuevo_no_falla()
    {
        var repo = new FotosFalsas();
        var servicio = new FotoLugarService(repo);
        await servicio.SubirAsync(12, Jpeg, 1);

        await servicio.EliminarAsync(12, 1);
        await servicio.EliminarAsync(12, 1);

        Assert.Empty(repo.Fotos);
    }

    private sealed class FotosFalsas : IFotoLugarRepository
    {
        private static readonly HashSet<int> Lugares = [12, 13];
        public Dictionary<int, FotoLugar> Fotos { get; } = [];

        public Task<FotoLugar?> ObtenerAsync(int lugarId, CancellationToken ct = default)
            => Task.FromResult(Fotos.GetValueOrDefault(lugarId));

        public Task<bool> GuardarAsync(int lugarId, byte[] contenido, string tipoMime, DateTime version, int adminId, CancellationToken ct = default)
        {
            if (!Lugares.Contains(lugarId)) return Task.FromResult(false);
            Fotos[lugarId] = new FotoLugar(contenido, tipoMime, version);
            return Task.FromResult(true);
        }

        public Task<bool> EliminarAsync(int lugarId, int adminId, CancellationToken ct = default)
        {
            if (!Lugares.Contains(lugarId)) return Task.FromResult(false);
            Fotos.Remove(lugarId);
            return Task.FromResult(true);
        }
    }
}
