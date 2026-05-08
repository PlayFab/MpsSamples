#include "MpsTelemetrySubsystem.h"

#include "Dom/JsonObject.h"
#include "MpsTelemetrySampleConfig.h"
#include "PlayFabTelemetryClient.h"
#include "UnrealServerMetricsCollector.h"

using namespace PlayFab::Samples::UnrealMpsTelemetry;

void UMpsTelemetrySubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
    Super::Initialize(Collection);
    TelemetryClient = MakeShared<FPlayFabTelemetryClient, ESPMode::ThreadSafe>();
    MetricsCollector = MakePimpl<FUnrealServerMetricsCollector>();
    MetricsCollector->Initialize();
}

void UMpsTelemetrySubsystem::Deinitialize()
{
    // Ensure the final event is enqueued even if the host's GSDK shutdown hook
    // never called BeginShutdown. Strong refs captured by in-flight HTTP
    // completion lambdas keep the underlying client alive after we release our
    // own pointer, and completion chaining inside the client keeps draining
    // remaining events until the queue is empty (or HTTP module is torn down).
    if (bInitializedForMps && !bShutdownStarted)
    {
        BeginShutdown();
    }
    else
    {
        RemoveTickers();
        if (TelemetryClient.IsValid() && TelemetryClient->GetPendingEventCount() > 0)
        {
            TelemetryClient->FlushAsync();
        }
    }

    TelemetryClient.Reset();
    MetricsCollector.Reset();
    Super::Deinitialize();
}

void UMpsTelemetrySubsystem::Tick(float DeltaTime)
{
    if (MetricsCollector.IsValid() && bInitializedForMps && !bShutdownStarted)
    {
        MetricsCollector->Tick(DeltaTime);
    }
}

TStatId UMpsTelemetrySubsystem::GetStatId() const
{
    RETURN_QUICK_DECLARE_CYCLE_STAT(UMpsTelemetrySubsystem, STATGROUP_Tickables);
}

bool UMpsTelemetrySubsystem::IsTickable() const
{
    return !IsTemplate() && bInitializedForMps && !bShutdownStarted;
}

void UMpsTelemetrySubsystem::InitializeForMps(const FString& TitleId, const FString& ExternalEntityId)
{
    if (bInitializedForMps)
    {
        UE_LOG(LogMpsTelemetrySample, Warning, TEXT("MPS telemetry sample is already initialized."));
        return;
    }

    if (TitleId.IsEmpty())
    {
        UE_LOG(LogMpsTelemetrySample, Error, TEXT("Pass the PlayFab title ID from GSDK config when initializing telemetry."));
        return;
    }

    if (!TelemetryClient.IsValid())
    {
        TelemetryClient = MakeShared<FPlayFabTelemetryClient, ESPMode::ThreadSafe>();
    }

    if (!MetricsCollector.IsValid())
    {
        MetricsCollector = MakePimpl<FUnrealServerMetricsCollector>();
        MetricsCollector->Initialize();
    }

    TelemetryClient->Configure(
        TitleId,
        FMpsTelemetrySampleConfig::GetTelemetryKey(),
        FMpsTelemetrySampleConfig::EventNamespace(),
        ExternalEntityId);

    // FTSTicker fires on the game thread driven by real elapsed time, so the
    // summary cadence is unaffected by world pause or time dilation. Weak lambdas
    // are no-ops if this subsystem has been destroyed before RemoveTickers ran.
    MetricsSummaryTickerHandle = FTSTicker::GetCoreTicker().AddTicker(
        FTickerDelegate::CreateWeakLambda(this, [this](float)
        {
            EnqueuePeriodicMetricsSummary();
            return true;
        }),
        FMpsTelemetrySampleConfig::MetricsSummaryIntervalSeconds);

    TelemetryFlushTickerHandle = FTSTicker::GetCoreTicker().AddTicker(
        FTickerDelegate::CreateWeakLambda(this, [this](float)
        {
            FlushTelemetry();
            return true;
        }),
        FMpsTelemetrySampleConfig::TelemetryFlushIntervalSeconds);

    bInitializedForMps = true;
}

void UMpsTelemetrySubsystem::BeginShutdown()
{
    if (!bInitializedForMps || bShutdownStarted)
    {
        return;
    }

    bShutdownStarted = true;
    RemoveTickers();

    UE_LOG(LogMpsTelemetrySample, Log, TEXT("MPS telemetry sample server is shutting down."));
    EnqueueMetricsSummary(TEXT("server_metrics_final"));
    FlushTelemetry();
}

void UMpsTelemetrySubsystem::RegisterCustomMetricsProvider(TFunction<void(TSharedRef<FJsonObject> Payload)> Provider)
{
    if (!MetricsCollector.IsValid())
    {
        MetricsCollector = MakePimpl<FUnrealServerMetricsCollector>();
        MetricsCollector->Initialize();
    }

    MetricsCollector->RegisterCustomMetricsProvider(MoveTemp(Provider));
}

void UMpsTelemetrySubsystem::EnqueuePeriodicMetricsSummary()
{
    EnqueueMetricsSummary(TEXT("server_metrics_summary"));
}

void UMpsTelemetrySubsystem::EnqueueMetricsSummary(const FString& EventName)
{
    if (!TelemetryClient.IsValid() || !MetricsCollector.IsValid())
    {
        return;
    }

    TelemetryClient->Enqueue(EventName, MetricsCollector->CaptureSummaryAndReset());
}

void UMpsTelemetrySubsystem::FlushTelemetry()
{
    if (TelemetryClient.IsValid())
    {
        TelemetryClient->FlushAsync();
    }
}

void UMpsTelemetrySubsystem::RemoveTickers()
{
    if (MetricsSummaryTickerHandle.IsValid())
    {
        FTSTicker::GetCoreTicker().RemoveTicker(MetricsSummaryTickerHandle);
        MetricsSummaryTickerHandle.Reset();
    }

    if (TelemetryFlushTickerHandle.IsValid())
    {
        FTSTicker::GetCoreTicker().RemoveTicker(TelemetryFlushTickerHandle);
        TelemetryFlushTickerHandle.Reset();
    }
}
