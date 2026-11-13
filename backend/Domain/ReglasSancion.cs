using System.Security.Cryptography;

namespace Breaku.Domain.Rules;

/// <summary>
/// HU28: sanciones y baja de usuarios. Reglas puras, sin base de datos ni HTTP.
/// Decisiones cerradas en #296: la escalera es manual (el sistema solo muestra el conteo de advertencias), una
/// advertencia levantada no cuenta, la SUSPENSION siempre tiene fecha de fin (7 días por defecto) y la BAJA es definitiva.
/// </summary>
public static class ReglasSancion
{
    public const int AdvertenciasParaSuspension = 3;
    public const int DiasSuspensionPorDefecto = 7;
    public const int LargoMaximoMotivo = 500;
    public const int LargoMaximoAviso = 200;
    public const string NombreCuentaDeBaja = "Cuenta dada de baja";

    public const string Normal = "ACTIVO";
    public const string Suspendida = "SUSPENDIDO";
    public const string DadaDeBaja = "BAJA";
    /// <summary>Cuenta con <c>activo = 0</c> sin una BAJA registrada (desactivada por fuera de HU28).</summary>
    public const string Desactivada = "DESACTIVADA";

    /// <summary>Código del 401 cuando el token es válido pero la cuenta está suspendida o dada de baja (el token vencido no lo lleva).</summary>
    public const string CodigoCuentaSancionada = "CUENTA_SANCIONADA";

    public const string ErrorValidacion = "VALIDACION";
    public const string ErrorNoEncontrado = "NO_ENCONTRADO";
    public const string ErrorNoSancionable = "NO_SANCIONABLE";
    public const string ErrorYaSuspendido = "YA_SUSPENDIDO";
    public const string ErrorYaDeBaja = "YA_DADO_DE_BAJA";
    public const string ErrorYaLevantada = "YA_LEVANTADA";
    public const string ErrorBajaNoSeLevanta = "BAJA_DEFINITIVA";

    public const string MensajeTipo = "El tipo de sanción tiene que ser ADVERTENCIA, SUSPENSION o BAJA.";
    public const string MensajeFaltaMotivo = "Escribe el motivo de la sanción.";
    public const string MensajeMotivoLargo = "El motivo puede tener hasta 500 caracteres.";
    public const string MensajeHastaFuturo = "La fecha de fin de la suspensión tiene que ser posterior a ahora.";
    public const string MensajeHastaSobra = "Solo la suspensión lleva fecha de fin.";
    public const string MensajeUsuarioNoExiste = "El usuario no existe.";
    public const string MensajeSancionNoExiste = "La sanción no existe.";
    public const string MensajeReporteNoExiste = "El reporte no existe.";
    public const string MensajeUnoMismo = "No puedes sancionarte a ti mismo.";
    public const string MensajeEsAdmin = "No se puede sancionar a un administrador.";
    public const string MensajeYaSuspendido = "El usuario ya está suspendido. Levanta la suspensión para aplicar otra.";
    public const string MensajeYaDeBaja = "El usuario ya está dado de baja.";
    public const string MensajeYaLevantada = "La sanción ya estaba levantada.";
    public const string MensajeBajaNoSeLevanta = "La baja es definitiva: no se puede levantar.";

    /// <summary>Una sanción tal como queda guardada, con lo mínimo que necesitan las reglas.</summary>
    public sealed record Registro(int Id, string Tipo, string Motivo, DateTime CreadoEn, DateTime? Hasta, DateTime? LevantadaEn);

    /// <summary>Por qué una cuenta no puede usar la plataforma (y qué se le muestra al intentar entrar).</summary>
    public sealed record BloqueoCuenta(string Tipo, string Motivo, DateTime? Hasta);

    /// <summary>Un registro vigente de verdad: no levantado y, si es suspensión, con la fecha de fin todavía por llegar.</summary>
    public static bool EstaVigente(Registro s, DateTime ahoraUtc) =>
        s.LevantadaEn is null
        && (s.Tipo == Sancion.TipoBaja || (s.Tipo == Sancion.TipoSuspension && s.Hasta is { } hasta && hasta > ahoraUtc));

    /// <summary>Las advertencias que cuentan para las 3: las levantadas por el administrador no cuentan.</summary>
    public static int ContarAdvertencias(IEnumerable<Registro> sanciones) =>
        sanciones.Count(s => s.Tipo == Sancion.TipoAdvertencia && s.LevantadaEn is null);

    /// <summary>La sanción que corresponde después de este conteo. Es solo una sugerencia: nada es automático.</summary>
    public static string SiguienteSugerida(int advertencias) =>
        advertencias >= AdvertenciasParaSuspension ? Sancion.TipoSuspension : Sancion.TipoAdvertencia;

    /// <summary>
    /// Lo que bloquea a la cuenta, o <c>null</c> si puede usar la plataforma. La BAJA bloquea (la fila queda con
    /// <c>activo = 0</c>); la suspensión bloquea mientras no esté levantada ni vencida; una advertencia nunca bloquea.
    /// </summary>
    public static BloqueoCuenta? BloqueoVigente(bool activo, IEnumerable<Registro> sanciones, DateTime ahoraUtc)
    {
        var lista = sanciones.ToList();
        if (!activo)
        {
            var baja = lista.Where(s => s.Tipo == Sancion.TipoBaja && s.LevantadaEn is null)
                .OrderByDescending(s => s.CreadoEn).FirstOrDefault();
            return baja is null
                ? new BloqueoCuenta(Desactivada, ReglasCuenta.MensajeInactiva, null)
                : new BloqueoCuenta(Sancion.TipoBaja, baja.Motivo, null);
        }

        var suspension = lista.Where(s => s.Tipo == Sancion.TipoSuspension && EstaVigente(s, ahoraUtc))
            .OrderByDescending(s => s.Hasta).FirstOrDefault();
        return suspension is null ? null : new BloqueoCuenta(Sancion.TipoSuspension, suspension.Motivo, suspension.Hasta);
    }

