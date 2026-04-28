namespace PlayFab.Samples.UnityMpsTelemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using UnityEngine;
    using UnityEngine.Profiling;
    using Debug = UnityEngine.Debug;

    public sealed class UnityServerMetricsCollector : MonoBehaviour
    {
        private readonly List<Func<IDictionary<string, object>>> _customMetricsProviders =
            new List<Func<IDictionary<string, object>>>();

        private Process _process;
        private DateTime _startedUtc;
        private DateTime _lastCpuSampleUtc;
        private TimeSpan _lastTotalProcessorTime;
        private float _sampleTimer;
        private float _frameDeltaSecondsTotal;
        private float _maxFrameDeltaSeconds;
        private int _frameCount;
        private int _longFrameCount;
        private double _lastCpuPercent;
        private long _lastWorkingSetBytes;
        private long _lastManagedHeapBytes;
        private long _lastUnityAllocatedBytes;
        private long _lastUnityReservedBytes;
        private long _lastUnityMonoUsedBytes;
        private int _lastGen0Collections;
        private int _lastGen1Collections;
        private int _lastGen2Collections;

        public void Initialize()
        {
            _process = Process.GetCurrentProcess();
            _startedUtc = DateTime.UtcNow;
            _lastCpuSampleUtc = _startedUtc;
            _lastTotalProcessorTime = _process.TotalProcessorTime;
            SampleProcessMetrics();
        }

        public void RegisterCustomMetricsProvider(Func<IDictionary<string, object>> provider)
        {
            // Use this hook for game-specific metrics that only your server knows about:
            // active players, connections, packets, replication backlog, match state, tick timing, etc.
            // Keep names stable and low-cardinality so telemetry stays easy to query and inexpensive.
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _customMetricsProviders.Add(provider);
        }

        public Dictionary<string, object> CaptureSummaryAndReset()
        {
            SampleProcessMetrics();

            float averageFrameMs = _frameCount == 0
                ? 0f
                : (_frameDeltaSecondsTotal / _frameCount) * 1000f;

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "uptimeSeconds", Math.Round((DateTime.UtcNow - _startedUtc).TotalSeconds, 3) },
                { "targetFrameRate", Application.targetFrameRate },
                { "frameCount", _frameCount },
                { "averageFrameMs", Math.Round(averageFrameMs, 3) },
                { "maxFrameMs", Math.Round(_maxFrameDeltaSeconds * 1000f, 3) },
                { "longFrameCount", _longFrameCount },
                { "longFrameThresholdMs", TelemetrySampleConfig.LongFrameThresholdMilliseconds },
                { "processCpuPercent", Math.Round(_lastCpuPercent, 3) },
                { "processorCount", Environment.ProcessorCount },
                { "processWorkingSetBytes", _lastWorkingSetBytes },
                { "managedHeapBytes", _lastManagedHeapBytes },
                { "unityAllocatedMemoryBytes", _lastUnityAllocatedBytes },
                { "unityReservedMemoryBytes", _lastUnityReservedBytes },
                { "unityMonoUsedMemoryBytes", _lastUnityMonoUsedBytes },
                { "gcGen0Collections", GC.CollectionCount(0) - _lastGen0Collections },
                { "gcGen1Collections", GC.CollectionCount(1) - _lastGen1Collections },
                { "gcGen2Collections", GC.CollectionCount(2) - _lastGen2Collections }
            };

            AddCustomMetrics(payload);
            ResetIntervalCounters();
            return payload;
        }

        private void Update()
        {
            float deltaSeconds = Time.unscaledDeltaTime;
            _frameCount++;
            _frameDeltaSecondsTotal += deltaSeconds;
            _maxFrameDeltaSeconds = Mathf.Max(_maxFrameDeltaSeconds, deltaSeconds);

            if (deltaSeconds * 1000f >= TelemetrySampleConfig.LongFrameThresholdMilliseconds)
            {
                _longFrameCount++;
            }

            _sampleTimer += deltaSeconds;
            if (_sampleTimer >= TelemetrySampleConfig.MetricsSampleIntervalSeconds)
            {
                _sampleTimer = 0f;
                SampleProcessMetrics();
            }
        }

        private void OnDestroy()
        {
            if (_process != null)
            {
                _process.Dispose();
                _process = null;
            }
        }

        private void SampleProcessMetrics()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                _process.Refresh();
                DateTime now = DateTime.UtcNow;
                TimeSpan totalProcessorTime = _process.TotalProcessorTime;
                double processorTimeMs = (totalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                double wallClockMs = (now - _lastCpuSampleUtc).TotalMilliseconds;

                if (wallClockMs > 0)
                {
                    _lastCpuPercent = (processorTimeMs / (wallClockMs * Environment.ProcessorCount)) * 100d;
                }

                _lastCpuSampleUtc = now;
                _lastTotalProcessorTime = totalProcessorTime;
                _lastWorkingSetBytes = _process.WorkingSet64;
                _lastManagedHeapBytes = GC.GetTotalMemory(false);
                _lastUnityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
                _lastUnityReservedBytes = Profiler.GetTotalReservedMemoryLong();
                _lastUnityMonoUsedBytes = Profiler.GetMonoUsedSizeLong();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to sample process metrics: " + ex.Message);
            }
        }

        private void ResetIntervalCounters()
        {
            _frameDeltaSecondsTotal = 0f;
            _maxFrameDeltaSeconds = 0f;
            _frameCount = 0;
            _longFrameCount = 0;
            _lastGen0Collections = GC.CollectionCount(0);
            _lastGen1Collections = GC.CollectionCount(1);
            _lastGen2Collections = GC.CollectionCount(2);
        }

        private void AddCustomMetrics(IDictionary<string, object> payload)
        {
            foreach (Func<IDictionary<string, object>> provider in _customMetricsProviders)
            {
                // The provider should read from your actual game/networking stack. For interval
                // counters, reset them inside your provider after returning the current values.
                IDictionary<string, object> customMetrics;
                try
                {
                    customMetrics = provider();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    continue;
                }

                if (customMetrics == null)
                {
                    Debug.LogError("Custom metrics provider returned null.");
                    continue;
                }

                foreach (KeyValuePair<string, object> metric in customMetrics)
                {
                    if (string.IsNullOrWhiteSpace(metric.Key))
                    {
                        Debug.LogError("Custom metrics provider returned an empty metric name.");
                        continue;
                    }

                    if (payload.ContainsKey(metric.Key))
                    {
                        Debug.LogError("Custom metric '" + metric.Key + "' conflicts with a built-in metric name.");
                        continue;
                    }

                    payload.Add(metric.Key, metric.Value);
                }
            }
        }
    }
}
