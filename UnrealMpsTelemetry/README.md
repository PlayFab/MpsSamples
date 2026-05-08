# PlayFab MPS Unreal telemetry sample

## Overview

This sample shows how an Unreal dedicated server running on PlayFab Multiplayer Servers (MPS) can collect aggregated runtime and process metrics and send them to PlayFab as telemetry events.

The sample is intentionally focused on metrics and telemetry. It does not include, reference, or wrap the Game Server SDK (GSDK). Game developers should import the official Unreal GSDK plugin into their own server project and wire this telemetry plugin into their existing MPS lifecycle code.

If you already have an Unreal server project, use this as a plugin-style drop-in sample: copy `UnrealMpsTelemetry` into your project's `Plugins` folder, enable the plugin for your server target, and call `UMpsTelemetrySubsystem::InitializeForMps` from your GSDK startup flow after reading the title ID from GSDK config.

## Code layout

| Path | Purpose |
| --- | --- |
| `UnrealMpsTelemetry.uplugin` | Runtime plugin descriptor for dropping the sample into an Unreal project. |
| `Source/UnrealMpsTelemetry/Public/MpsTelemetrySubsystem.h` | Game instance subsystem that owns metrics collection and telemetry flushing. |
| `Source/UnrealMpsTelemetry/Public/MpsTelemetrySampleConfig.h` | Event names, cadence, queue limits, and telemetry key lookup. |
| `Source/UnrealMpsTelemetry/Private/UnrealServerMetricsCollector.*` | Collects Unreal tick and process memory metrics and supports custom metrics providers. |
| `Source/UnrealMpsTelemetry/Private/PlayFabTelemetryClient.*` | Queues, batches, serializes, and sends events to PlayFab telemetry. |

The checked-in files are a reusable plugin sample, not a complete Unreal game project. Copy them into your own GSDK-enabled Unreal dedicated server and build with your normal Unreal toolchain.

## What the sample collects

The sample sends aggregated telemetry rather than raw per-tick or per-packet data. By default, it queues one compact summary event every 60 seconds and one final metrics event when shutdown is requested through the subsystem.

Collected metrics include:

| Area | Metrics |
| --- | --- |
| Unreal/runtime health | uptime, tick count, average/max tick time, long-tick count, long-tick threshold |
| Process/system health | processor count, process physical memory, process virtual memory, available physical memory, available virtual memory |

## Telemetry volume controls

The sample avoids high-volume telemetry patterns:

- No per-tick telemetry.
- No per-packet telemetry.
- Runtime samples are aggregated locally.
- Events are batched before calling PlayFab.
- Event names and payload fields are stable and low-cardinality.
- The default cadence is one summary event per 60 seconds, plus one final metrics event when `BeginShutdown` is called.

Tune the constants in `Source/UnrealMpsTelemetry/Public/MpsTelemetrySampleConfig.h` if your title needs a different event cadence.

With the default cadence, each running server sends:

- one `server_metrics_summary` event every 60 seconds;
- one `server_metrics_final` event when `BeginShutdown` is called.

By default, no other telemetry event names are emitted. Custom metrics providers add fields to those same events rather than creating new event names.

## Add game or network metrics

This sample does not create a network listener. Your Unreal game server already owns its MPS game port and networking stack, so network metrics should come from your server code.

Register a custom metrics provider before calling `InitializeForMps`. The provider is called each time the sample builds a telemetry summary, and its fields are merged into the same PlayFab telemetry event as the built-in Unreal/process metrics.

