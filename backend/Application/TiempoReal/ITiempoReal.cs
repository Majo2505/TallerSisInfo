namespace Breaku.Application.TiempoReal;

/// <summary>
/// HU18: avisa a la app abierta que algo cambió, para que se actualice sin recargar. Solo manda la señal (sin datos):
/// cada cliente vuelve a pedir lo suyo a la API, que aplica los permisos de siempre. Si el aviso en vivo falla, no se
/// pierde nada: la campanita igual consulta cada 15 segundos.
/// </summary>
public interface ITiempoReal
{
    /// <summary>La campanita de estos estudiantes tiene algo nuevo (o algo que ya no va).</summary>
    Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default);

    /// <summary>Hay una comunidad nueva: Descubrir se recarga en la sede de quien la creó y llega la invitación a los invitados.</summary>
    Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default);

    /// <summary>Cambiaron los avisos de quienes están en esta comunidad (invitaciones nuevas, se llenó, se reabrió, terminó).</summary>
    Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default);
}

/// <summary>Sin tiempo real (pruebas): no hace nada.</summary>
public sealed class SinTiempoReal : ITiempoReal
{
    public static readonly SinTiempoReal Instancia = new();

    public Task AvisosCambiaronAsync(IReadOnlyCollection<int> usuarioIds, CancellationToken ct = default) => Task.CompletedTask;
    public Task ComunidadCreadaAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
    public Task AvisosDeComunidadAsync(int comunidadId, CancellationToken ct = default) => Task.CompletedTask;
}
