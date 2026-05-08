#pragma once

#include "CoreMinimal.h"
#include "Http.h"

class FJsonObject;

class FPlayFabTelemetryClient : public TSharedFromThis<FPlayFabTelemetryClient, ESPMode::ThreadSafe>
{
public:
    void Configure(const FString& TitleId, const FString& TelemetryKey, const FString& EventNamespace, const FString& ExternalEntityId);
    void SetExternalEntityId(const FString& ExternalEntityId);
    bool Enqueue(const FString& EventName, TSharedPtr<FJsonObject> Payload);
    void FlushAsync();
    int32 GetPendingEventCount() const;

private:
    void RequeueBatch(const TArray<TSharedPtr<FJsonObject>>& Batch);
    void ClearFlushInProgress();
    void LogMissingTelemetryKey();
    FString NormalizeExternalEntityId(const FString& ExternalEntityId) const;

    mutable FCriticalSection SyncRoot;
    TArray<TSharedPtr<FJsonObject>> PendingEvents;
    TSharedPtr<FJsonObject> CustomTags;
    FString Endpoint;
    FString EventNamespaceName;
    FString ExternalEntityIdValue;
    FString TelemetryKeyValue;
    bool bConfigured = false;
    bool bFlushInProgress = false;
    bool bMissingKeyLogged = false;
};
