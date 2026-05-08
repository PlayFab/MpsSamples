#include "PlayFabTelemetryClient.h"

#include "Dom/JsonObject.h"
#include "Dom/JsonValue.h"
#include "HttpModule.h"
#include "MpsTelemetrySampleConfig.h"
#include "MpsTelemetrySubsystem.h"
#include "Serialization/JsonSerializer.h"
#include "Serialization/JsonWriter.h"

using namespace PlayFab::Samples::UnrealMpsTelemetry;

void FPlayFabTelemetryClient::Configure(const FString& TitleId, const FString& TelemetryKey, const FString& EventNamespace, const FString& ExternalEntityId)
{
    if (TitleId.IsEmpty())
    {
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("A PlayFab title ID is required to configure telemetry."));
        return;
    }

    TelemetryKeyValue = TelemetryKey;
    EventNamespaceName = EventNamespace.IsEmpty() ? FString(FMpsTelemetrySampleConfig::EventNamespace()) : EventNamespace;
    ExternalEntityIdValue = NormalizeExternalEntityId(ExternalEntityId);
    Endpoint = FString::Printf(TEXT("https://%s.playfabapi.com/Event/WriteTelemetryEvents"), *TitleId);

    CustomTags = MakeShared<FJsonObject>();
    CustomTags->SetStringField(TEXT("sample"), TEXT("UnrealMpsTelemetry"));
    CustomTags->SetStringField(TEXT("sampleVersion"), TEXT("1"));
    bConfigured = true;

    if (!FMpsTelemetrySampleConfig::IsConfiguredTelemetryKey(TelemetryKeyValue))
    {
        LogMissingTelemetryKey();
    }
}

void FPlayFabTelemetryClient::SetExternalEntityId(const FString& ExternalEntityId)
{
    ExternalEntityIdValue = NormalizeExternalEntityId(ExternalEntityId);
}

bool FPlayFabTelemetryClient::Enqueue(const FString& EventName, TSharedPtr<FJsonObject> Payload)
{
    if (!bConfigured)
    {
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("Telemetry event was not queued because the telemetry client is not configured."));
        return false;
    }

    if (!FMpsTelemetrySampleConfig::IsConfiguredTelemetryKey(TelemetryKeyValue))
    {
        LogMissingTelemetryKey();
        return false;
    }

    if (EventName.IsEmpty())
    {
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("Telemetry event was not queued because the event name is empty."));
        return false;
    }

    if (!Payload.IsValid())
    {
        Payload = MakeShared<FJsonObject>();
        Payload->SetNumberField(TEXT("schemaVersion"), 1);
    }

    TSharedPtr<FJsonObject> Entity = MakeShared<FJsonObject>();
    Entity->SetStringField(TEXT("Type"), TEXT("external"));
    Entity->SetStringField(TEXT("Id"), ExternalEntityIdValue);

    TSharedPtr<FJsonObject> TelemetryEvent = MakeShared<FJsonObject>();
    TelemetryEvent->SetStringField(TEXT("EventNamespace"), EventNamespaceName);
    TelemetryEvent->SetStringField(TEXT("Name"), EventName);
    TelemetryEvent->SetStringField(TEXT("OriginalId"), FGuid::NewGuid().ToString(EGuidFormats::Digits));
    TelemetryEvent->SetStringField(TEXT("OriginalTimestamp"), FDateTime::UtcNow().ToIso8601());
    TelemetryEvent->SetObjectField(TEXT("Entity"), Entity);
    TelemetryEvent->SetObjectField(TEXT("Payload"), Payload);

    FScopeLock Lock(&SyncRoot);
    if (PendingEvents.Num() >= FMpsTelemetrySampleConfig::MaxQueuedEvents)
    {
        PendingEvents.RemoveAt(0);
        UE_LOG(LogMpsTelemetrySample, Warning, TEXT("Telemetry queue is full. Dropped the oldest pending telemetry event."));
    }

    PendingEvents.Add(TelemetryEvent);
    return true;
}

