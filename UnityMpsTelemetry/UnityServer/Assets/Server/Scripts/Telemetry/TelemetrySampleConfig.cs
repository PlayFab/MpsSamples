namespace PlayFab.Samples.UnityMpsTelemetry
{
    public static class TelemetrySampleConfig
    {
        // Keep the placeholder obviously fake so a real telemetry key is not committed by accident.
        public const string TelemetryKeyPlaceholder = "PASTE_YOUR_PLAYFAB_TELEMETRY_KEY_HERE";

        // In PlayFab MPS, prefer storing the telemetry key as a managed secret named
        // "TelemetryKey". MPS exposes that secret to the server as PF_MPS_SECRET_TelemetryKey.
        public const string MpsSecretTelemetryKeyEnvironmentVariable = "PF_MPS_SECRET_TelemetryKey";

        // Use this fallback when you provide the key through your own deployment environment.
        // For quick testing only, you can temporarily replace TelemetryKeySourceOverride below.
        public const string TelemetryKeyEnvironmentVariable = "PLAYFAB_TELEMETRY_KEY";

        // PlayFab telemetry event namespaces allow dots, but not underscores.
        public const string EventNamespace = "custom.mps.unityserver";

        // Source override is intentionally a placeholder. It is convenient for temporary validation,
        // but production servers should pass this value through environment/deployment config.
        private const string TelemetryKeySourceOverride = TelemetryKeyPlaceholder;

        // Sample cadence: collect cheap runtime samples locally, then emit one compact summary.
        public const float MetricsSampleIntervalSeconds = 5f;
        public const float MetricsSummaryIntervalSeconds = 60f;
        public const float TelemetryFlushIntervalSeconds = 60f;
        public const float ShutdownFlushTimeoutSeconds = 10f;
        public const float TelemetryRetryDelaySeconds = 1f;
        public const float LongFrameThresholdMilliseconds = 100f;

        // Batch and queue limits prevent telemetry failures from growing memory without bound.
        public const int MaxEventsPerBatch = 200;
        public const int MaxQueuedEvents = 1000;

        // Useful while developing the sample, but usually too noisy for a running fleet.
        public static readonly bool LogTelemetryPayloads = false;

        public static bool HasTelemetryKey
        {
            get
            {
                return IsConfiguredTelemetryKey(TelemetryKey);
            }
        }

        public static string TelemetryKey
        {
            get
            {
                // Prefer MPS managed secrets so keys do not need to live in source code,
                // container images, command-line arguments, or Unity serialized assets.
                string mpsSecretTelemetryKey =
                    System.Environment.GetEnvironmentVariable(MpsSecretTelemetryKeyEnvironmentVariable);
                if (IsConfiguredTelemetryKey(mpsSecretTelemetryKey))
                {
                    return mpsSecretTelemetryKey;
                }

                string environmentTelemetryKey = System.Environment.GetEnvironmentVariable(TelemetryKeyEnvironmentVariable);
                return IsConfiguredTelemetryKey(environmentTelemetryKey)
                    ? environmentTelemetryKey
                    : TelemetryKeySourceOverride;
            }
        }

        public static bool IsConfiguredTelemetryKey(string telemetryKey)
        {
            return !string.IsNullOrWhiteSpace(telemetryKey)
                && telemetryKey != TelemetryKeyPlaceholder;
        }
    }
}
