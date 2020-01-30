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

### To run this sample in Azure
1. Build this solution as **Release|x64** and place all generated binaries in a zipped folder. Make sure the files are all at the root of the zipped folder (not in a subfolder).
2. Follow our [quickstart guide](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/quickstart-for-multiplayer-servers-api-powershell), but instead of using the provided test server as an asset, use the zipped folder you created in step 1.
3. When you request a server, open a web browser window and type in the ip and port provided in the response to the New-PFMultiplayerServer command, you should see a json body with details about the sample server.

### To run this sample locally
1. Setup [PlayFab's local debugging toolset](https://api.playfab.com/docs/tutorials/landing-tournaments/multiplayer-servers-2.0/debugging-playfab-multiplayer-platform-integration-locally), following the *Verifying GSDK integration* section.
2. Once the local agent is running, open any browser, and navigate to `http://localhost:<port>`, where `<port>` is the NodePort set in your MultiplayerSettings.json (default 56100). You should see a json body with details about the game server.

## Known Limitations
* Currently, this sample will only work locally if you are also running the local agent. If you see a GSDK Exception on initialization, make sure you are running via the local agent.