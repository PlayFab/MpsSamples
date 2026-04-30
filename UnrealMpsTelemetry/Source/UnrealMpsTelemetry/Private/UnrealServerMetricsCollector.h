#pragma once

#include "CoreMinimal.h"

class FJsonObject;

class FUnrealServerMetricsCollector
{
public:
    void Initialize();
    void Tick(float DeltaTime);
    void RegisterCustomMetricsProvider(TFunction<void(TSharedRef<FJsonObject> Payload)> Provider);
    TSharedRef<FJsonObject> CaptureSummaryAndReset();

private:
    void ResetIntervalCounters();
    void AddCustomMetrics(TSharedRef<FJsonObject> Payload);

    TArray<TFunction<void(TSharedRef<FJsonObject> Payload)>> CustomMetricsProviders;
    double StartedSeconds = 0.0;
    double TickDeltaSecondsTotal = 0.0;
    double MaxTickDeltaSeconds = 0.0;
    int32 TickCount = 0;
    int32 LongTickCount = 0;
};
