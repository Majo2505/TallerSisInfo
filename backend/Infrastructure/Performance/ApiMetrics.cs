using System.Collections.Concurrent;
using System.Diagnostics;

namespace Breaku.Infrastructure.Performance;

public sealed record ApiMetricSnapshot(long Requests, long Errors, double AverageMilliseconds, double P50Milliseconds, double P95Milliseconds);

public sealed class ApiMetrics
{
    private readonly ConcurrentDictionary<string, EndpointMetrics> _endpoints = new(StringComparer.OrdinalIgnoreCase);

    public RequestMeasurement Start(string method, string path)
    {
        var endpoint = _endpoints.GetOrAdd($"{method} {path}", _ => new EndpointMetrics());
        return new RequestMeasurement(endpoint);
    }

    public IReadOnlyDictionary<string, ApiMetricSnapshot> Snapshot()
        => _endpoints.ToDictionary(pair => pair.Key, pair => pair.Value.Snapshot(), StringComparer.OrdinalIgnoreCase);

    public sealed class RequestMeasurement
    {
        private readonly EndpointMetrics _endpoint;
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        internal RequestMeasurement(EndpointMetrics endpoint) => _endpoint = endpoint;

        public void Complete(int statusCode)
        {
            _endpoint.Record(statusCode >= 500, _clock.Elapsed.TotalMilliseconds);
        }
    }

    private sealed class EndpointMetrics
    {
        private readonly object _sync = new();
        private long _requests;
        private long _errors;
        private double _totalMilliseconds;
        private readonly Queue<double> _recentDurations = new();

        public void Record(bool error, double milliseconds)
        {
            lock (_sync)
            {
                _requests++;
                if (error) _errors++;
                _totalMilliseconds += milliseconds;
                _recentDurations.Enqueue(milliseconds);
                while (_recentDurations.Count > 1000) _recentDurations.Dequeue();
            }
        }

        public ApiMetricSnapshot Snapshot()
        {
            lock (_sync)
            {
                var durations = _recentDurations.OrderBy(value => value).ToArray();
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
