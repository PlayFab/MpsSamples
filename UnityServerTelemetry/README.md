# PlayFab MPS — Unity Server Telemetry Sample

## Overview

This sample shows how to collect game server performance metrics from a Unity dedicated server and send them to the [PlayFab Telemetry API](https://learn.microsoft.com/en-us/rest/api/playfab/events/play-stream-events/write-telemetry-events) using a [telemetry key](https://learn.microsoft.com/en-us/gaming/playfab/data-analytics/ingest-data/telemetry-keys-overview).

It consists of three scripts that you can drop into any Unity game server project that uses PlayFab Multiplayer Servers (MPS).

## Metrics Collected

| Category | Metric | Source | Notes |
|----------|--------|--------|-------|
| Simulation | Update loop rate (fps) | Frame counting | Not render FPS — server simulation loop rate |
| Simulation | Avg frame time (ms) | `Time.unscaledDeltaTime` | Wall-clock frame duration, averaged over window |
| Simulation | Max frame time (ms) | `Time.unscaledDeltaTime` | Spike detection |
| Simulation | Fixed tick rate | FixedUpdate counting | Physics simulation rate |
| Memory | Total used (MB) | `ProfilerRecorder` | All Unity memory |
| Memory | GC used (MB) | `ProfilerRecorder` | Managed heap in use |
| Memory | GC reserved (MB) | `ProfilerRecorder` | Managed heap reserved |
| Memory | GC alloc/frame (bytes) | `ProfilerRecorder` | Per-frame allocation pressure |
| Memory | Mono heap (MB) | `Profiler` API | Mono backend |
| Memory | Mono used (MB) | `Profiler` API | Mono backend |
| CPU | CPU usage % | `System.Diagnostics.Process` | Best-effort; -1 if unavailable |
| CPU | GC gen 0/1/2 counts | `GC.CollectionCount()` | Cumulative since process start |
| CPU | Thread count | `System.Diagnostics.Process` | Best-effort; -1 if unavailable |
| Game | Connected players | Set externally | From your networking layer |
| Game | Max players | Set externally | Server capacity |
| Game | Server uptime (s) | `Time.realtimeSinceStartup` | Since process start |
| Game | Network objects | Set externally | Active networked entities |

## Setup

### 1. Add the scripts to your project

Copy the `Scripts/` folder into your Unity server project's `Assets/` directory:
- `ServerMetricsCollector.cs`
- `PlayFabTelemetrySender.cs`
- `ServerTelemetryManager.cs`

### 2. Add to your scene

Create an empty GameObject in your server scene and attach the `ServerTelemetryManager` component.

### 3. Configure the telemetry key

You need a PlayFab telemetry key. Create one in **PlayFab Game Manager → Data → Telemetry Keys**.

There are two ways to provide the key to your game server:

#### Option A: MPS Managed Secrets (recommended for production)

Use the [MPS secret management feature](https://learn.microsoft.com/en-us/gaming/playfab/multiplayer/servers/manage-secrets):

1. Upload the telemetry key as a secret named `TelemetryKey` using the `UploadSecret` API
2. Reference it in your build via `GameSecretReferences`
3. The game server reads it automatically from the `PF_MPS_SECRET_TelemetryKey` environment variable

#### Option B: Hardcode in Inspector (for local testing)

Set the `telemetryKey` field directly on the `ServerTelemetryManager` component in the Inspector.

### 4. Configure the Title ID

The `titleId` field can be:
- Set in the Inspector
- Or read from the `PF_TITLE_ID` environment variable (e.g. from GSDK config)

## Integration with GSDK

If your server already uses the PlayFab GSDK (like the [UnityMirror sample](../UnityMirror/)), you can wire the telemetry manager into your GSDK lifecycle:

```csharp
public class AgentListener : MonoBehaviour
{
    public ServerTelemetryManager telemetryManager;

    private List<ConnectedPlayer> _connectedPlayers;

    void Start()
    {
        _connectedPlayers = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();

        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;

        // ... other GSDK setup ...

        StartCoroutine(ReadyForPlayers());
    }

    private void OnServerActive()
    {
        // Start your networking server...
        UNetServer.StartListen();

        // Read title ID from GSDK config if needed
        var config = PlayFabMultiplayerAgentAPI.GetConfigSettings();
        if (config.ContainsKey("titleId"))
        {
            telemetryManager.titleId = config["titleId"];
        }

        // Telemetry starts automatically in ServerTelemetryManager.Start()
    }

    private void OnPlayerAdded(string playfabId)
    {
        _connectedPlayers.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);

        // Update telemetry with current player count
        telemetryManager.SetGameMetrics(_connectedPlayers.Count, 32);
    }

    private void OnPlayerRemoved(string playfabId)
    {
        // ... remove player ...
        telemetryManager.SetGameMetrics(_connectedPlayers.Count, 32);
    }

    private void OnShutdown()
    {
        telemetryManager.StopTelemetry();
        // ... shutdown logic ...
    }
}
```

## How It Works

1. **ServerTelemetryManager** starts on `Start()` and resolves configuration from MPS secrets / environment variables / Inspector fields
2. Every `collectionIntervalSeconds` (default: 30s), it calls `ServerMetricsCollector.CollectMetrics()` to take a snapshot
3. Snapshots are buffered in memory
4. Every `sendIntervalSeconds` (default: 60s), buffered snapshots are sent to `POST https://{titleId}.playfabapi.com/Event/WriteTelemetryEvents` with the `X-TelemetryKey` header
5. Each snapshot becomes one telemetry event with namespace `custom.server_telemetry` and name `server_metrics`

## Configuration

| Field | Default | Description |
|-------|---------|-------------|
| `titleId` | (empty) | PlayFab Title ID |
| `telemetryKey` | (empty) | PlayFab Telemetry Key |
| `serverId` | Machine name | Identifier for this server instance |
| `collectionIntervalSeconds` | 30 | How often to collect metrics |
| `sendIntervalSeconds` | 60 | How often to flush buffered metrics to PlayFab |

## Limitations

- **CPU metrics** (`cpuUsagePercent`, `threadCount`) use `System.Diagnostics.Process` which may not be available on all platforms (especially IL2CPP). These fields report `-1` when unavailable.
- **ProfilerRecorder** counters may not be available in all build configurations. Unavailable counters report `-1`.
- **Mono heap metrics** are most useful with the Mono scripting backend; values may be limited on IL2CPP.
- This is sample code — for production use, consider adding retry logic with exponential backoff, bounded queues, and event deduplication.
