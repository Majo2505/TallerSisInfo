using System.Collections.Concurrent;
using Breaku.Domain.Rules;

namespace Breaku.Application.Sanciones;

/// <summary>
/// HU28: valida en cada petición (y en el token del hub de tiempo real) que el usuario siga pudiendo usar la plataforma:
/// que exista, esté activo y no tenga una suspensión vigente. El resultado se guarda ~60 s para no ir a la base en cada
/// llamada; al sancionar o levantar se invalida la entrada, así que en un solo servidor el cambio es inmediato (con
/// varios, tardaría hasta el minuto del caché).
/// </summary>
public interface IValidadorSesion
{
    /// <summary>Si puede usar la API y, cuando la cuenta está sancionada, por qué (para el cuerpo del 401).</summary>
    Task<ResultadoSesion> ValidarAsync(int usuarioId, CancellationToken ct = default);

    async Task<bool> PuedeUsarApiAsync(int usuarioId, CancellationToken ct = default) => (await ValidarAsync(usuarioId, ct)).Permitido;

    /// <summary>Olvida lo guardado de este usuario (se llama al sancionar o levantar).</summary>
    void Invalidar(int usuarioId);
}

/// <summary><c>Bloqueo</c> solo viene cuando la cuenta existe pero está suspendida o dada de baja; un usuario que ya no existe da <c>Permitido = false</c> sin bloqueo.</summary>
public sealed record ResultadoSesion(bool Permitido, ReglasSancion.BloqueoCuenta? Bloqueo = null)
{
    /// <summary>El texto del 401 por sanción: el mismo que ve quien intenta entrar (motivo y, si la hay, fecha de fin).</summary>
    public string? Mensaje => Bloqueo is null ? null : ReglasSancion.MensajeBloqueo(Bloqueo);
}

public sealed class ValidadorSesion : IValidadorSesion
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(60);
    private const int MaximoEntradas = 10_000;

    private readonly Func<int, DateTime, CancellationToken, Task<EstadoSesion?>> _lector;
    private readonly TimeProvider _reloj;
    private readonly ConcurrentDictionary<int, (ResultadoSesion Resultado, DateTime Vence)> _cache = new();

    // lector: lee el estado del usuario en la base (null si no existe).
    public ValidadorSesion(Func<int, DateTime, CancellationToken, Task<EstadoSesion?>> lector, TimeProvider reloj)
    {
        _lector = lector;
        _reloj = reloj;
    }

    public async Task<ResultadoSesion> ValidarAsync(int usuarioId, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        if (_cache.TryGetValue(usuarioId, out var guardado) && guardado.Vence > ahora)
            return guardado.Resultado;

        var estado = await _lector(usuarioId, ahora, ct);
        var resultado = estado is { Existe: true, Bloqueo: null } ? new ResultadoSesion(true) : new ResultadoSesion(false, estado?.Bloqueo);

        if (_cache.Count >= MaximoEntradas)
            foreach (var vencida in _cache.Where(e => e.Value.Vence <= ahora).Select(e => e.Key).ToList())
                _cache.TryRemove(vencida, out _);
        _cache[usuarioId] = (resultado, ahora + Vigencia);
        return resultado;
    }

    public async Task<bool> PuedeUsarApiAsync(int usuarioId, CancellationToken ct = default) => (await ValidarAsync(usuarioId, ct)).Permitido;

    public void Invalidar(int usuarioId) => _cache.TryRemove(usuarioId, out _);
}
