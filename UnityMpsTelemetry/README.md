# PlayFab MPS Unity telemetry sample

## Overview

This sample shows how a Unity dedicated server running on PlayFab Multiplayer Servers (MPS) can collect game-server health metrics and send them to PlayFab as telemetry events.

The sample is intentionally focused on metrics and telemetry. It does not include, reference, or wrap the Game Server SDK (GSDK). Game developers should import the official Unity GSDK into their own server project and wire these telemetry/metrics scripts into their existing MPS lifecycle code.

If you already have a Unity server project, you can use this as a scripts-only drop-in sample: copy `UnityServer\Assets\Server\Scripts` into your project's `Assets` folder, attach `MpsTelemetryServerBootstrap` to one persistent server GameObject, and call `InitializeForMps(titleId)` from your GSDK startup flow.

## Code layout

| Path | Purpose |
| --- | --- |
| `UnityServer\Assets\Server\Scripts\Metrics` | Collects Unity runtime, process, memory, and GC metrics. |
| `UnityServer\Assets\Server\Scripts\Telemetry` | Configures PlayFab telemetry event names, cadence, and transport. |
| `UnityServer\Assets\Server\Scripts\MpsTelemetryServerBootstrap.cs` | Sample bootstrap that starts metrics capture and telemetry flushing after your GSDK integration provides MPS config. |

The checked-in Unity project is a minimal container for inspecting and compiling the sample. The reusable part is the C# scripts folder.

## What the sample collects

The sample sends aggregated telemetry rather than raw per-frame or per-packet data. By default, it queues one compact summary event every 60 seconds and one final metrics event when shutdown is requested through the sample bootstrap.

Collected metrics include:

| Area | Metrics |
| --- | --- |
| Unity/runtime health | uptime, frame count, average/max frame time, long-frame count, target frame rate |
| Process health | CPU percentage, processor count, process working set, managed heap, Unity allocated/reserved memory, Mono used memory, GC collection deltas |

## Telemetry volume controls

The sample avoids high-volume telemetry patterns:

- No per-frame telemetry.
- No per-packet telemetry.
- Runtime samples are aggregated locally.
- Events are batched before calling PlayFab.
- Event names and payload fields are stable and low-cardinality.
- The default cadence is one summary event per 60 seconds, plus one final metrics event when `BeginShutdown` or `FlushFinalMetricsAsync` is called.

Tune the constants in `UnityServer\Assets\Server\Scripts\Telemetry\TelemetrySampleConfig.cs` if your title needs a different event cadence.

With the default cadence, each running server sends:

- one `server_metrics_summary` event every 60 seconds;
- one `server_metrics_final` event when `BeginShutdown` or `FlushFinalMetricsAsync` is called.

By default, no other telemetry event names are emitted. Custom metrics providers add fields to those same events rather than creating new event names.

## Add game or network metrics

This sample does not create a network listener. Your Unity game server already owns its MPS game port and networking stack, so network metrics should come from your server code.

Register a custom metrics provider before calling `InitializeForMps`. The provider is called each time the sample builds a telemetry summary, and its fields are merged into the same PlayFab telemetry event as the built-in Unity/process metrics.

```csharp
using System.Collections;
using System.Collections.Generic;
using PlayFab.Samples.UnityMpsTelemetry;
using UnityEngine;

public sealed class MyGsdkServerBootstrap : MonoBehaviour
{
    [SerializeField]
    private MpsTelemetryServerBootstrap _telemetry;

    private void OnGSDKServerStarted(string titleId)
    {
        _telemetry.RegisterCustomMetricsProvider(CaptureNetworkMetrics);
        _telemetry.InitializeForMps(titleId);

        // Start your own networking stack on the MPS-assigned port, then call ReadyForPlayers.
    }

    private IEnumerator OnGSDKShutdown()
    {
        yield return _telemetry.FlushFinalMetricsAsync();

        // Continue with your own server shutdown after the final telemetry flush.
    }

    private IDictionary<string, object> CaptureNetworkMetrics()
    {
        return new Dictionary<string, object>
        {
            { "networkActiveConnections", _networkServer.ActiveConnectionCount },
            { "networkBytesReceived", _networkServer.GetAndResetBytesReceived() },
            { "networkBytesSent", _networkServer.GetAndResetBytesSent() },
            { "networkPacketErrorCount", _networkServer.GetAndResetPacketErrorCount() },
            { "networkReplicationBacklog", _networkServer.ReplicationBacklogCount }
        };
    }
}
```

Use stable, low-cardinality field names. For interval counters such as bytes or packet errors, reset the counter inside your provider after returning the current value.
Replace `_networkServer` in the example with your Mirror, Netcode for GameObjects, FishNet, custom UDP/TCP, ECS/jobs, or other server networking component.

## Integrate with MPS

This sample does not take a dependency on GSDK. In a real MPS game server:

1. Download or clone the official PlayFab GSDK repository: `https://github.com/PlayFab/gsdk`.
2. Import the Unity GSDK into your game server project.
3. Copy `UnityServer\Assets\Server\Scripts` from this sample into your Unity server project.
4. Attach `MpsTelemetryServerBootstrap` to one persistent server GameObject, or create an equivalent component in your existing bootstrap.
5. Use your GSDK bootstrap to read MPS config such as title ID and assigned game port.
6. Register any custom metrics providers before initialization.
7. Call `InitializeForMps(titleId)` after GSDK startup.
8. Start your own networking stack on the assigned game port and call `ReadyForPlayers` when ready.
9. Wire your GSDK shutdown callback to `FlushFinalMetricsAsync` or `BeginShutdown`.

