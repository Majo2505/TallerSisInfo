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
    Task<bool> PuedeUsarApiAsync(int usuarioId, CancellationToken ct = default);

    /// <summary>Olvida lo guardado de este usuario (se llama al sancionar o levantar).</summary>
    void Invalidar(int usuarioId);
}

public sealed class ValidadorSesion : IValidadorSesion
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromSeconds(60);
    private const int MaximoEntradas = 10_000;

    private readonly Func<int, DateTime, CancellationToken, Task<EstadoSesion?>> _lector;
    private readonly TimeProvider _reloj;
    private readonly ConcurrentDictionary<int, (bool Permitido, DateTime Vence)> _cache = new();

    // lector: lee el estado del usuario en la base (null si no existe).
    public ValidadorSesion(Func<int, DateTime, CancellationToken, Task<EstadoSesion?>> lector, TimeProvider reloj)
    {
        _lector = lector;
        _reloj = reloj;
    }

    public async Task<bool> PuedeUsarApiAsync(int usuarioId, CancellationToken ct = default)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        if (_cache.TryGetValue(usuarioId, out var guardado) && guardado.Vence > ahora)
            return guardado.Permitido;

        var estado = await _lector(usuarioId, ahora, ct);
        var permitido = estado is { Existe: true, Bloqueo: null };

        if (_cache.Count >= MaximoEntradas)
            foreach (var vencida in _cache.Where(e => e.Value.Vence <= ahora).Select(e => e.Key).ToList())
                _cache.TryRemove(vencida, out _);
        _cache[usuarioId] = (permitido, ahora + Vigencia);
        return permitido;
    }

    public void Invalidar(int usuarioId) => _cache.TryRemove(usuarioId, out _);
}
