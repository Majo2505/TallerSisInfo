namespace Breaku.Application;

public interface IDatabaseHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
