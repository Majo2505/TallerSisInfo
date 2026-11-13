using Breaku.Application.Common.Interfaces;
using Breaku.Application.Horarios;
using Breaku.Application.Sanciones;
using Breaku.Domain;
using Breaku.Domain.Rules;

namespace Breaku.Application.Perfil;

/// <summary>
/// HU29: el estudiante elimina su propia cuenta. Pide el texto de confirmación y reautenticación (contraseña, o un nuevo
/// login con Google si la cuenta lo tiene enlazado); no deja eliminar al ADMIN ni con una SUSPENSION vigente. El borrado
/// reutiliza la purga de HU28 con <c>eliminarCuenta: true</c>, en una sola transacción.
/// </summary>
public class EliminarCuentaService
{
    private readonly IPerfilRepository _perfil;
    private readonly ISancionRepository _sanciones;
    private readonly PurgaUsuarioService _purga;
    private readonly ITransaccion _transaccion;
    private readonly IHasherContrasenas _hasher;
    private readonly IVerificadorGoogle _google;
    private readonly IValidadorSesion _validador;
    private readonly TimeProvider _reloj;

    public EliminarCuentaService(IPerfilRepository perfil, ISancionRepository sanciones, PurgaUsuarioService purga,
        ITransaccion transaccion, IHasherContrasenas hasher, IVerificadorGoogle google, IValidadorSesion validador, TimeProvider reloj)
    {
        _perfil = perfil;
        _sanciones = sanciones;
        _purga = purga;
        _transaccion = transaccion;
        _hasher = hasher;
        _google = google;
        _validador = validador;
        _reloj = reloj;
    }

    public async Task EliminarAsync(int usuarioId, EliminarCuentaRequest req, CancellationToken ct = default)
    {
        var usuario = await _perfil.ObtenerUsuarioAsync(usuarioId, ct)
                      ?? throw new ReglaNegocioException("NO_ENCONTRADO", "No encontramos tu cuenta.");

        if (ReglasEliminarCuenta.ValidarRol(usuario.Rol) is { } rolNoPermitido)
            throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorNoEliminable, rolNoPermitido);
        if (ReglasEliminarCuenta.ValidarConfirmacion(req.Confirmacion) is { } sinConfirmar)
            throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorConfirmacion, sinConfirmar);

        await ReautenticarAsync(usuario, req, ct);

        ResultadoPurga? purga = null;
        await _transaccion.EjecutarAsync(async () =>
        {
            // Con la fila bloqueada, una sanción que llegue a la vez no se pisa con el borrado.
            await _sanciones.BloquearUsuarioAsync(usuarioId, ct);
            var registros = (await _sanciones.ListarSancionesAsync(usuarioId, ct)).Select(s => s.Registro).ToList();
            if (ReglasEliminarCuenta.TieneSancionActiva(registros, _reloj.GetUtcNow().UtcDateTime))
                throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorSancionActiva, ReglasEliminarCuenta.MensajeSancionActiva);

            purga = await _purga.PurgarAsync(usuarioId, eliminarCuenta: true, ct);
            return 0;
        }, ct);

        // Ya confirmado: el token deja de servir en la siguiente petición (el usuario ya no existe).
        _validador.Invalidar(usuarioId);
        if (purga is not null)
        {
            try { await _purga.AvisarAsync(purga, ct); }
            catch (Exception) { /* el borrado ya se confirmó: un fallo al avisar en vivo no lo deshace ni debe responder error */ }
        }
    }

    /// <summary>Contraseña o login de Google; si la cuenta tiene las dos, sirve cualquiera. No toca la base.</summary>
    private async Task ReautenticarAsync(Usuario usuario, EliminarCuentaRequest req, CancellationToken ct)
    {
        if (!ReglasEliminarCuenta.HayCredencial(req.Contrasena, req.CredencialGoogle))
            throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorConfirmacion, ReglasEliminarCuenta.MensajeFaltaReautenticacion);

        if (!string.IsNullOrEmpty(req.Contrasena))
        {
            if (_hasher.Verificar(req.Contrasena, usuario.PasswordHash)) return;
            // Si además mandó Google, se intenta con él antes de rechazar.
            if (string.IsNullOrWhiteSpace(req.CredencialGoogle))
                throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorContrasenaIncorrecta, ReglasEliminarCuenta.MensajeContrasenaIncorrecta);
        }

        if (usuario.GoogleSub is null || !_google.Configurado)
            throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorGoogleIncorrecto,
                _google.Configurado ? ReglasEliminarCuenta.MensajeGoogleIncorrecto : ReglasEliminarCuenta.MensajeGoogleNoDisponible);

        var identidad = await _google.VerificarAsync(req.CredencialGoogle!, ct);
        if (identidad is not { CorreoVerificado: true } || identidad.Sub != usuario.GoogleSub)
            throw new ReglaNegocioException(ReglasEliminarCuenta.ErrorGoogleIncorrecto, ReglasEliminarCuenta.MensajeGoogleIncorrecto);
    }
}
