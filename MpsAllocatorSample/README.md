# MpsAllocatorSample

This sample is a simple .NET Core application that allows you to easily call some frequently used MPS APIs, like list VMs/servers and allocate (RequestMultiplayerServer). In order to use it, you need to have installed [.NET Core installed](https://dotnet.microsoft.com/download/dotnet-core). You can then use either [dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build) command to build an executable or just run `dotnet run` to run the application.

To use it, you need to provide your PlayFab TitleID plus a developer secret key. The app uses the PlayFab SDK via [Nuget](https://www.nuget.org/packages/PlayFabAllSDK/).