# WindowsRunnerCSharp
Sample executable that integrates with PlayFab's Multiplayer Gameserver SDK (GSDK).

## Dependencies
* PlayFab's Multiplayer Gameserver SDK (GSDK) - included.
* .Net Core 3.1

## Prerequisites
* Understanding of C#.
* Get [PlayFab's local debugging toolset](https://learn.microsoft.com/en-US/gaming/playfab/features/multiplayer/servers/locally-debugging-game-servers-and-integration-with-playfab).

## Overview
WindowsRunnserCSharp is a sample executable that integrates with PlayFab's Gameserver SDK (GSDK). It starts an http server that will respond to GET requests with a json file containing whatever configuration values it read from the GSDK.

WindowsRunnerCSharpClient is a sample client that integrates with PlayFab's SDK. It logs in a player, measures ping times to all Azure regions using the MPS QoS servers, allocates a server and "connects" to the server by issuing an http GET.

## Getting Started Guide

### To run this sample locally (server only)
1. Setup [PlayFab's local debugging toolset](https://learn.microsoft.com/en-US/gaming/playfab/features/multiplayer/servers/locally-debugging-game-servers-and-integration-with-playfab), following the *Verifying GSDK integration* section.
1. Once the local agent is running, open any browser, and navigate to `http://localhost:<port>`, where `<port>` is the NodePort set in your MultiplayerSettings.json (default 56100). You should see a json body with details about the game server.
1. The local deployment does not include the PlayFab service so the client will not be able to run locally

### To run this sample in Azure
#### Running the server
1. Build WindowsRunnerCSharp as **Release|x64** and place all generated binaries in a zipped folder. Make sure the files are all at the root of the zipped folder (not in a subfolder).
1. Follow the [quickstart guide](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/quickstart-for-multiplayer-servers-api-powershell), but instead of using the provided test server as an asset, use the zipped folder you created in step 1. Required Settings:
* Virtual Machine OS
    * Platform: Windows
    * Container Image: Windows Server Core
* Assets
    * Upload the asset package created above
    * Mount path: C:\Assets
* Start Command: C:\Assets\WindowsRunnerCSharp.exe
* Port configuration
    * Name: gameport
    * Number: \<any valid port\>
* Regions: Any region, but make sure standby servers is > 0

#### Running the client
Once the server is deployed and has reached standby. To run the sample client...
1. Follow the steps above to get the WindowsRunnerCSharp server deployed to Azure. Wait for the server to reach standing by
1. Record the title id and build id of your build
1. Build WindowsRunnerCSharpClient and navigate to the output folder
1. Run the WindowsRunnerCSharpClient with the command line `dotnet WindowsRunnerCSharpClient.dll --titleId <TitleId> --buildId <BuildId>`
```
Usage:
  WindowsRunnerCSharpClient [options]

Options:
  --titleId <titleid>      Your PlayFab titleId (Hex)
  --playerId <playerid>    Optional player id, if not specified a GUID will be used
  --buildId <buildid>      Build id, if not specified a GUID will be used
  --verbose                When passed, print verbose results
  --version                Display version information
```
