using UnrealBuildTool;

public class UnrealMpsTelemetry : ModuleRules
{
    public UnrealMpsTelemetry(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicDependencyModuleNames.AddRange(
            new[]
            {
                "Core",
                "CoreUObject",
                "Engine",
                "HTTP",
                "Json"
            });
    }
}
