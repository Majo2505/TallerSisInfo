namespace Breaku.Domain.Rules;

public static class ReglasPerfil
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoCarrera = 80;
    public const int SemestreMinimo = 1;
    public const int SemestreMaximo = 10;

    public static string? Validar(string? nombre, string? carrera, int? semestre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "El nombre es obligatorio.";
        if (nombre.Trim().Length > LargoMaximoNombre) return $"El nombre no puede superar {LargoMaximoNombre} caracteres.";
        if (carrera is not null && carrera.Trim().Length > LargoMaximoCarrera)
            return $"La carrera no puede superar {LargoMaximoCarrera} caracteres.";
        if (semestre is not null && (semestre < SemestreMinimo || semestre > SemestreMaximo))
            return $"El semestre tiene que estar entre {SemestreMinimo} y {SemestreMaximo}.";
        return null;
    }
}
