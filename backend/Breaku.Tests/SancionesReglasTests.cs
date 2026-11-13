using Breaku.Domain;
using Breaku.Domain.Rules;
using static Breaku.Domain.Rules.ReglasSancion;

namespace Breaku.Tests;

public class ReglasSancionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc);

    private static Registro R(string tipo, int id = 1, DateTime? hasta = null, DateTime? levantada = null, string motivo = "Lenguaje ofensivo")
        => new(id, tipo, motivo, Ahora.AddDays(-1), hasta, levantada);

    // ---------- conteo de advertencias

    [Fact]
    public void Las_advertencias_levantadas_no_cuentan()
    {
        var sanciones = new[]
        {
            R(Sancion.TipoAdvertencia, 1), R(Sancion.TipoAdvertencia, 2, levantada: Ahora), R(Sancion.TipoAdvertencia, 3),
            R(Sancion.TipoSuspension, 4, hasta: Ahora.AddDays(1))
        };

        Assert.Equal(2, ContarAdvertencias(sanciones));
    }

    [Theory]
    [InlineData(0, Sancion.TipoAdvertencia)]
    [InlineData(2, Sancion.TipoAdvertencia)]
    [InlineData(3, Sancion.TipoSuspension)]
    [InlineData(5, Sancion.TipoSuspension)]
    public void Tras_tres_advertencias_la_siguiente_es_suspension(int advertencias, string esperada)
        => Assert.Equal(esperada, SiguienteSugerida(advertencias));

    [Fact]
    public void Levantar_una_de_tres_advertencias_baja_el_conteo_y_la_sugerencia()
    {
        var tres = new[] { R(Sancion.TipoAdvertencia, 1), R(Sancion.TipoAdvertencia, 2), R(Sancion.TipoAdvertencia, 3) };
        Assert.Equal(Sancion.TipoSuspension, SiguienteSugerida(ContarAdvertencias(tres)));

        var levantada = new[] { tres[0] with { LevantadaEn = Ahora }, tres[1], tres[2] };
        Assert.Equal(Sancion.TipoAdvertencia, SiguienteSugerida(ContarAdvertencias(levantada)));
    }

    // ---------- sanción activa / bloqueo

    [Fact]
    public void Una_suspension_con_fecha_de_fin_por_llegar_bloquea_con_su_motivo_y_su_fecha()
    {
        var hasta = Ahora.AddDays(3);
        var bloqueo = BloqueoVigente(true, [R(Sancion.TipoSuspension, hasta: hasta, motivo: "Acoso")], Ahora);

        Assert.Equal(new BloqueoCuenta(Sancion.TipoSuspension, "Acoso", hasta), bloqueo);
    }

    [Fact]
    public void Una_suspension_vencida_deja_de_bloquear_sin_intervencion()
        => Assert.Null(BloqueoVigente(true, [R(Sancion.TipoSuspension, hasta: Ahora.AddSeconds(-1))], Ahora));

    [Fact]
    public void Una_suspension_justo_en_su_fecha_de_fin_ya_no_bloquea()
        => Assert.Null(BloqueoVigente(true, [R(Sancion.TipoSuspension, hasta: Ahora)], Ahora));

    [Fact]
    public void Una_suspension_levantada_no_bloquea()
        => Assert.Null(BloqueoVigente(true, [R(Sancion.TipoSuspension, hasta: Ahora.AddDays(3), levantada: Ahora.AddHours(-1))], Ahora));

    [Fact]
    public void Una_advertencia_nunca_bloquea()
        => Assert.Null(BloqueoVigente(true, [R(Sancion.TipoAdvertencia), R(Sancion.TipoAdvertencia, 2), R(Sancion.TipoAdvertencia, 3)], Ahora));

    [Fact]
    public void Con_dos_suspensiones_vigentes_gana_la_que_termina_mas_tarde()
    {
        var bloqueo = BloqueoVigente(true,
            [R(Sancion.TipoSuspension, 1, Ahora.AddDays(1), motivo: "A"), R(Sancion.TipoSuspension, 2, Ahora.AddDays(9), motivo: "B")], Ahora);

        Assert.Equal("B", bloqueo!.Motivo);
    }

    [Fact]
    public void La_baja_bloquea_con_su_motivo_y_sin_fecha_de_fin()
    {
        var bloqueo = BloqueoVigente(false, [R(Sancion.TipoAdvertencia), R(Sancion.TipoBaja, 2, motivo: "Fraude")], Ahora);

        Assert.Equal(new BloqueoCuenta(Sancion.TipoBaja, "Fraude", null), bloqueo);
    }

    [Fact]
    public void Una_cuenta_inactiva_sin_baja_registrada_queda_desactivada()
        => Assert.Equal(Desactivada, BloqueoVigente(false, [], Ahora)!.Tipo);

    [Fact]
    public void El_estado_visible_distingue_activo_suspendido_y_baja()
    {
        Assert.Equal(Normal, EstadoVisible(null));
        Assert.Equal(Suspendida, EstadoVisible(new BloqueoCuenta(Sancion.TipoSuspension, "x", Ahora)));
        Assert.Equal(DadaDeBaja, EstadoVisible(new BloqueoCuenta(Sancion.TipoBaja, "x", null)));
    }

    [Fact]
    public void El_mensaje_de_suspension_dice_motivo_y_fecha_de_fin_en_hora_de_Bolivia()
    {
        // 2026-11-20 15:00 UTC = 11:00 en Bolivia (UTC-4).
        var mensaje = MensajeBloqueo(new BloqueoCuenta(Sancion.TipoSuspension, "Acoso", new DateTime(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc)));

        Assert.Equal("Tu cuenta está suspendida hasta el 20/11/2026 11:00. Motivo: Acoso", mensaje);
    }

    [Fact]
    public void El_mensaje_de_baja_dice_el_motivo_y_ninguna_fecha()
        => Assert.Equal("Tu cuenta fue dada de baja. Motivo: Fraude", MensajeBloqueo(new BloqueoCuenta(Sancion.TipoBaja, "Fraude", null)));

    // ---------- validación de lo que manda el admin

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_motivo_la_sancion_no_vale(string? motivo)
    {
        var (valida, error) = Validar("ADVERTENCIA", motivo, null, Ahora);

        Assert.Null(valida);
        Assert.Equal(MensajeFaltaMotivo, error);
    }

    [Fact]
    public void Un_motivo_de_mas_de_500_caracteres_no_vale()
        => Assert.Equal(MensajeMotivoLargo, Validar("BAJA", new string('x', 501), null, Ahora).Error);

    [Theory]
    [InlineData(null)]
    [InlineData("MULTA")]
    [InlineData("")]
    public void Solo_valen_advertencia_suspension_y_baja(string? tipo)
        => Assert.Equal(MensajeTipo, Validar(tipo, "Motivo", null, Ahora).Error);

    [Fact]
    public void El_tipo_se_lee_sin_importar_mayusculas_ni_espacios()
        => Assert.Equal(Sancion.TipoSuspension, Validar("  suspension ", " Motivo ", null, Ahora).Valida!.Tipo);

    [Fact]
    public void Una_suspension_sin_fecha_de_fin_recibe_siete_dias()
    {
        var (valida, error) = Validar("SUSPENSION", "Acoso", null, Ahora);

        Assert.Null(error);
        Assert.Equal(Ahora.AddDays(DiasSuspensionPorDefecto), valida!.Hasta);
        Assert.Equal(7, DiasSuspensionPorDefecto);
    }

    [Fact]
    public void Una_suspension_con_fecha_futura_la_respeta()
    {
        var hasta = Ahora.AddDays(30);

        Assert.Equal(hasta, Validar("SUSPENSION", "Acoso", hasta, Ahora).Valida!.Hasta);
    }

    [Fact]
    public void Una_suspension_con_fecha_pasada_no_vale()
        => Assert.Equal(MensajeHastaFuturo, Validar("SUSPENSION", "Acoso", Ahora.AddMinutes(-1), Ahora).Error);

    [Theory]
    [InlineData("ADVERTENCIA")]
    [InlineData("BAJA")]
    public void Solo_la_suspension_lleva_fecha_de_fin(string tipo)
        => Assert.Equal(MensajeHastaSobra, Validar(tipo, "Motivo", Ahora.AddDays(1), Ahora).Error);

    [Theory]
    [InlineData("ADVERTENCIA")]
    [InlineData("BAJA")]
    public void La_advertencia_y_la_baja_se_guardan_sin_fecha_de_fin(string tipo)
        => Assert.Null(Validar(tipo, "Motivo", null, Ahora).Valida!.Hasta);

    // ---------- a quién se puede sancionar

    [Fact]
    public void Un_admin_no_se_sanciona_a_si_mismo()
        => Assert.Equal(MensajeUnoMismo, ValidarObjetivo(2, 2, "ADMIN"));

    [Fact]
    public void Un_admin_no_sanciona_a_otro_admin()
        => Assert.Equal(MensajeEsAdmin, ValidarObjetivo(2, 9, "ADMIN"));

    [Fact]
    public void Un_admin_si_sanciona_a_un_estudiante()
        => Assert.Null(ValidarObjetivo(2, 9, "ESTUDIANTE"));

    [Fact]
    public void Una_cuenta_dada_de_baja_no_recibe_mas_sanciones()
    {
        var baja = new BloqueoCuenta(Sancion.TipoBaja, "x", null);

        Assert.Equal(ErrorYaDeBaja, ValidarEstado(Sancion.TipoAdvertencia, baja)!.Value.Codigo);
        Assert.Equal(ErrorYaDeBaja, ValidarEstado(Sancion.TipoBaja, baja)!.Value.Codigo);
    }

    [Fact]
    public void Una_suspension_vigente_no_se_encima_con_otra_pero_si_admite_advertencia_o_baja()
    {
        var suspendida = new BloqueoCuenta(Sancion.TipoSuspension, "x", Ahora.AddDays(2));

        Assert.Equal(ErrorYaSuspendido, ValidarEstado(Sancion.TipoSuspension, suspendida)!.Value.Codigo);
        Assert.Null(ValidarEstado(Sancion.TipoAdvertencia, suspendida));
        Assert.Null(ValidarEstado(Sancion.TipoBaja, suspendida));
    }

    [Fact]
    public void Un_usuario_sin_bloqueo_recibe_cualquier_sancion()
    {
        Assert.Null(ValidarEstado(Sancion.TipoAdvertencia, null));
        Assert.Null(ValidarEstado(Sancion.TipoSuspension, null));
        Assert.Null(ValidarEstado(Sancion.TipoBaja, null));
    }

    // ---------- levantar

    [Fact]
    public void La_baja_no_se_levanta()
        => Assert.Equal(ErrorBajaNoSeLevanta, ValidarLevantar(R(Sancion.TipoBaja))!.Value.Codigo);

    [Fact]
    public void Una_sancion_ya_levantada_no_se_levanta_dos_veces()
        => Assert.Equal(ErrorYaLevantada, ValidarLevantar(R(Sancion.TipoAdvertencia, levantada: Ahora))!.Value.Codigo);

    [Fact]
    public void Una_advertencia_o_una_suspension_se_levantan()
    {
        Assert.Null(ValidarLevantar(R(Sancion.TipoAdvertencia)));
        Assert.Null(ValidarLevantar(R(Sancion.TipoSuspension, hasta: Ahora.AddDays(1))));
    }

    // ---------- aviso y baja

    [Fact]
    public void El_aviso_de_la_advertencia_cabe_en_la_columna_de_200_caracteres()
    {
        var aviso = MensajeAdvertencia(new string('a', 500));

        Assert.Equal(LargoMaximoAviso, aviso.Length);
        Assert.StartsWith("Recibiste una advertencia: ", aviso);
        Assert.EndsWith("…", aviso);
    }

    [Fact]
    public void Un_motivo_corto_va_completo_en_el_aviso()
        => Assert.Equal("Recibiste una advertencia: Lenguaje ofensivo", MensajeAdvertencia(" Lenguaje ofensivo "));

    [Fact]
    public void El_hash_inutilizable_no_es_un_hash_BCrypt_y_es_distinto_cada_vez()
    {
        var a = HashInutilizable();
        var b = HashInutilizable();

        Assert.NotEqual(a, b);
        Assert.InRange(a.Length, 1, 255); // columna password_hash VARCHAR(255)
        Assert.False(a.StartsWith("$2", StringComparison.Ordinal));
        Assert.Throws<BCrypt.Net.SaltParseException>(() => BCrypt.Net.BCrypt.Verify("cualquiera", a));
    }
}

