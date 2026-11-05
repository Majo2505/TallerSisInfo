namespace Breaku.Domain;

public static class ReglasCorreoInstitucional
{
    public const string Dominio = "@ucb.edu.bo";
    public const string DominioGoogle = "ucb.edu.bo";
    public const string MensajeNoInstitucional = "Solo se permite el acceso con un correo institucional (@ucb.edu.bo)";

    public static bool EsInstitucional(string? correo)
    {
        var valor = correo?.Trim() ?? string.Empty;
        var usuario = valor.Length > Dominio.Length ? valor[..^Dominio.Length] : string.Empty;
        return usuario.Length > 0 && !usuario.Contains('@') && valor.EndsWith(Dominio, StringComparison.OrdinalIgnoreCase);
    }
}
