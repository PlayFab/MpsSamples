# WindowsRunnerCSharp
Sample executable that integrates with PlayFab's Multiplayer Gameserver SDK (GSDK).

## Dependencies
* PlayFab's Multiplayer Gameserver SDK (GSDK) - included.

## Prerequisites
* Understanding of C#.
* Get [PlayFab's local debugging toolset](https://api.playfab.com/docs/tutorials/landing-tournaments/multiplayer-servers-2.0/debugging-playfab-multiplayer-platform-integration-locally).

## Overview
This is a simple executable that integrates with PlayFab's Gameserver SDK (GSDK). It starts an http server that will respond to GET requests with a json file containing whatever configuration values it read from the GSDK.

## Features
* Accepts GET requests and returns a json body with the GSDK configuration values.
* Sets up the PlayFab GSDK.

## Getting Started Guide

### To run this sample locally
1. Setup [PlayFab's local debugging toolset](https://api.playfab.com/docs/tutorials/landing-tournaments/multiplayer-servers-2.0/debugging-playfab-multiplayer-platform-integration-locally), following the *Verifying GSDK integration* section.
1. Once the local agent is running, open any browser, and navigate to `http://localhost:<port>`, where `<port>` is the NodePort set in your MultiplayerSettings.json (default 56100). You should see a json body with details about the game server.

### To run this sample in Azure
1. Build this solution as **Release|x64** and place all generated binaries in a zipped folder. Make sure the files are all at the root of the zipped folder (not in a subfolder).
1. Follow the [quickstart guide](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/quickstart-for-multiplayer-servers-api-powershell), but instead of using the provided test server as an asset, use the zipped folder you created in step 1. Required Settings:
* Virtual Machine OS
    * Platform: Windows
    * Container Image: Windows Server Core
* Assets
    * Upload the asset package created above
    * Mount path: C:\Assets
* Start Command: C:\Assets\WindowsRunnerCSharp.exe
* Port configuration
    * Name: game_port
    * Number: \<any valid port\>
* Regions: Any region, but make sure standby servers is > 0

3. When you request a server, open a web browser window and type in the ip and port provided in the response to the New-PFMultiplayerServer command, you should see a json body with details about the sample server.

### To run the sample client
A sample client is provided which follows the standard flow for login, querying for QoS results, allocating a server, and connecting. To run the sample client...
1. Follow the steps above to get a client deployed to Azure
1. Run the WindowsRunnerCSharpClient by building and then typing `dotnet WindowsRunnerCSharpClient.dll --titleId <TitleId> --buildId <BuildId>`