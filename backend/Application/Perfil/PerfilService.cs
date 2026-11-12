using System.Text.Json;
using Breaku.Application.Common.Interfaces;
using Breaku.Application.Horarios;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Perfil;

public class PerfilService
{
    private readonly IPerfilRepository _repo;
    private readonly IHasherContrasenas _hasher;

    public PerfilService(IPerfilRepository repo, IHasherContrasenas hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    /// <summary>
    /// Cambia la contraseña (pide la actual) o la crea si la cuenta entra solo con Google. La nueva tiene al menos 8
    /// caracteres, una letra y un número, y es distinta de la actual. Queda en el historial sin la contraseña.
    /// </summary>
    public async Task CambiarContrasenaAsync(int usuarioId, CambiarContrasenaRequest req, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);
        var tenia = usuario.PasswordHash is not null;

        if (tenia)
        {
            if (string.IsNullOrEmpty(req.Actual)) throw new ReglaNegocioException("VALIDACION", ReglasCuenta.MensajeFaltaActual);
            if (!_hasher.Verificar(req.Actual, usuario.PasswordHash))
                throw new ReglaNegocioException("CONTRASENA_INCORRECTA", ReglasCuenta.MensajeActualIncorrecta);
        }

        var error = ReglasCuenta.ValidarNueva(req.Nueva, req.Confirmacion);
        if (error is not null) throw new ReglaNegocioException("VALIDACION", error);
        if (tenia && _hasher.Verificar(req.Nueva!, usuario.PasswordHash))
            throw new ReglaNegocioException("VALIDACION", ReglasCuenta.MensajeIgualALaActual);

        usuario.PasswordHash = _hasher.Cifrar(req.Nueva!);
        usuario.UpdatedAt = DateTime.UtcNow;
        usuario.UpdatedBy = usuarioId;
        await _repo.GuardarConHistorialAsync(usuarioId,
            JsonSerializer.Serialize(new { contrasena = tenia ? "(oculta)" : null }),
            JsonSerializer.Serialize(new { contrasena = tenia ? "cambiada" : "creada" }), ct);
    }

    public async Task<PerfilDto> ObtenerAsync(int usuarioId, CancellationToken ct = default)
        => ADto(await ObtenerUsuarioAsync(usuarioId, ct));

    public async Task<PerfilDto> ActualizarAsync(int usuarioId, ActualizarPerfilRequest req, CancellationToken ct = default)
    {
        var usuario = await ObtenerUsuarioAsync(usuarioId, ct);

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
            return ADto(usuario);

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

    private static PerfilDto ADto(Usuario u) => new(u.Nombre, u.Email, u.Rol, u.Carrera, u.Semestre, u.PasswordHash is not null, u.GoogleSub is not null);
}
