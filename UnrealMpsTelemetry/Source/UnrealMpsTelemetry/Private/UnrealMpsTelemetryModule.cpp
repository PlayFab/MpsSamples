#include "Modules/ModuleManager.h"
#include "MpsTelemetrySubsystem.h"

DEFINE_LOG_CATEGORY(LogMpsTelemetrySample);

class FUnrealMpsTelemetryModule final : public IModuleInterface
{
};

IMPLEMENT_MODULE(FUnrealMpsTelemetryModule, UnrealMpsTelemetry)
