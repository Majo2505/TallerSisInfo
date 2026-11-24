using Breaku.Domain.Rules;

namespace Breaku.Domain;

/// <summary>
/// HU32: cuándo el sistema avisa al administrador sobre un estudiante, y con qué texto. Solo avisa: nunca advierte ni
/// sanciona por su cuenta; el admin decide. Los administradores no reciben alertas sobre otros admins (no se sancionan).
/// </summary>
public static class ReglasAlertasAdmin
{
    /// <summary>Intentos bloqueados por seguridad que disparan la alerta, dentro de <see cref="Ventana"/>.</summary>
    public const int IntentosParaAlerta = 3;
    public static readonly TimeSpan Ventana = TimeSpan.FromHours(24);

    /// <summary>Reportes pendientes, de personas distintas, sobre lo que creó el mismo estudiante.</summary>
    public const int ReportesParaAlerta = 3;

    /// <summary>Motivos que el formulario de advertencia trae sugeridos (editables; nunca se envían solos).</summary>
    public const string MotivoSugeridoSospechoso = "Intentos repetidos bloqueados por seguridad";
    public const string MotivoSugeridoReportes = "Reportes acumulados";

    private const int LargoMaximoNombre = 60;

    /// <summary>Desde cuándo se cuentan los intentos (UTC).</summary>
    public static DateTime InicioVentana(DateTime ahoraUtc) => ahoraUtc - Ventana;

    /// <summary>Se avisa desde el tercer intento, y cada intento nuevo actualiza la misma alerta.</summary>
    public static bool AlertarPorIntentos(int intentosEnVentana, bool esAdmin)
        => !esAdmin && intentosEnVentana >= IntentosParaAlerta;

    /// <summary>Se avisa con 3 o más personas distintas que reportaron y siguen esperando respuesta.</summary>
    public static bool AlertarPorReportes(int personasQueReportaron, bool esAdmin)
        => !esAdmin && personasQueReportaron >= ReportesParaAlerta;

    /// <summary>
    /// Solo cuentan los intentos que escribió el estudiante. Si lo bloqueado fue la respuesta del modelo (el modelo ya
    /// respondió: <c>modelo</c> tiene valor), no es culpa del estudiante. La corrección a mano de HU11 se guarda con
    /// modelo <c>manual</c> y sí cuenta.
    /// </summary>
    public static bool CuentaComoIntento(string? modelo, string modeloCorreccionManual)
        => modelo is null || modelo == modeloCorreccionManual;

    /// <summary>
    /// El total de las últimas 24 h y, si ya lo advirtieron antes, cuántos fueron después de su última advertencia (para
    /// saber si siguió intentando tras el aviso).
    /// </summary>
    public static string MensajeSospechoso(string nombre, int intentos, int? desdeUltimaAdvertencia = null)
    {
        var base_ = $"{Nombre(nombre)} tuvo {intentos} intentos bloqueados por seguridad en las últimas 24 horas";
        return desdeUltimaAdvertencia switch
        {
            null => base_ + ".",
            1 => base_ + "; 1 desde su última advertencia.",
            var n => base_ + $"; {n} desde su última advertencia."
        };
    }

    public static string MensajeReportes(string nombre, int personas)
        => $"{Nombre(nombre)} acumula reportes pendientes de {personas} personas distintas sobre sus comunidades o eventos.";

    private static string Nombre(string nombre)
    {
        var limpio = string.IsNullOrWhiteSpace(nombre) ? "Un estudiante" : nombre.Trim();
        return limpio.Length <= LargoMaximoNombre ? limpio : limpio[..(LargoMaximoNombre - 1)].TrimEnd() + "…";
    }
}
