using Breaku.Infrastructure.Performance;

public class PerformanceMetricsTests
{
    [Fact]
    public void Snapshot_includes_average_and_p95_for_requests()
    {
        var metrics = new ApiMetrics();
        metrics.Start("GET", "/health").Complete(200);
        metrics.Start("GET", "/health").Complete(500);

        var snapshot = metrics.Snapshot()["GET /health"];

        Assert.Equal(2, snapshot.Requests);
        Assert.Equal(1, snapshot.Errors);
        Assert.True(snapshot.AverageMilliseconds >= 0);
        Assert.True(snapshot.P50Milliseconds >= 0);
        Assert.True(snapshot.P95Milliseconds >= 0);
    }
}
