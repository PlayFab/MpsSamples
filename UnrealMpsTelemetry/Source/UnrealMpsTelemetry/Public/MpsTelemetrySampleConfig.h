#pragma once

#include "CoreMinimal.h"

namespace PlayFab
{
namespace Samples
{
namespace UnrealMpsTelemetry
{
    struct FMpsTelemetrySampleConfig
    {
        static constexpr double MetricsSummaryIntervalSeconds = 60.0;
        static constexpr double TelemetryFlushIntervalSeconds = 60.0;
        static constexpr double LongTickThresholdMilliseconds = 100.0;
        static constexpr int32 MaxEventsPerBatch = 200;
        static constexpr int32 MaxQueuedEvents = 1000;
        static constexpr bool LogTelemetryPayloads = false;

        static const TCHAR* TelemetryKeyPlaceholder()
        {
            return TEXT("PASTE_YOUR_PLAYFAB_TELEMETRY_KEY_HERE");
        }

        static const TCHAR* MpsSecretTelemetryKeyEnvironmentVariable()
        {
            return TEXT("PF_MPS_SECRET_TelemetryKey");
        }

        static const TCHAR* TelemetryKeyEnvironmentVariable()
        {
            return TEXT("PLAYFAB_TELEMETRY_KEY");
        }

        static const TCHAR* EventNamespace()
        {
            return TEXT("custom.mps.unrealserver");
        }

        static const TCHAR* TelemetryKeySourceOverride()
        {
            return TelemetryKeyPlaceholder();
        }

        static FString GetTelemetryKey()
        {
            FString MpsSecretTelemetryKey = FPlatformMisc::GetEnvironmentVariable(MpsSecretTelemetryKeyEnvironmentVariable());
            if (IsConfiguredTelemetryKey(MpsSecretTelemetryKey))
            {
                return MpsSecretTelemetryKey;
            }

            FString EnvironmentTelemetryKey = FPlatformMisc::GetEnvironmentVariable(TelemetryKeyEnvironmentVariable());
            return IsConfiguredTelemetryKey(EnvironmentTelemetryKey)
                ? EnvironmentTelemetryKey
                : FString(TelemetryKeySourceOverride());
        }

        static bool IsConfiguredTelemetryKey(const FString& TelemetryKey)
        {
            return !TelemetryKey.IsEmpty() && TelemetryKey != TelemetryKeyPlaceholder();
        }
    };
}
}
}