```cpp
#include "Dom/JsonObject.h"
#include "MpsTelemetrySubsystem.h"

void UMyGsdkServerBootstrap::OnGSDKServerStarted(const FString& TitleId)
{
    UMpsTelemetrySubsystem* Telemetry = GetGameInstance()->GetSubsystem<UMpsTelemetrySubsystem>();
    Telemetry->RegisterCustomMetricsProvider(
        [this](TSharedRef<FJsonObject> Payload)
        {
            Payload->SetNumberField(TEXT("networkActiveConnections"), NetworkServer->GetActiveConnectionCount());
            Payload->SetNumberField(TEXT("networkBytesReceived"), NetworkServer->GetAndResetBytesReceived());
            Payload->SetNumberField(TEXT("networkBytesSent"), NetworkServer->GetAndResetBytesSent());
            Payload->SetNumberField(TEXT("networkPacketErrorCount"), NetworkServer->GetAndResetPacketErrorCount());
            Payload->SetNumberField(TEXT("networkReplicationBacklog"), NetworkServer->GetReplicationBacklogCount());
        });

    Telemetry->InitializeForMps(TitleId);

    // Start your own networking stack on the MPS-assigned port, then call ReadyForPlayers.
}

void UMyGsdkServerBootstrap::OnGSDKShutdown()
{
    GetGameInstance()->GetSubsystem<UMpsTelemetrySubsystem>()->BeginShutdown();

    // Continue with your own server shutdown after giving the HTTP module a chance to flush.
}
```

Use stable, low-cardinality field names. For interval counters such as bytes or packet errors, reset the counter inside your provider after writing the current value.
Replace `NetworkServer` in the example with your Unreal networking, replication graph, Online Services, custom socket, or gameplay server component.

## Integrate with MPS

This sample does not take a dependency on GSDK. In a real MPS game server:

1. Download or clone the official PlayFab GSDK repository: `https://github.com/PlayFab/gsdk`.
2. Import the Unreal GSDK plugin into your game server project.
3. Copy this `UnrealMpsTelemetry` folder into your Unreal project's `Plugins` folder.
4. Enable the plugin for your server target and regenerate project files if needed.
5. Use your GSDK bootstrap to read MPS config such as title ID and assigned game port.
6. Register any custom metrics providers before initialization.
7. Call `InitializeForMps(TitleId)` after GSDK startup.
8. Start your own networking stack on the assigned game port and call `ReadyForPlayers` when ready.
9. Wire your GSDK shutdown callback to `BeginShutdown` before completing process shutdown.

## Configure PlayFab telemetry

1. In PlayFab Game Manager, open your title.
2. Go to **Data > Telemetry Keys**.
3. Create a telemetry key.
4. Recommended for MPS: upload the key as a managed secret named `TelemetryKey` and reference it from your build. MPS exposes it to the server as `PF_MPS_SECRET_TelemetryKey`.
5. Alternative: set the `PLAYFAB_TELEMETRY_KEY` environment variable before starting the server.
6. Pass the PlayFab title ID from GSDK config into `UMpsTelemetrySubsystem::InitializeForMps`.
7. For temporary validation only, you can replace the value returned by `TelemetryKeySourceOverride()` in `Source/UnrealMpsTelemetry/Public/MpsTelemetrySampleConfig.h`, but do not commit a real key.

The sample checks for telemetry keys in this order: `PF_MPS_SECRET_TelemetryKey`, `PLAYFAB_TELEMETRY_KEY`, then the source override placeholder.

Telemetry is sent to:

```text
https://<titleId>.playfabapi.com/Event/WriteTelemetryEvents
```

The request uses the `X-TelemetryKey` header and the namespace `custom.mps.unrealserver`.

## Unreal version

The sample is written as a small runtime plugin intended for Unreal dedicated server projects that already build with the official Unreal GSDK plugin. It uses standard Unreal Engine modules: `Core`, `CoreUObject`, `Engine`, `HTTP`, and `Json`.

## Build and package

If you copy the plugin into an existing game, build and package that game using your normal Unreal dedicated-server pipeline. The sample does not prescribe a build output path, container image, or packaging flow; configure those the same way you package your own MPS game servers.

## Test with LocalMultiplayerAgent in container mode

To test in LocalMultiplayerAgent or MPS, first integrate the official Unreal GSDK plugin in your game server project. This sample does not call `ReadyForPlayers` or perform MPS state transitions by itself.

Configure LocalMultiplayerAgent for the build/package layout you chose. Use the port name, protocol, and start command your title already defines. For example, make sure the settings include:

