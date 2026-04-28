namespace PlayFab.Samples.UnityMpsTelemetry
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class MpsTelemetryServerBootstrap : MonoBehaviour
    {
        private PlayFabTelemetryClient _telemetryClient;
        private UnityServerMetricsCollector _metricsCollector;
        private bool _initialized;
        private bool _shutdownStarted;

        private void Awake()
        {
            EnsureComponentsInitialized();
        }

        public void InitializeForMps(string titleId)
        {
            EnsureComponentsInitialized();

            // Call this from your GSDK startup flow after reading MPS config.
            // The title ID should come from GSDK config settings; this sample does not
            // reference GSDK directly so developers can integrate the official GSDK themselves.
            if (_initialized)
            {
                Debug.LogWarning("MPS telemetry sample is already initialized.");
                return;
            }

            _telemetryClient.Configure(
                GetRequiredTitleId(titleId),
                TelemetrySampleConfig.TelemetryKey,
                TelemetrySampleConfig.EventNamespace,
                "unitympsserver");

            StartCoroutine(MetricsSummaryLoop());
            StartCoroutine(TelemetryFlushLoop());
            _initialized = true;
        }

        public void RegisterCustomMetricsProvider(Func<IDictionary<string, object>> provider)
        {
            EnsureComponentsInitialized();

            // Register this from your game server bootstrap before InitializeForMps.
            // Example use: read active connections, bytes sent/received, packet loss, or
            // replication backlog from your real networking layer and add those fields to
            // the same PlayFab telemetry event as the built-in Unity/process metrics.
            _metricsCollector.RegisterCustomMetricsProvider(provider);
        }

        public void BeginShutdown()
        {
            // Fire-and-forget wrapper for GSDK shutdown callbacks that cannot yield a coroutine.
            // If your shutdown flow can wait on coroutines, yield FlushFinalMetricsAsync instead.
            StartCoroutine(FlushFinalMetricsAsync());
        }

        public IEnumerator FlushFinalMetricsAsync()
        {
            // Wire your GSDK shutdown callback here so the final metrics event has a chance
            // to flush before the process exits. This sample does not call Application.Quit;
            // the host server/GSDK bootstrap owns process lifetime.
            if (!_initialized)
            {
                Debug.LogWarning("MPS telemetry sample shutdown was requested before initialization.");
                yield break;
            }

            if (_shutdownStarted)
            {
                yield break;
            }

            yield return ShutdownAfterTelemetryFlush();
        }

        private void EnsureComponentsInitialized()
        {
            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);

            if (_telemetryClient != null)
            {
                return;
            }

            _telemetryClient = gameObject.AddComponent<PlayFabTelemetryClient>();
            _metricsCollector = gameObject.AddComponent<UnityServerMetricsCollector>();
            _metricsCollector.Initialize();
        }

        private IEnumerator MetricsSummaryLoop()
        {
            // Metrics are aggregated in process and emitted once per interval. This keeps
            // PlayFab event volume low compared with sending per-frame or per-packet telemetry.
            while (!_shutdownStarted)
            {
                yield return new WaitForSecondsRealtime(TelemetrySampleConfig.MetricsSummaryIntervalSeconds);
                EnqueueMetricsSummary("server_metrics_summary");
            }
        }

        private IEnumerator TelemetryFlushLoop()
        {
            // Flush queued events independently from collection so a temporary telemetry
            // failure does not block the game loop.
            while (!_shutdownStarted)
            {
                yield return new WaitForSecondsRealtime(TelemetrySampleConfig.TelemetryFlushIntervalSeconds);
                if (_shutdownStarted)
                {
                    yield break;
                }

                yield return _telemetryClient.FlushAsync();
            }
        }

        private IEnumerator ShutdownAfterTelemetryFlush()
        {
            _shutdownStarted = true;
            Debug.Log("MPS telemetry sample server is shutting down.");

            // Emit one final aggregate snapshot during graceful shutdown.
            EnqueueMetricsSummary("server_metrics_final");

            yield return _telemetryClient.FlushAllAsync(TelemetrySampleConfig.ShutdownFlushTimeoutSeconds);
            Debug.Log("MPS telemetry sample final metrics flush completed.");
        }

        private void EnqueueMetricsSummary(string eventName)
        {
            Dictionary<string, object> payload = _metricsCollector.CaptureSummaryAndReset();
            _telemetryClient.Enqueue(eventName, payload);
        }

        private static string GetRequiredTitleId(string titleId)
        {
            // The title ID should come from GSDK config when this server is running in MPS.
            if (!string.IsNullOrWhiteSpace(titleId))
            {
                return titleId;
            }

            string message = "Pass the PlayFab title ID from GSDK config when initializing telemetry.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