    /// <summary>El estado que ve el administrador en la lista: ACTIVO, SUSPENDIDO, BAJA (o DESACTIVADA).</summary>
    public static string EstadoVisible(BloqueoCuenta? bloqueo) => bloqueo?.Tipo switch
    {
        null => Normal,
        Sancion.TipoSuspension => Suspendida,
        Sancion.TipoBaja => DadaDeBaja,
        _ => Desactivada
    };

    /// <summary>El mensaje que se le muestra a quien intenta entrar con una cuenta bloqueada (hora de Bolivia).</summary>
    public static string MensajeBloqueo(BloqueoCuenta bloqueo) => bloqueo.Tipo switch
    {
        Sancion.TipoSuspension when bloqueo.Hasta is { } hasta =>
            $"Tu cuenta está suspendida hasta el {ReglasNotificacion.AHoraLocal(hasta):dd/MM/yyyy HH:mm}. Motivo: {bloqueo.Motivo}",
        Sancion.TipoSuspension => $"Tu cuenta está suspendida. Motivo: {bloqueo.Motivo}",
        Sancion.TipoBaja => $"Tu cuenta fue dada de baja. Motivo: {bloqueo.Motivo}",
        _ => bloqueo.Motivo
    };

    /// <summary>El tipo en mayúsculas, o <c>null</c> si no es ADVERTENCIA, SUSPENSION ni BAJA.</summary>
    public static string? LeerTipo(string? tipo)
    {
        var limpio = tipo?.Trim().ToUpperInvariant();
        return limpio is Sancion.TipoAdvertencia or Sancion.TipoSuspension or Sancion.TipoBaja ? limpio : null;
    }

    public sealed record SancionValida(string Tipo, string Motivo, DateTime? Hasta);

    /// <summary>
    /// Valida lo que manda el administrador. El motivo es obligatorio; la SUSPENSION sin fecha de fin recibe 7 días
    /// (nunca se guarda sin fecha); ADVERTENCIA y BAJA no llevan fecha. Devuelve el error o la sanción lista para guardar.
    /// </summary>
    public static (SancionValida? Valida, string? Error) Validar(string? tipo, string? motivo, DateTime? hasta, DateTime ahoraUtc)
    {
        var tipoLeido = LeerTipo(tipo);
        if (tipoLeido is null) return (null, MensajeTipo);

        var motivoLimpio = motivo?.Trim() ?? "";
        if (motivoLimpio.Length == 0) return (null, MensajeFaltaMotivo);
        if (motivoLimpio.Length > LargoMaximoMotivo) return (null, MensajeMotivoLargo);

        if (tipoLeido != Sancion.TipoSuspension)
            return hasta is null ? (new SancionValida(tipoLeido, motivoLimpio, null), null) : (null, MensajeHastaSobra);

        var fin = hasta is null ? ahoraUtc.AddDays(DiasSuspensionPorDefecto) : hasta.Value.ToUniversalTime();
        return fin > ahoraUtc ? (new SancionValida(tipoLeido, motivoLimpio, fin), null) : (null, MensajeHastaFuturo);
    }

    /// <summary>Un administrador no se sanciona a sí mismo ni a otro administrador. Devuelve el error, o <c>null</c> si se puede.</summary>
    public static string? ValidarObjetivo(int adminId, int usuarioId, string rolDelUsuario)
    {
        if (adminId == usuarioId) return MensajeUnoMismo;
        return rolDelUsuario == "ADMIN" ? MensajeEsAdmin : null;
    }

    /// <summary>
    /// Si la sanción nueva no se puede aplicar a este estado: una cuenta dada de baja no recibe más sanciones y una
    /// suspensión no se encima con otra (primero se levanta). Devuelve (código, mensaje) o <c>null</c>.
    /// </summary>
    public static (string Codigo, string Mensaje)? ValidarEstado(string tipo, BloqueoCuenta? bloqueo)
    {
        if (bloqueo is not null && bloqueo.Tipo != Sancion.TipoSuspension) return (ErrorYaDeBaja, MensajeYaDeBaja);
        if (tipo == Sancion.TipoSuspension && bloqueo is not null) return (ErrorYaSuspendido, MensajeYaSuspendido);
        return null;
    }

    /// <summary>Se puede levantar una advertencia o una suspensión que no esté levantada; la BAJA no termina sola ni se levanta.</summary>
    public static (string Codigo, string Mensaje)? ValidarLevantar(Registro sancion)
    {
        if (sancion.Tipo == Sancion.TipoBaja) return (ErrorBajaNoSeLevanta, MensajeBajaNoSeLevanta);
        if (sancion.LevantadaEn is not null) return (ErrorYaLevantada, MensajeYaLevantada);
        return null;
    }

    /// <summary>El aviso de la campanita por una advertencia (la columna <c>mensaje</c> admite 200 caracteres).</summary>
    public static string MensajeAdvertencia(string motivo)
    {
        var texto = $"Recibiste una advertencia: {motivo.Trim()}";
        return texto.Length <= LargoMaximoAviso ? texto : texto[..(LargoMaximoAviso - 1)] + "…";
    }

    /// <summary>
    /// El valor que reemplaza a <c>password_hash</c> en una cuenta dada de baja. <c>ck_usuario_credencial</c> exige uno
    /// de los dos (contraseña o Google); no es un hash BCrypt válido, así que ninguna contraseña lo verifica.
    /// </summary>
    public static string HashInutilizable() => "!baja!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
