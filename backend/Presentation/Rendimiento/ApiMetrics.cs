using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Breaku.Presentation.Rendimiento;

public sealed record ApiMetricSnapshot(long Requests, long Errors, double AverageMilliseconds, double P50Milliseconds, double P95Milliseconds);

public sealed class ApiMetrics
{
    private const int VentanaDuraciones = 1000;
    private readonly ConcurrentDictionary<string, EndpointMetrics> _endpoints = new(StringComparer.OrdinalIgnoreCase);

    public static long Start() => Stopwatch.GetTimestamp();

    public void Record(string method, string route, int statusCode, long startTimestamp)
    {
        var milliseconds = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        _endpoints.GetOrAdd($"{method} {route}", _ => new EndpointMetrics()).Record(statusCode >= 500, milliseconds);
    }

    public IReadOnlyDictionary<string, ApiMetricSnapshot> Snapshot()
        => _endpoints.ToDictionary(pair => pair.Key, pair => pair.Value.Snapshot(), StringComparer.OrdinalIgnoreCase);

    public string ToPrometheus()
    {
        var output = new StringBuilder();
        output.AppendLine("# HELP breaku_http_requests_total Total HTTP requests by route.");
        output.AppendLine("# TYPE breaku_http_requests_total counter");
        output.AppendLine("# HELP breaku_http_errors_total HTTP 5xx responses by route.");
        output.AppendLine("# TYPE breaku_http_errors_total counter");
        output.AppendLine("# HELP breaku_http_request_duration_ms HTTP request duration in milliseconds (avg, p50, p95 over the last 1000 requests).");
        output.AppendLine("# TYPE breaku_http_request_duration_ms gauge");
        foreach (var (key, metric) in Snapshot().OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var route = key.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
            output.AppendLine($"breaku_http_requests_total{{route=\"{route}\"}} {Format(metric.Requests)}");
            output.AppendLine($"breaku_http_errors_total{{route=\"{route}\"}} {Format(metric.Errors)}");
            output.AppendLine($"breaku_http_request_duration_ms{{route=\"{route}\",stat=\"avg\"}} {Format(metric.AverageMilliseconds)}");
            output.AppendLine($"breaku_http_request_duration_ms{{route=\"{route}\",stat=\"p50\"}} {Format(metric.P50Milliseconds)}");
            output.AppendLine($"breaku_http_request_duration_ms{{route=\"{route}\",stat=\"p95\"}} {Format(metric.P95Milliseconds)}");
        }
        return output.ToString();
    }

    private static string Format(IFormattable value) => value.ToString(null, CultureInfo.InvariantCulture);

    private sealed class EndpointMetrics
    {
        private readonly object _sync = new();
        private readonly Queue<double> _recentDurations = new();
        private long _requests;
        private long _errors;
        private double _totalMilliseconds;

        public void Record(bool error, double milliseconds)
        {
            lock (_sync)
            {
                _requests++;
                if (error) _errors++;
                _totalMilliseconds += milliseconds;
                _recentDurations.Enqueue(milliseconds);
                while (_recentDurations.Count > VentanaDuraciones) _recentDurations.Dequeue();
            }
        }

        public ApiMetricSnapshot Snapshot()
        {
            lock (_sync)
            {
                var durations = _recentDurations.Order().ToArray();
                var p95Index = durations.Length == 0 ? 0 : (int)Math.Ceiling(durations.Length * 0.95) - 1;
                return new ApiMetricSnapshot(
                    _requests,
                    _errors,
                    _requests == 0 ? 0 : _totalMilliseconds / _requests,
                    durations.Length == 0 ? 0 : durations[durations.Length / 2],
                    durations.Length == 0 ? 0 : durations[p95Index]);
            }
        }
    }
}
