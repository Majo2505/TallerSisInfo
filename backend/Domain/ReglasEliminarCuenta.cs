namespace Breaku.Domain.Rules;

/// <summary>HU29: eliminar mi propia cuenta. Reglas puras: quién puede, qué se confirma y cuándo lo impide una sanción.</summary>
public static class ReglasEliminarCuenta
{
    public const string TextoConfirmacion = "ELIMINAR";

    public const string ErrorNoEliminable = "NO_ELIMINABLE";
    public const string ErrorSancionActiva = "SANCION_ACTIVA";
    public const string ErrorConfirmacion = "VALIDACION";
    public const string ErrorContrasenaIncorrecta = "CONTRASENA_INCORRECTA";
    public const string ErrorGoogleIncorrecto = "GOOGLE_INCORRECTO";

    public const string MensajeAdmin = "El administrador no puede eliminar su cuenta por esta vía.";
    public const string MensajeSancionActiva = "Tienes una sanción activa; contacta al administrador";
    public const string MensajeFaltaConfirmacion = "Escribe ELIMINAR para confirmar.";
    public const string MensajeFaltaReautenticacion = "Escribe tu contraseña o vuelve a entrar con Google para confirmar que eres tú.";
    public const string MensajeContrasenaIncorrecta = "La contraseña no es correcta.";
    public const string MensajeGoogleIncorrecto = "No pudimos confirmar tu cuenta de Google. Entra con la cuenta que tienes enlazada.";
    public const string MensajeGoogleNoDisponible = "El inicio de sesión con Google no está configurado.";

    /// <summary>Null si el texto es exactamente ELIMINAR (sin tener en cuenta espacios alrededor).</summary>
    public static string? ValidarConfirmacion(string? confirmacion)
        => string.Equals(confirmacion?.Trim(), TextoConfirmacion, StringComparison.Ordinal) ? null : MensajeFaltaConfirmacion;

    /// <summary>Null si el rol puede eliminar su cuenta; el administrador no.</summary>
    public static string? ValidarRol(string rol) => rol == "ADMIN" ? MensajeAdmin : null;

    /// <summary>
    /// Solo una SUSPENSION vigente (no levantada y con <c>hasta</c> mayor que ahora) impide eliminar la cuenta.
    /// Una ADVERTENCIA nunca la impide; una suspensión vencida o levantada tampoco.
    /// </summary>
    public static bool TieneSancionActiva(IEnumerable<ReglasSancion.Registro> sanciones, DateTime ahoraUtc)
        => sanciones.Any(s => s.Tipo == Sancion.TipoSuspension && ReglasSancion.EstaVigente(s, ahoraUtc));

    /// <summary>Qué forma de reautenticarse eligió quien pide eliminar (contraseña, Google o ninguna).</summary>
    public static bool HayCredencial(string? contrasena, string? credencialGoogle)
        => !string.IsNullOrEmpty(contrasena) || !string.IsNullOrWhiteSpace(credencialGoogle);
}
