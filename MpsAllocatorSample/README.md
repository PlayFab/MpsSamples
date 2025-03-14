# MpsAllocatorSample

This sample is a simple .NET Core application that allows you to easily call some frequently used MPS APIs, like list VMs/servers and allocate (RequestMultiplayerServer). In order to use it, you need to have installed [.NET Core installed](https://dotnet.microsoft.com/download/dotnet). You can then use either [dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build) command to build an executable or just run `dotnet run` to run the application.

To use it, you need to provide your PlayFab TitleID plus a developer secret key.
You can either set them via environment variables (PF_TITLEID and PF_SECRET respectively) or type them during the execution of the program. To get a secret key for your title, visit `https://developer.playfab.com/en-US/r/t/<Your_TitleID>/settings/secret-keys`.

The app uses the PlayFab SDK via [Nuget](https://www.nuget.org/packages/PlayFabAllSDK/).

Questions? Please open an [issue](https://github.com/PlayFab/MpsSamples/issues) and we'll get back to you ASAP.