public class ReglasCuentaBloqueoTests
{
    private static readonly ReglasCuenta.IdentidadGoogle Identidad = new("sub-1", "ana@ucb.edu.bo", true, "Ana");
    private static readonly ReglasSancion.BloqueoCuenta Baja = new(Sancion.TipoBaja, "Fraude", null);
    private static readonly ReglasSancion.BloqueoCuenta Suspension = new(Sancion.TipoSuspension, "Acoso", new DateTime(2026, 11, 20, 15, 0, 0, DateTimeKind.Utc));

    private static ReglasCuenta.CuentaExistente Cuenta(ReglasSancion.BloqueoCuenta? bloqueo, bool activa = true, string? sub = "sub-1")
        => new(3, "ESTUDIANTE", activa, sub, bloqueo);

    [Fact]
    public void Una_cuenta_dada_de_baja_se_rechaza_con_el_motivo_y_no_se_crea_otra_con_Google()
    {
        var cuenta = Cuenta(Baja, activa: false);

        var porSub = ReglasCuenta.DecidirGoogle(Identidad, cuenta, null);
        var porCorreo = ReglasCuenta.DecidirGoogle(Identidad, null, cuenta);

        foreach (var decision in new[] { porSub, porCorreo })
        {
            Assert.Equal(ReglasCuenta.Accion.Rechazar, decision.Accion);
            Assert.Equal(Baja, decision.Bloqueo);
            Assert.Contains("Fraude", decision.Mensaje);
        }
    }

    [Fact]
    public void Una_cuenta_suspendida_se_rechaza_con_el_motivo_y_la_fecha_de_fin()
    {
        var decision = ReglasCuenta.DecidirGoogle(Identidad, Cuenta(Suspension), null);

        Assert.Equal(ReglasCuenta.Accion.Rechazar, decision.Accion);
        Assert.Equal(Suspension, decision.Bloqueo);
        Assert.Contains("20/11/2026 11:00", decision.Mensaje);
        Assert.Contains("Acoso", decision.Mensaje);
    }

    [Fact]
    public void Con_la_suspension_vencida_o_levantada_la_cuenta_entra()
        => Assert.Equal(ReglasCuenta.Accion.Entrar, ReglasCuenta.DecidirGoogle(Identidad, Cuenta(null), null).Accion);

    [Fact]
    public void El_administrador_sigue_sin_entrar_con_Google_aunque_tenga_bloqueo()
    {
        var admin = new ReglasCuenta.CuentaExistente(1, "ADMIN", true, null, Suspension);

        Assert.Equal(ReglasCuenta.MensajeAdmin, ReglasCuenta.DecidirGoogle(Identidad, null, admin).Mensaje);
    }
}