## Configure PlayFab telemetry

1. In PlayFab Game Manager, open your title.
2. Go to **Data > Telemetry Keys**.
3. Create a telemetry key.
4. Recommended for MPS: upload the key as a managed secret named `TelemetryKey` and reference it from your build. MPS exposes it to the server as `PF_MPS_SECRET_TelemetryKey`.
5. Alternative: set the `PLAYFAB_TELEMETRY_KEY` environment variable before starting the server.
6. Pass the PlayFab title ID from GSDK config into `MpsTelemetryServerBootstrap.InitializeForMps`.
7. For temporary validation only, you can replace `TelemetryKeySourceOverride` in `UnityServer\Assets\Server\Scripts\Telemetry\TelemetrySampleConfig.cs`, but do not commit a real key.

The sample checks for telemetry keys in this order: `PF_MPS_SECRET_TelemetryKey`, `PLAYFAB_TELEMETRY_KEY`, then the source override placeholder.

Telemetry is sent to:

```text
https://<titleId>.playfabapi.com/Event/WriteTelemetryEvents
```

The request uses the `X-TelemetryKey` header and the namespace `custom.mps.unityserver`.

## Unity version

This sample was created for Unity `6000.4.4f1`.

The expected editor path on Windows is:

```powershell
C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe
```

The `MpsTelemetryServer` scene and project settings are checked in. Open `UnityServer` in Unity if you want to inspect or modify the scene.

## Build and package

If you copy the scripts into an existing game, build and package that game using your normal Unity dedicated-server pipeline. If you use the checked-in `UnityServer` project as a starting point, integrate the official Unity GSDK and your networking stack first. The sample does not prescribe a build output path, container image, or packaging flow; configure those the same way you package your own MPS game servers.

## Test with LocalMultiplayerAgent in Linux container mode

To test in LocalMultiplayerAgent or MPS, first integrate the official Unity GSDK in your game server project. This sample does not call `ReadyForPlayers` or perform MPS state transitions by itself.

Configure LocalMultiplayerAgent for the build/package layout you chose. Use the port name, protocol, and start command your title already defines. For example, make sure the settings include:

- game port name: your configured MPS game port name
- protocol: your configured protocol, such as TCP or UDP
- server listening port: the port your Unity server binds inside the process or container
- start command: your Unity server executable with the arguments your build pipeline requires

In your GSDK-enabled server bootstrap:

1. Read title ID and assigned game port from GSDK config.
2. Configure your telemetry key through environment or deployment configuration.
3. Initialize this telemetry sample with the title ID.
4. Start your own networking stack on the assigned game port.
5. Call `ReadyForPlayers` when your server is ready.
6. Yield `FlushFinalMetricsAsync` from your shutdown coroutine, or call `BeginShutdown` if your callback cannot yield.

Check the LocalMultiplayerAgent output for state transitions from initializing to standing by to active, and check server logs for telemetry flush messages.

## Verify events in PlayFab

In PlayFab Game Manager:

1. Open your title.
2. Go to **Data > Data Explorer**.
3. Open the **Advanced** query view.
4. Run a query for the sample namespace and event names:

```kusto
['events.all']
| where FullName_Namespace == "custom.mps.unityserver"
| where FullName_Name in ("server_metrics_summary", "server_metrics_final")
| project Timestamp, FullName_Name, Entity_Id, EventData
| order by Timestamp desc
| limit 100
```

The payload values will vary by server, but a `server_metrics_summary` event should include fields like this:

```json
{
  "Payload": {
    "uptimeSeconds": 360.25,
    "targetFrameRate": -1,
    "frameCount": 3580,
    "averageFrameMs": 16.74,
    "maxFrameMs": 68.12,
    "longFrameCount": 1,
    "longFrameThresholdMs": 100,
    "processCpuPercent": 8.42,
    "processorCount": 8,
    "processWorkingSetBytes": 184320000,
    "managedHeapBytes": 2150400,
    "unityAllocatedMemoryBytes": 48234496,
    "unityReservedMemoryBytes": 109051904,
    "unityMonoUsedMemoryBytes": 4194304,
    "gcGen0Collections": 2,
    "gcGen1Collections": 0,
    "gcGen2Collections": 0
  }
}
```

If you register a custom metrics provider, your own fields appear in the same `Payload` object. For example:

```json
{
  "networkActiveConnections": 12,
  "networkBytesReceived": 98304,
  "networkBytesSent": 147456,
  "networkPacketErrorCount": 0,
  "networkReplicationBacklog": 3
}
```

## Production notes

- Prefer MPS managed secrets for telemetry keys; avoid storing real keys in source, Unity scenes, prefabs, or container images.
- Move telemetry intervals into build metadata, environment configuration, or your deployment pipeline if different titles or fleets need different cadences.
- Keep per-server identifiers in payload fields rather than creating many event names.
- Add game-specific counters from your networking stack, gameplay loop, match state, or ECS/job systems.
- Consider sampling or longer intervals for very large fleets.
- Use PlayFab Data Connections if you need to export telemetry to your own storage or analytics pipeline.
