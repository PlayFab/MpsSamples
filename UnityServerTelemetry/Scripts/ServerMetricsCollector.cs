using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

/// <summary>
/// Collects game server performance metrics using Unity's ProfilerRecorder,
/// Profiler API, GC, and System.Diagnostics.Process.
/// Attach to a GameObject — call CollectMetrics() periodically to get a snapshot.
/// </summary>
public class ServerMetricsCollector : MonoBehaviour
{
    // Game-specific metrics — set these from your networking/game code
    public int ConnectedPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int NetworkObjectCount { get; set; }

    // ProfilerRecorder instances for memory metrics
    ProfilerRecorder _totalUsedMemoryRecorder;
    ProfilerRecorder _gcUsedMemoryRecorder;
    ProfilerRecorder _gcReservedMemoryRecorder;
    ProfilerRecorder _gcAllocInFrameRecorder;

    // Frame time tracking
    int _frameCount;
    float _frameTimeSum;
    float _maxFrameTime;

    // Fixed tick tracking
    int _fixedUpdateCount;

    // CPU tracking (best-effort)
    TimeSpan _lastCpuTime;
    float _lastCpuCheckTime;
    float _cpuUsagePercent = -1f;
    bool _cpuMetricsAvailable = true;

    void OnEnable()
    {
        // ProfilerRecorder may not be available on all platforms/build configs
        try { _totalUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory"); } catch { }
        try { _gcUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory"); } catch { }
        try { _gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory"); } catch { }
        try { _gcAllocInFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); } catch { }

        ResetAccumulators();
        InitCpuTracking();
    }

    void OnDisable()
    {
        _totalUsedMemoryRecorder.Dispose();
        _gcUsedMemoryRecorder.Dispose();
        _gcReservedMemoryRecorder.Dispose();
        _gcAllocInFrameRecorder.Dispose();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _frameCount++;
        _frameTimeSum += dt;
        if (dt > _maxFrameTime) _maxFrameTime = dt;
    }

    void FixedUpdate()
    {
        _fixedUpdateCount++;
    }

    /// <summary>
    /// Collects all metrics into a dictionary and resets per-window accumulators.
    /// Call this at your desired collection interval (e.g. every 30 seconds).
    /// </summary>
    public Dictionary<string, object> CollectMetrics()
    {
        float elapsed = _frameTimeSum;
        var metrics = new Dictionary<string, object>();

        // Internal metadata (prefixed with _ so sender can use it but exclude from payload)
        metrics["_timestamp"] = DateTime.UtcNow.ToString("O");

        // Simulation
        metrics["updateLoopRate"] = elapsed > 0 ? Mathf.Round(_frameCount / elapsed * 10f) / 10f : 0f;
        metrics["avgFrameTimeMs"] = _frameCount > 0 ? Mathf.Round(_frameTimeSum / _frameCount * 1000f * 10f) / 10f : 0f;
        metrics["maxFrameTimeMs"] = Mathf.Round(_maxFrameTime * 1000f * 10f) / 10f;
        metrics["fixedTickRate"] = elapsed > 0 ? Mathf.Round(_fixedUpdateCount / elapsed * 10f) / 10f : 0f;

        // Memory (ProfilerRecorder values are in bytes)
        metrics["totalUsedMemoryMB"] = GetRecorderMB(_totalUsedMemoryRecorder);
        metrics["gcUsedMemoryMB"] = GetRecorderMB(_gcUsedMemoryRecorder);
        metrics["gcReservedMemoryMB"] = GetRecorderMB(_gcReservedMemoryRecorder);
        metrics["lastFrameGcAllocBytes"] = GetRecorderValue(_gcAllocInFrameRecorder);
        metrics["monoHeapSizeMB"] = Mathf.Round(Profiler.GetMonoHeapSizeLong() / (1024f * 1024f) * 10f) / 10f;
        metrics["monoUsedSizeMB"] = Mathf.Round(Profiler.GetMonoUsedSizeLong() / (1024f * 1024f) * 10f) / 10f;

        // CPU
        UpdateCpuUsage();
        metrics["cpuUsagePercent"] = _cpuUsagePercent;
        metrics["gcGen0Collections"] = GC.CollectionCount(0);
        metrics["gcGen1Collections"] = GC.CollectionCount(1);
        metrics["gcGen2Collections"] = GC.CollectionCount(2);
        metrics["threadCount"] = GetThreadCount();

        // Game
        metrics["connectedPlayers"] = ConnectedPlayers;
        metrics["maxPlayers"] = MaxPlayers;
        metrics["serverUptimeSeconds"] = Mathf.Round(Time.realtimeSinceStartup * 10f) / 10f;
        metrics["networkObjectCount"] = NetworkObjectCount;

        ResetAccumulators();
        return metrics;
    }

    void ResetAccumulators()
    {
        _frameCount = 0;
        _frameTimeSum = 0f;
        _maxFrameTime = 0f;
        _fixedUpdateCount = 0;
    }

    float GetRecorderMB(ProfilerRecorder recorder)
    {
        if (recorder.Valid && recorder.IsRunning)
            return Mathf.Round(recorder.LastValue / (1024f * 1024f) * 10f) / 10f;
        return -1f;
    }

    long GetRecorderValue(ProfilerRecorder recorder)
    {
        if (recorder.Valid && recorder.IsRunning)
            return recorder.LastValue;
        return -1;
    }

    // CPU tracking via System.Diagnostics.Process (best-effort, may not work on all platforms)
    void InitCpuTracking()
    {
        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                _lastCpuTime = proc.TotalProcessorTime;
                _lastCpuCheckTime = Time.realtimeSinceStartup;
            }
        }
        catch
        {
            _cpuMetricsAvailable = false;
            Debug.Log("[ServerMetricsCollector] CPU metrics unavailable on this platform");
        }
    }

    void UpdateCpuUsage()
    {
        if (!_cpuMetricsAvailable)
        {
            _cpuUsagePercent = -1f;
            return;
        }

        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                TimeSpan currentCpuTime = proc.TotalProcessorTime;
                float currentTime = Time.realtimeSinceStartup;
                float elapsedWall = currentTime - _lastCpuCheckTime;

                if (elapsedWall > 0.1f)
                {
                    double cpuElapsed = (currentCpuTime - _lastCpuTime).TotalSeconds;
                    int coreCount = Mathf.Max(1, SystemInfo.processorCount);
                    _cpuUsagePercent = Mathf.Round((float)(cpuElapsed / (elapsedWall * coreCount)) * 1000f) / 10f;
                    _cpuUsagePercent = Mathf.Clamp(_cpuUsagePercent, 0f, 100f);
                }

                _lastCpuTime = currentCpuTime;
                _lastCpuCheckTime = currentTime;
            }
        }
        catch
        {
            _cpuMetricsAvailable = false;
            _cpuUsagePercent = -1f;
        }
    }

    int GetThreadCount()
    {
        try
        {
            using (var proc = Process.GetCurrentProcess())
            {
                return proc.Threads.Count;
            }
        }
        catch
        {
            return -1;
        }
    }
}
