using System.Text.Json;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Perfil;

/// <summary>
/// Casos de uso de HU21: ver y editar el perfil. Solo se editan nombre, carrera y semestre; el correo institucional
/// y el rol no se pueden cambiar desde aquí. Cada cambio queda en historial_cambio.
/// </summary>
public class PerfilService
{
    private readonly IPerfilRepository _repo;
    public PerfilService(IPerfilRepository repo) => _repo = repo;

    public async Task<PerfilDto> ObtenerAsync(int usuarioId, CancellationToken ct = default)
        => ADto(await ObtenerUsuarioAsync(usuarioId, ct));

    public async Task<PerfilDto> ActualizarAsync(int usuarioId, ActualizarPerfilRequest req, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);

        // Correo y rol no son editables: si llegan distintos se rechaza en vez de ignorarlos en silencio.
        if (req.Correo is not null && !string.Equals(req.Correo.Trim(), usuario.Email, StringComparison.OrdinalIgnoreCase))
            throw new ReglaNegocioException("NO_EDITABLE", "El correo institucional no se puede modificar.");
        if (req.Rol is not null && !string.Equals(req.Rol.Trim(), usuario.Rol, StringComparison.OrdinalIgnoreCase))
            throw new ReglaNegocioException("NO_EDITABLE", "El rol no se puede modificar.");

        var error = ReglasPerfil.Validar(req.Nombre, req.Carrera, req.Semestre);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);

        var nombre = req.Nombre!.Trim();
        var carrera = string.IsNullOrWhiteSpace(req.Carrera) ? null : req.Carrera.Trim();
        var semestre = req.Semestre is null ? null : (byte?)req.Semestre;

        if (nombre == usuario.Nombre && carrera == usuario.Carrera && semestre == usuario.Semestre)
            return ADto(usuario);   // sin cambios: no se escribe ni se audita

        var antes = Valores(usuario);
        usuario.Nombre = nombre;
        usuario.Carrera = carrera;
        usuario.Semestre = semestre;
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = usuarioId;

        await _repo.GuardarConHistorialAsync(usuarioId, antes, Valores(usuario), ct);
        return ADto(usuario);
    }

    private async Task<Usuario> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct) =>
        await _repo.ObtenerUsuarioAsync(usuarioId, ct)
        ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

    private static string Valores(Usuario u) =>
        JsonSerializer.Serialize(new { nombre = u.Nombre, carrera = u.Carrera, semestre = u.Semestre });

    private static PerfilDto ADto(Usuario u) => new(u.Nombre, u.Email, u.Rol, u.Carrera, u.Semestre);
}
