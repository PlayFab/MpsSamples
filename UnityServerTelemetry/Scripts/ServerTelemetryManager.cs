using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates server metrics collection and telemetry sending.
/// Attach to a GameObject in your server scene. Metrics are collected at a
/// configurable interval and sent to the PlayFab Telemetry API in batches.
///
/// Telemetry key can be provided via:
///   1. MPS managed secrets (env var PF_MPS_SECRET_TelemetryKey) — recommended for production
///   2. Inspector field — for local testing / hardcoded
///
/// Title ID can be provided via:
///   1. GSDK config (if GSDK is integrated)
///   2. Inspector field — fallback
/// </summary>
public class ServerTelemetryManager : MonoBehaviour
{
    [Header("PlayFab Configuration")]
    [Tooltip("PlayFab Title ID. If empty, reads from env var PF_TITLE_ID.")]
    public string titleId;

    [Tooltip("PlayFab Telemetry Key. If empty, reads from env var PF_MPS_SECRET_TelemetryKey.")]
    public string telemetryKey;

    [Tooltip("Server ID used as the telemetry entity identifier. If empty, uses machine name.")]
    public string serverId;

    [Header("Collection Settings")]
    [Tooltip("How often to collect a metrics snapshot (seconds).")]
    public float collectionIntervalSeconds = 30f;

    [Tooltip("How often to send buffered metrics to PlayFab (seconds).")]
    public float sendIntervalSeconds = 60f;

    [Tooltip("Maximum buffered snapshots. Oldest are dropped when full.")]
    public int maxBufferSize = 500;

    static ServerTelemetryManager _instance;

    ServerMetricsCollector _collector;
    PlayFabTelemetrySender _sender;
    List<Dictionary<string, object>> _buffer = new List<Dictionary<string, object>>();
    bool _isRunning;

    void Awake()
    {
        // Singleton guard — prevent duplicates across scene loads
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ResolveConfiguration();

        if (string.IsNullOrEmpty(titleId) || string.IsNullOrEmpty(telemetryKey))
        {
            Debug.LogWarning("[ServerTelemetryManager] Missing titleId or telemetryKey. " +
                "Call Initialize(titleId, telemetryKey) to start telemetry later.");
            return;
        }

        InitializeInternal();
    }

    /// <summary>
    /// Initialize telemetry with explicit configuration.
    /// Use this when titleId/telemetryKey are not available at Start() time
    /// (e.g. when reading from GSDK config in OnServerActive).
    /// </summary>
    public void Initialize(string titleId, string telemetryKey, string serverId = null)
    {
        this.titleId = titleId;
        this.telemetryKey = telemetryKey;
        if (!string.IsNullOrEmpty(serverId)) this.serverId = serverId;
        InitializeInternal();
    }

    void InitializeInternal()
    {
        if (_sender != null) return; // already initialized

        if (string.IsNullOrEmpty(serverId))
        {
            serverId = Environment.MachineName;
        }

        _collector = gameObject.AddComponent<ServerMetricsCollector>();
        _sender = new PlayFabTelemetrySender(titleId, telemetryKey, serverId);

        Debug.Log($"[ServerTelemetryManager] Initialized — titleId={titleId}, serverId={serverId}, " +
                  $"collect every {collectionIntervalSeconds}s, send every {sendIntervalSeconds}s");

        StartTelemetry();
    }

    /// <summary>
    /// Starts the collection and sending loops.
    /// </summary>
    public void StartTelemetry()
    {
        if (_isRunning || _sender == null) return;
        _isRunning = true;
        StartCoroutine(CollectionLoop());
        StartCoroutine(SendLoop());
    }

    /// <summary>
    /// Stops the collection and sending loops.
    /// Note: buffered metrics that haven't been sent will be lost.
    /// For graceful shutdown, call this from your GSDK shutdown callback.
    /// </summary>
    public void StopTelemetry()
    {
        if (!_isRunning) return;
        _isRunning = false;
        StopAllCoroutines();
    }

    /// <summary>
    /// Set game-specific metrics from your networking code.
    /// Call this whenever player count or network object count changes.
    /// </summary>
    public void SetGameMetrics(int connectedPlayers, int maxPlayers, int networkObjectCount = 0)
    {
        if (_collector == null) return;
        _collector.ConnectedPlayers = connectedPlayers;
        _collector.MaxPlayers = maxPlayers;
        _collector.NetworkObjectCount = networkObjectCount;
    }

    IEnumerator CollectionLoop()
    {
        while (_isRunning)
        {
            yield return new WaitForSecondsRealtime(collectionIntervalSeconds);

            if (_collector != null)
            {
                var metrics = _collector.CollectMetrics();
                _buffer.Add(metrics);

                // Drop oldest if buffer exceeds max size
                while (_buffer.Count > maxBufferSize)
                {
                    _buffer.RemoveAt(0);
                }
            }
        }
    }

    IEnumerator SendLoop()
    {
        while (_isRunning)
        {
            yield return new WaitForSecondsRealtime(sendIntervalSeconds);

            if (_buffer.Count > 0 && _sender != null)
            {
                // Swap buffer so collection can continue during send
                var toSend = _buffer;
                _buffer = new List<Dictionary<string, object>>();
                yield return StartCoroutine(_sender.SendMetrics(toSend));
            }
        }
    }

    void OnDestroy()
    {
        _isRunning = false;
        if (_instance == this) _instance = null;
    }

    void ResolveConfiguration()
    {
        // Telemetry key: prefer MPS managed secret, fall back to Inspector value
        if (string.IsNullOrEmpty(telemetryKey))
        {
            string envKey = Environment.GetEnvironmentVariable("PF_MPS_SECRET_TelemetryKey");
            if (!string.IsNullOrEmpty(envKey))
            {
                telemetryKey = envKey;
                Debug.Log("[ServerTelemetryManager] Telemetry key loaded from MPS secret");
            }
        }

        // Title ID: try env var, fall back to Inspector value
        if (string.IsNullOrEmpty(titleId))
        {
            string envTitleId = Environment.GetEnvironmentVariable("PF_TITLE_ID");
            if (!string.IsNullOrEmpty(envTitleId))
            {
                titleId = envTitleId;
                Debug.Log("[ServerTelemetryManager] Title ID loaded from environment variable");
            }
        }

        // Server ID: try env var, fall back to machine name
        if (string.IsNullOrEmpty(serverId))
        {
            string envServerId = Environment.GetEnvironmentVariable("PF_SERVER_ID");
            if (!string.IsNullOrEmpty(envServerId))
            {
                serverId = envServerId;
            }
        }
    }
}
