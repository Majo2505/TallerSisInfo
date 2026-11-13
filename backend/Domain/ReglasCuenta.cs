using System.Text;

namespace Breaku.Domain.Rules;

/// <summary>Contraseñas y acceso con Google: reglas puras, sin base de datos ni HTTP.</summary>
public static class ReglasCuenta
{
    public const string DominioInstitucional = "ucb.edu.bo";

    // ---------- intentos (fuerza bruta)

    public const string PoliticaLogin = "login";
    public const string PoliticaContrasena = "contrasena";
    /// <summary>Por IP: alto porque en el campus muchos salen por la misma IP.</summary>
    public const int IntentosLoginPorMinuto = 30;
    /// <summary>Por estudiante.</summary>
    public const int IntentosContrasenaPorMinuto = 5;
    public const string MensajeDemasiadosIntentos = "Hiciste muchos intentos seguidos. Espera un minuto y vuelve a intentarlo.";

    // ---------- contraseñas

    public const int LargoMinimo = 8;
    /// <summary>BCrypt solo usa los primeros 72 bytes: más allá, dos contraseñas distintas darían igual.</summary>
    public const int BytesMaximos = 72;

    public const string MensajeNoCoinciden = "Las contraseñas no coinciden.";
    public const string MensajeIgualALaActual = "La nueva contraseña tiene que ser distinta de la actual.";
    public const string MensajeActualIncorrecta = "La contraseña actual no es correcta.";
    public const string MensajeFaltaActual = "Escribe tu contraseña actual.";

    /// <summary>Null si la contraseña nueva es válida; si no, el mensaje para el estudiante.</summary>
    public static string? ValidarNueva(string? nueva, string? confirmacion)
    {
        if (string.IsNullOrEmpty(nueva)) return "Escribe la nueva contraseña.";
        if (nueva.Length < LargoMinimo) return $"La contraseña tiene que tener al menos {LargoMinimo} caracteres.";
        if (Encoding.UTF8.GetByteCount(nueva) > BytesMaximos) return "La contraseña es demasiado larga (máximo 72 caracteres sin tildes).";
        if (nueva.Trim() != nueva) return "La contraseña no puede empezar ni terminar con espacios.";
        if (!nueva.Any(char.IsLetter) || !nueva.Any(char.IsDigit)) return "La contraseña tiene que tener al menos una letra y un número.";
        if (nueva != confirmacion) return MensajeNoCoinciden;
        return null;
    }

    // ---------- Google

    /// <summary>Lo que dice el token de Google, ya verificado (firma, emisor, audiencia y vencimiento).</summary>
    public sealed record IdentidadGoogle(string Sub, string Correo, bool CorreoVerificado, string? Nombre);

    // Bloqueo: por qué la cuenta no puede entrar (suspensión o baja de HU28); null si puede.
    public sealed record CuentaExistente(int Id, string Rol, bool Activa, string? GoogleSub,
        ReglasSancion.BloqueoCuenta? Bloqueo = null);

    public enum Accion { Entrar, Enlazar, Crear, Rechazar }

    public sealed record Decision(Accion Accion, int? UsuarioId = null, string? Mensaje = null,
        ReglasSancion.BloqueoCuenta? Bloqueo = null)
    {
        public static Decision No(string mensaje) => new(Accion.Rechazar, Mensaje: mensaje);

        /// <summary>Cuenta suspendida o dada de baja: se rechaza con el motivo (y la fecha de fin, si la hay).</summary>
        public static Decision Bloqueada(ReglasSancion.BloqueoCuenta bloqueo)
            => new(Accion.Rechazar, Mensaje: ReglasSancion.MensajeBloqueo(bloqueo), Bloqueo: bloqueo);
    }

    public const string MensajeSoloInstitucional = "Entra con tu cuenta de Google institucional (@ucb.edu.bo).";
    public const string MensajeCorreoSinVerificar = "Tu cuenta de Google no tiene el correo verificado.";
    public const string MensajeAdmin = "El administrador entra con su correo y contraseña.";
    public const string MensajeInactiva = "Tu cuenta está desactivada.";
    public const string MensajeOtraCuentaGoogle = "Tu cuenta ya está enlazada con otra cuenta de Google.";

    /// <summary>
    /// Qué hacer con quien entra con Google:
    /// · Ya enlazada (mismo <c>sub</c>) → entra.
    /// · Mismo correo sin Google → se enlaza y entra (Google ya verificó que el correo es suyo).
    /// · Sin cuenta → se crea como estudiante, sin contraseña (la puede crear después en el perfil).
    /// El administrador entra solo con contraseña: nadie se vuelve admin por una cuenta de Google.
    /// </summary>
    public static Decision DecidirGoogle(IdentidadGoogle identidad, CuentaExistente? porSub, CuentaExistente? porCorreo)
    {
        if (!identidad.CorreoVerificado) return Decision.No(MensajeCorreoSinVerificar);
        if (!EsInstitucional(identidad.Correo)) return Decision.No(MensajeSoloInstitucional);

        var cuenta = porSub ?? porCorreo;
        if (cuenta is null) return new Decision(Accion.Crear);
        if (cuenta.Rol == "ADMIN") return Decision.No(MensajeAdmin);
        // Una cuenta dada de baja conserva su fila (con correo y google_sub): por eso se la encuentra y nunca se crea otra.
        if (cuenta.Bloqueo is not null) return Decision.Bloqueada(cuenta.Bloqueo);
        if (!cuenta.Activa) return Decision.No(MensajeInactiva);
        if (porSub is not null) return new Decision(Accion.Entrar, porSub.Id);
        if (cuenta.GoogleSub is not null && cuenta.GoogleSub != identidad.Sub) return Decision.No(MensajeOtraCuentaGoogle);
        return new Decision(Accion.Enlazar, cuenta.Id);
    }

    public static bool EsInstitucional(string? correo)
    {
        var valor = correo?.Trim() ?? "";
        var arroba = valor.LastIndexOf('@');
        return arroba > 0 && valor.IndexOf('@') == arroba
               && string.Equals(valor[(arroba + 1)..], DominioInstitucional, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>El nombre que trae Google, o la parte del correo antes de la @ si no trae (máx. 100, como la columna).</summary>
    public static string NombreParaCuentaNueva(IdentidadGoogle identidad)
    {
        var nombre = string.IsNullOrWhiteSpace(identidad.Nombre) ? identidad.Correo.Split('@')[0] : identidad.Nombre.Trim();
        return nombre.Length > 100 ? nombre[..100] : nombre;
    }
}