void FPlayFabTelemetryClient::FlushAsync()
{
    TArray<TSharedPtr<FJsonObject>> Batch;
    {
        // Hold SyncRoot across the in-progress check, queue dequeue, and the
        // bFlushInProgress flip so two concurrent callers can never both pull a
        // batch and start parallel HTTP requests.
        FScopeLock Lock(&SyncRoot);

        if (bFlushInProgress || !bConfigured || !FMpsTelemetrySampleConfig::IsConfiguredTelemetryKey(TelemetryKeyValue))
        {
            return;
        }

        const int32 BatchCount = FMath::Min(PendingEvents.Num(), FMpsTelemetrySampleConfig::MaxEventsPerBatch);
        if (BatchCount == 0)
        {
            return;
        }

        Batch.Append(PendingEvents.GetData(), BatchCount);
        PendingEvents.RemoveAt(0, BatchCount);
        bFlushInProgress = true;
    }

    TArray<TSharedPtr<FJsonValue>> EventValues;
    EventValues.Reserve(Batch.Num());
    for (const TSharedPtr<FJsonObject>& Event : Batch)
    {
        EventValues.Add(MakeShared<FJsonValueObject>(Event));
    }

    TSharedPtr<FJsonObject> RequestBody = MakeShared<FJsonObject>();
    RequestBody->SetArrayField(TEXT("Events"), EventValues);
    RequestBody->SetObjectField(TEXT("CustomTags"), CustomTags);

    FString RequestPayload;
    TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&RequestPayload);
    if (!FJsonSerializer::Serialize(RequestBody.ToSharedRef(), Writer))
    {
        RequeueBatch(Batch);
        ClearFlushInProgress();
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("Failed to serialize PlayFab telemetry payload."));
        return;
    }

    if (FMpsTelemetrySampleConfig::LogTelemetryPayloads)
    {
        UE_LOG(LogMpsTelemetrySample, Log, TEXT("%s"), *RequestPayload);
    }

    TSharedRef<IHttpRequest, ESPMode::ThreadSafe> Request = FHttpModule::Get().CreateRequest();
    Request->SetURL(Endpoint);
    Request->SetVerb(TEXT("POST"));
    Request->SetHeader(TEXT("Accept"), TEXT("application/json"));
    Request->SetHeader(TEXT("Content-Type"), TEXT("application/json"));
    Request->SetHeader(TEXT("X-TelemetryKey"), TelemetryKeyValue);
    Request->SetContentAsString(RequestPayload);

    // Capture a strong ref so the client outlives the in-flight request even if
    // the owning subsystem releases its TSharedPtr (for example, during
    // UMpsTelemetrySubsystem::Deinitialize). Without this, the dequeued batch
    // would be silently dropped on shutdown.
    TSharedRef<FPlayFabTelemetryClient, ESPMode::ThreadSafe> StrongClient = AsShared();
    Request->OnProcessRequestComplete().BindLambda(
        [StrongClient, Batch](FHttpRequestPtr RequestPtr, FHttpResponsePtr Response, bool bSucceeded)
        {
            const int32 ResponseCode = Response.IsValid() ? Response->GetResponseCode() : 0;
            if (!bSucceeded || !Response.IsValid() || ResponseCode < 200 || ResponseCode >= 300)
            {
                StrongClient->RequeueBatch(Batch);
                FString ResponseBody = Response.IsValid() ? Response->GetContentAsString() : FString();
                // Cap the logged body so a sustained 5xx storm cannot flood logs.
                const int32 MaxLoggedBodyLength = 512;
                if (ResponseBody.Len() > MaxLoggedBodyLength)
                {
                    ResponseBody = ResponseBody.Left(MaxLoggedBodyLength) + TEXT("... [truncated]");
                }
                UE_LOG(
                    LogMpsTelemetrySample,
                    Error,
                    TEXT("PlayFab telemetry flush failed. HttpCode=%d, Body=%s"),
                    ResponseCode,
                    *ResponseBody);
            }
            else
            {
                UE_LOG(LogMpsTelemetrySample, Log, TEXT("Flushed %d telemetry event(s) to PlayFab."), Batch.Num());
            }

            StrongClient->ClearFlushInProgress();

            // Completion chaining: if more events accumulated (or were requeued)
            // while this request was in flight, kick off another flush. This is
            // what drains the queue during shutdown when the periodic ticker is
            // already gone.
            if (StrongClient->GetPendingEventCount() > 0)
            {
                StrongClient->FlushAsync();
            }
        });

    if (!Request->ProcessRequest())
    {
        RequeueBatch(Batch);
        ClearFlushInProgress();
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("Failed to start PlayFab telemetry HTTP request."));
    }
}

int32 FPlayFabTelemetryClient::GetPendingEventCount() const
{
    FScopeLock Lock(&SyncRoot);
    return PendingEvents.Num();
}

void FPlayFabTelemetryClient::ClearFlushInProgress()
{
    FScopeLock Lock(&SyncRoot);
    bFlushInProgress = false;
}

void FPlayFabTelemetryClient::RequeueBatch(const TArray<TSharedPtr<FJsonObject>>& Batch)
{
    FScopeLock Lock(&SyncRoot);

    // The failed batch contains the oldest pending events (FlushAsync pulls from
    // the front of PendingEvents). Anything still in PendingEvents is newer than
    // the batch. To match the Unity sample we always drop the oldest events
    // first, so when the queue is over capacity we trim the front of the batch
    // rather than the newer events that arrived while the flush was in flight.
    const int32 AvailableSlots = FMpsTelemetrySampleConfig::MaxQueuedEvents - PendingEvents.Num();
    if (AvailableSlots <= 0)
    {
        UE_LOG(
            LogMpsTelemetrySample,
            Warning,
            TEXT("Telemetry queue is full. Dropped %d event(s) after a failed flush."),
            Batch.Num());
        return;
    }

    TArray<TSharedPtr<FJsonObject>> EventsToRequeue = Batch;
    if (EventsToRequeue.Num() > AvailableSlots)
    {
        const int32 DroppedEventCount = EventsToRequeue.Num() - AvailableSlots;
        EventsToRequeue.RemoveAt(0, DroppedEventCount);
        UE_LOG(
            LogMpsTelemetrySample,
            Warning,
            TEXT("Telemetry queue is near capacity. Dropped %d oldest event(s) after a failed flush."),
            DroppedEventCount);
    }

    PendingEvents.Insert(EventsToRequeue, 0);
}

void FPlayFabTelemetryClient::LogMissingTelemetryKey()
{
    if (bMissingKeyLogged)
    {
        return;
    }

    bMissingKeyLogged = true;
    UE_LOG(
        LogMpsTelemetrySample,
        Warning,
        TEXT("PlayFab telemetry key is not configured. Set PF_MPS_SECRET_TelemetryKey or PLAYFAB_TELEMETRY_KEY."));
}

FString FPlayFabTelemetryClient::NormalizeExternalEntityId(const FString& ExternalEntityId) const
{
    return ExternalEntityId.IsEmpty() ? FString(TEXT("unrealmpsserver")) : ExternalEntityId;
}
