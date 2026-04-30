#pragma once

#include "CoreMinimal.h"
#include "Containers/Ticker.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "Templates/PimplPtr.h"
#include "Tickable.h"
#include "MpsTelemetrySubsystem.generated.h"

class FJsonObject;
class FPlayFabTelemetryClient;
class FUnrealServerMetricsCollector;

DECLARE_LOG_CATEGORY_EXTERN(LogMpsTelemetrySample, Log, All);

UCLASS()
class UNREALMPSTELEMETRY_API UMpsTelemetrySubsystem : public UGameInstanceSubsystem, public FTickableGameObject
{
    GENERATED_BODY()

public:
    virtual void Initialize(FSubsystemCollectionBase& Collection) override;
    virtual void Deinitialize() override;

    virtual void Tick(float DeltaTime) override;
    virtual TStatId GetStatId() const override;
    virtual bool IsTickable() const override;

    UFUNCTION(BlueprintCallable, Category = "PlayFab|MPS Telemetry")
    void InitializeForMps(const FString& TitleId, const FString& ExternalEntityId = TEXT("unrealmpsserver"));

    UFUNCTION(BlueprintCallable, Category = "PlayFab|MPS Telemetry")
    void BeginShutdown();

    void RegisterCustomMetricsProvider(TFunction<void(TSharedRef<FJsonObject> Payload)> Provider);

private:
    void EnqueuePeriodicMetricsSummary();
    void EnqueueMetricsSummary(const FString& EventName);
    void FlushTelemetry();
    void RemoveTickers();

    TSharedPtr<FPlayFabTelemetryClient, ESPMode::ThreadSafe> TelemetryClient;
    // TPimplPtr type-erases the deleter so the metrics collector header (which is
    // private to the plugin) does not have to be visible to consumers of this header.
    // TUniquePtr<ForwardDecl> would otherwise force UHT-generated code to instantiate
    // the destructor for an incomplete type.
    TPimplPtr<FUnrealServerMetricsCollector> MetricsCollector;

    // Use FTSTicker instead of FTimerManager so the periodic summary and flush keep
    // firing at real time, even if the world is paused or time-dilated. This matches
    // the Unity sample, which intentionally uses WaitForSecondsRealtime.
    FTSTicker::FDelegateHandle MetricsSummaryTickerHandle;
    FTSTicker::FDelegateHandle TelemetryFlushTickerHandle;
    bool bInitializedForMps = false;
    bool bShutdownStarted = false;
};
