#include "UnrealServerMetricsCollector.h"

#include "Dom/JsonObject.h"
#include "HAL/PlatformMemory.h"
#include "HAL/PlatformMisc.h"
#include "HAL/PlatformTime.h"
#include "MpsTelemetrySampleConfig.h"
#include "MpsTelemetrySubsystem.h"

using namespace PlayFab::Samples::UnrealMpsTelemetry;

void FUnrealServerMetricsCollector::Initialize()
{
    StartedSeconds = FPlatformTime::Seconds();
    ResetIntervalCounters();
}

void FUnrealServerMetricsCollector::Tick(float DeltaTime)
{
    TickCount++;
    TickDeltaSecondsTotal += DeltaTime;
    MaxTickDeltaSeconds = FMath::Max(MaxTickDeltaSeconds, static_cast<double>(DeltaTime));

    if (DeltaTime * 1000.0 >= FMpsTelemetrySampleConfig::LongTickThresholdMilliseconds)
    {
        LongTickCount++;
    }
}

void FUnrealServerMetricsCollector::RegisterCustomMetricsProvider(TFunction<void(TSharedRef<FJsonObject> Payload)> Provider)
{
    if (!Provider)
    {
        UE_LOG(LogMpsTelemetrySample, Warning, TEXT("Ignoring empty custom metrics provider."));
        return;
    }

    CustomMetricsProviders.Add(MoveTemp(Provider));
}

TSharedRef<FJsonObject> FUnrealServerMetricsCollector::CaptureSummaryAndReset()
{
    const double AverageTickMs = TickCount == 0
        ? 0.0
        : (TickDeltaSecondsTotal / static_cast<double>(TickCount)) * 1000.0;
    const FPlatformMemoryStats MemoryStats = FPlatformMemory::GetStats();

    TSharedRef<FJsonObject> Payload = MakeShared<FJsonObject>();
    Payload->SetNumberField(TEXT("uptimeSeconds"), static_cast<double>(FMath::RoundToInt((FPlatformTime::Seconds() - StartedSeconds) * 1000.0)) / 1000.0);
    Payload->SetNumberField(TEXT("tickCount"), TickCount);
    Payload->SetNumberField(TEXT("averageTickMs"), static_cast<double>(FMath::RoundToInt(AverageTickMs * 1000.0)) / 1000.0);
    Payload->SetNumberField(TEXT("maxTickMs"), static_cast<double>(FMath::RoundToInt(MaxTickDeltaSeconds * 1000000.0)) / 1000.0);
    Payload->SetNumberField(TEXT("longTickCount"), LongTickCount);
    Payload->SetNumberField(TEXT("longTickThresholdMs"), FMpsTelemetrySampleConfig::LongTickThresholdMilliseconds);
    Payload->SetNumberField(TEXT("processorCount"), FPlatformMisc::NumberOfCoresIncludingHyperthreads());
    Payload->SetNumberField(TEXT("processPhysicalMemoryBytes"), static_cast<double>(MemoryStats.UsedPhysical));
    Payload->SetNumberField(TEXT("processVirtualMemoryBytes"), static_cast<double>(MemoryStats.UsedVirtual));
    Payload->SetNumberField(TEXT("availablePhysicalMemoryBytes"), static_cast<double>(MemoryStats.AvailablePhysical));
    Payload->SetNumberField(TEXT("availableVirtualMemoryBytes"), static_cast<double>(MemoryStats.AvailableVirtual));

    AddCustomMetrics(Payload);
    ResetIntervalCounters();
    return Payload;
}

void FUnrealServerMetricsCollector::ResetIntervalCounters()
{
    TickDeltaSecondsTotal = 0.0;
    MaxTickDeltaSeconds = 0.0;
    TickCount = 0;
    LongTickCount = 0;
}

void FUnrealServerMetricsCollector::AddCustomMetrics(TSharedRef<FJsonObject> Payload)
{
    for (const TFunction<void(TSharedRef<FJsonObject> Payload)>& Provider : CustomMetricsProviders)
    {
        Provider(Payload);
    }
}
