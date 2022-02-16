
# Wrapping an existing game using GSDK

Welcome to visit MPS Samples Page!  

We will go through the steps from building existing wrapper game to deploying it to Multiplayer Servers (MPS) Cloud.

## What is this Wrapper for?

There are cases in which you want to wrap an existing game with a custom process that uses Multiplayer Servers Game Server SDK (GSDK). Specifically, the process created by the `wrapper` project integrates with GSDK and is responsible for spawning your game server executable. You may want to create this wrapper for one of the following reasons:

- you already have an existing game (or building a new one) and you want to try Multiplayer Servers service with the minimum possible effort
- you want to evaluate the MPS platform (in this case you may also see our [OpenArena sample](./OpenArena/README.md))

> This sample and corresponding technique is NOT recommended for use in production but only for evaluation/development purposes. Proper integration of your game server with the GSDK is highly recommended.

The samples require .NET Core 3.1 SDK, you can download it [here](https://dotnet.microsoft.com/download). Usage of [Visual Studio Code](https://code.visualstudio.com/) is also highly recommended.

## Wrapping an existing game server using the wrapper app

To get started, you can find two .NET Core projects in the current folder. 
- `wrapper` is a .NET Core console application that acts as a wrapper for your game server and integrates with GSDK using the [latest Nuget package](https://www.nuget.org/packages/com.playfab.csharpgsdk)
- `fakegame` is a .NET Core console application. It's meant as literally a `fake game`, it just starts ASP.NET Core Web Server [Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.1) that listens to TCP port. It's meant to simulate a game server that has absolutely zero knowledge of GSDK. You can use it if you don't have a game server of your own. It has two GET routes we can use, `/hello` for getting a simple response and `/hello/terminate` that can terminate the server.


## Build Your Wrapper sample 

- Build and Deploy Wrapper sample as Process   
Please check out the readme under [WrapperProcess](/ProcessSample/README.md) and learn how to build wrapper samples.

- Create a Linux Container with Wrapper sample  
If you want to learn how to creat a Linux build with wrapper game sample, 
please check out readme under [WrapperContainer](/ContainerSample/README.md). 