- game port name: your configured MPS game port name
- protocol: your configured protocol, such as TCP or UDP
- server listening port: the port your Unreal server binds inside the process or container
- start command: your Unreal dedicated server executable with the arguments your build pipeline requires

In your GSDK-enabled server bootstrap:

1. Read title ID and assigned game port from GSDK config.
2. Configure your telemetry key through environment or deployment configuration.
3. Initialize this telemetry sample with the title ID.
4. Start your own networking stack on the assigned game port.
5. Call `ReadyForPlayers` when your server is ready.
6. Call `BeginShutdown` from your shutdown callback before completing process shutdown.

Check the LocalMultiplayerAgent output for state transitions from initializing to standing by to active, and check server logs for telemetry flush messages.

## Verify events in PlayFab

In PlayFab Game Manager:

1. Open your title.
2. Go to **Data > Data Explorer**.
3. Open the **Advanced** query view.
4. Run a query for the sample namespace and event names:

```kusto
['events.all']
| where FullName_Namespace == "custom.mps.unrealserver"
| where FullName_Name in ("server_metrics_summary", "server_metrics_final")
| project Timestamp, FullName_Name, Entity_Id, EventData
| order by Timestamp desc
| limit 100
```

The payload values will vary by server, but a `server_metrics_summary` event ingested into PlayFab telemetry looks like this when inspected in Data Explorer (title-specific identifiers redacted):

```json
{
  "Timestamp": "2024-01-15T22:14:55.2968902Z",
  "EntityLineage": {
    "namespace": "<title namespace id>",
    "title": "<your title id>",
    "external": "unrealmpsserver"
  },
  "SchemaVersion": "2.0.1",
  "FullName": {
    "Namespace": "custom.mps.unrealserver",
    "Name": "server_metrics_summary"
  },
  "Id": "<event id>",
  "Entity": {
    "Id": "unrealmpsserver",
    "Type": "external"
  },
  "Originator": {
    "Id": "<your title id>",
    "Type": "external"
  },
  "OriginInfo": {
    "Timestamp": "2024-01-15T22:14:55.6800000Z",
    "Id": "<originator event id>",
    "CustomTags": {
      "sample": "UnrealMpsTelemetry",
      "sampleVersion": "1"
    },
    "Key": "test"
  },
  "Payload": {
    "uptimeSeconds": 90.394,
    "tickCount": 111731,
    "averageTickMs": 0.537,
    "maxTickMs": 1229.641,
    "longTickCount": 1,
    "longTickThresholdMs": 100,
    "processorCount": 20,
    "processPhysicalMemoryBytes": 1615429632,
    "processVirtualMemoryBytes": 1475825664,
    "availablePhysicalMemoryBytes": 31376830464,
    "availableVirtualMemoryBytes": 21841702912
  },
  "PayloadContentType": "Json"
}
```

The `Payload` object holds the metrics fields the sample emits. The surrounding envelope (`Timestamp`, `EntityLineage`, `FullName`, `Entity`, `Originator`, `OriginInfo`, `PayloadContentType`) is added by PlayFab telemetry ingestion and is the same shape for every event.

If you register a custom metrics provider, your own fields appear in the same `Payload` object alongside the built-in ones. For example, a network metrics provider might add:

```json
{
  "Payload": {
    "uptimeSeconds": 90.394,
    "...": "(built-in fields above)",
    "networkActiveConnections": 12,
    "networkBytesReceived": 98304,
    "networkBytesSent": 147456,
    "networkPacketErrorCount": 0,
    "networkReplicationBacklog": 3
  }
}
```

## Production notes

- Prefer MPS managed secrets for telemetry keys; avoid storing real keys in source, Unreal assets, command-line arguments, or container images.
- Move telemetry intervals into build metadata, environment configuration, or your deployment pipeline if different titles or fleets need different cadences.
- Keep per-server identifiers in payload fields rather than creating many event names.
- Add game-specific counters from your networking stack, gameplay loop, match state, replication graph, or Online Services integration.
- Consider sampling or longer intervals for very large fleets.
- Use PlayFab Data Connections if you need to export telemetry to your own storage or analytics pipeline.
