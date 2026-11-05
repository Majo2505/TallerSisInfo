using Breaku.Infrastructure.Performance;

namespace Breaku.Tests;

public class PerformanceMetricsTests
{
    [Fact]
    public void Snapshot_includes_average_and_p95_for_requests()
    {
        var metrics = new ApiMetrics();
        metrics.Record("GET", "/health", 200, ApiMetrics.Start());
        metrics.Record("GET", "/health", 500, ApiMetrics.Start());

        var snapshot = metrics.Snapshot()["GET /health"];

        Assert.Equal(2, snapshot.Requests);
        Assert.Equal(1, snapshot.Errors);
        Assert.True(snapshot.AverageMilliseconds >= 0);
        Assert.True(snapshot.P50Milliseconds >= 0);
        Assert.True(snapshot.P95Milliseconds >= snapshot.P50Milliseconds);
    }

    [Fact]
    public void Prometheus_output_lists_requests_errors_and_latency_per_route()
    {
        var metrics = new ApiMetrics();
        metrics.Record("POST", "api/recomendaciones", 200, ApiMetrics.Start());
        metrics.Record("POST", "api/recomendaciones", 503, ApiMetrics.Start());

        var text = metrics.ToPrometheus();

        Assert.Contains("breaku_http_requests_total{route=\"POST api/recomendaciones\"} 2", text);
        Assert.Contains("breaku_http_errors_total{route=\"POST api/recomendaciones\"} 1", text);
        Assert.Contains("breaku_http_request_duration_ms{route=\"POST api/recomendaciones\",stat=\"p95\"}", text);
    }

    [Fact]
    public void Routes_are_grouped_by_template_so_the_metrics_stay_bounded()
    {
        var metrics = new ApiMetrics();
        foreach (var _ in Enumerable.Range(1, 50))
            metrics.Record("GET", "api/comunidades/{id}/participantes", 200, ApiMetrics.Start());

        Assert.Single(metrics.Snapshot());
    }
}
