# PlayFab Multiplayer Unity Custom Game Server Sample

## Overview

This repository is the home of a custom game server example, made in Unity game engine, that shows how to create a game server that can talk to PlayFab's new multiplayer platform. There are a couple key components such as the MultiPlayer Agent API (GSDK) and a sample game client in this repository.  The purpose of this project is to provide you with an out-of-box example that you, as a game developer, can use as a starting point for your own game server.  In addition, we will identify in this document key parts of this project so that you can integrate what is needed into an existing project.

## Prerequisites

- Unity Engine (tested with 2019.2.18f1)
- Understanding of C# language
- Basic or Intermediate knowledge of Unity
- Recent version of the [PlayFab Game Server SDK](https://github.com/PlayFab/gsdk/tree/master/UnityGsdk) - included
- [PlayFab Unity SDK](https://github.com/PlayFab/UnitySDK) - included in the sample
- When running locally you must have a copy of [Local Multiplayer Agent](https://github.com/PlayFab/LocalMultiplayerAgent)
- It is recommended that you first run the [WindowsRunnerCSharp](https://github.com/PlayFab/gsdkSamples/blob/master/WindowsRunnerCSharp/README.md) sample to get acquainted with the GSDK.

## gsdk - Game Server SDK (aka Multiplayer Agent API)

Your game server uses the GSDK to talk to the Agent process that is running inside the Virtual Machine. The Agent's purpose is to communicate the game server status to the Multiplayer Servers service. Agent can also send notifications to game server to notify of shutdowns and maintenance schedules.

## Included Projects

There are two projects included in this repository. 
- UnityServer -  This is an example game server project that can talk to the Multiplayer Agent.  
- UnityClient - This is an example client that can talk to the UnityServer 

## Networking API

This sample uses [Mirror](https://github.com/vis2k/Mirror), the community replacement for Unity's UNET Networking System. You can see Mirror's documentation [here](https://mirror-networking.com/docs/General/index.html).

## Getting Started for Windows game servers using local Windows Containers

### Setup
1. Make sure you have Docker for Windows installed, check [here](https://docs.docker.com/docker-for-windows/install/) for installation instructions. You should have configured it to use Windows Containers. 
1. Download the [repository](https://github.com/PlayFab/gsdkSamples)
1. Open the two UnityMirror projects (UnityServer and UnityClient) and make sure they build. 

### Running the Server
1. Configure PlayFab integration.
    - This sample integrates with PlayFab API. To configure your credentials, you should use `Window->PlayFab->Editor Extensions` from the Unity IDE.
1. Build the UnityServer project.
    - Run Build from Unity IDE
        - Target platform = Windows
        - Architecture = x86_64
        - Server Build = \<checked\>
    - Navigate to the build output folder
    - Select all files and then right click and select "Send to -> Compressed (zipped) Folder"
    - Please bear in mind that you should not compress the folder containing the Unity output, just the files themselves.
1. Open the folder where you have downloaded the *LocalMultiplayerAgent* and modify the *MultiplayerSettings.json* file. Set the following values: 
    - RunContainers = `true`
    - OutputFolder = A local path with enough space on your machine. Useful data for your game will be written there.
    - LocalFilePath = Path to the zip created above of the build output for the server project. 
    - StartGameCommand = `C:\\Assets\\UnityServer.exe -nographics -batchmode -logFile C:\\GameLogs\\UnityEditor.log`
    - Within GamePort, set Number to 7777 and Protocol to TCP. Also make a note of the external port number, called NodePort
1. In PowerShell
    - Run the LocalMultiplayerAgentSetup file in *agentfolder/setup.ps1* (you may need to open Powershell with admin permissions for this purpose). If you get a signing violation, you may need to run `Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process`.
    - Run the Local Multiplayer Agent located in *agentfolder/MockVmAgent.exe*.
1. You will see the mock agent report state. Wait for reports to go from `Waiting for heartbeats from the game server.....` to `CurrentGameState: StandingBy` to `CurrentGameState: Active` then proceed to the next step

### Running the client
1. With the server running, open the UnityClient project in the Unity IDE.
1. Click the menu button PlayFab->MakePlayFabSharedSettings
1. In the PlayFabSharedSettings widget, set your [Title Id](https://docs.microsoft.com/en-us/gaming/playfab/personas/developer#retrieving-your-titleid). It is a hex number, generally 4 or 5 digits.
1. Ensure your server is still running and in the Active state from the step above then click File->Build/Run or Run inside Editor the client (UnityClient) project. Observe that the game is in *Active* state. When the server finishes execution, your client will disconnect as well.

## Debugging issues
- Make sure the server is still running. It will shutdown after `NumHeartBeatsForTerminateResponse` heartbeats. If you would like more time, you can increase this value.
- Check out the logs.  Client logs are available in the Unity IDE. Server logs are dropped in the `OutputFolder` specified in the MultiplayerSettings.json.
- Check out additional information about [Locally debugging game servers](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/locally-debugging-game-servers-and-integration-with-playfab)


## PlayFabMulitiplayerAgent API reference

**Start()** -  This initializes the GameObject into the scene that will talk to the Agent.  It also uses environment variables to know how to talk to the local agent

**ReadyForPlayers()** -- This tells the agent that your server is ready for Players.  Under the hood it is setting the agent current status to  StandingBy.  You typically want to call this once all your own initialization code has been started.  The server will stay in this state until the Multiplayer agent Service moves to Active.

**SetState(SessionHostStatus status)** -  This api tells the mutliplayer agent service that you are changing the state on the next heartbeat.  For example, if you are shutting down the game server, you can tell the agent and it will move to terminating status.

**AddPlayer(string playerId)** - Informs the multiplayer agent that a player has been added to the server.  PlayerId is arbitrary, but most common is to use the PlayFabId or EntityId of the player added.

**RemovePlayer(string playerId)** - Informs the multiplayer agent that a player has been removed or left the server.  PlayerId is arbitrary, but most common is to use the PlayFabId or EntityId of the player removed.

**GetConfigSettings()** -- Returns an object with a series of configuration values available to your server. Properties include the PlayFab TitleId, the ServerId, the Region for this server, etc.

<hr/>

## GSDK (MultiplayerAgentAPI) - Events

**OnMaintenanceCallback** - Listen to this event to get updates about when the server has scheduled maintenance.

**OnShutDownCallback** - Listen to this event to know when the server is shutting down

**OnServerActiveCallback** - Listen to this event to know when the state of the server has been moved to Active.  In general, you will want to startup your networking service layer when this event has been fired.   When active, that means you are truely ready to receive player connections.

**OnAgentErrorCallback** - If any error happens while trying to speak to the local agent

<hr/>

## Components

### AgentListener

In the code below, you'll notice that we are subscribing to events from the GSDK. 

We are doing the following tasks with this componenet

1. Start (init) the Agent API -  this means we are now talking to the agent every N seconds.
2. Listen for events
3. Notify the server that we are ready for players ( in standby mode )
4. When we receive a status of Active, we are starting the unity networking server.
5. Messages received are sent to connected clients


 ``` c#
using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;

public class AgentListener : MonoBehaviour {
    public UnityNetworkServer UNetServer;
    public bool Debugging = false;
    // Use this for initialization
    void Start () {
        _connectedPlayers = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        UNetServer.OnPlayerAdded.AddListener(OnPlayerAdded);
        UNetServer.OnPlayerRemoved.AddListener(OnPlayerRemoved);

        StartCoroutine(ReadyForPlayers());

    }

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }
    
    private void OnServerActive()
    {
        UNetServer.StartServer();
        Debug.Log("Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(string playfabId)
    {
        ConnectedPlayer player = _connectedPlayers.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
        _connectedPlayers.Remove(player);
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnPlayerAdded(string playfabId)
    {
        _connectedPlayers.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("Server is shutting down");
        foreach(var conn in UNetServer.Connections)
        {
            conn.Connection.Send(CustomGameServerMessageTypes.ShutdownMessage, new ShutdownMessage());
        }
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("Maintenance scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
        foreach (var conn in UNetServer.Connections)
        {
            conn.Connection.Send(CustomGameServerMessageTypes.ShutdownMessage, new MaintenanceMessage() {
                ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
            });
        }
    }
}
 ```

## Running Unity Server as a Linux executable

You can use Unity to publish a Linux executable for your game server. This will allow you to use it on PlayFab Multiplayer Services using Ubuntu Linux Virtual Machines. You can find the instructions to deploy Linux based builds [here](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/deploying-linux-based-builds). Furthermore, you can debug your Linux game server locally using the instructions [here](https://github.com/PlayFab/LocalMultiplayerAgent/blob/master/linuxContainersOnWindows.md).

To build the Unity Server as a Linux executable, you need to follow these instructions:

- [Install Docker Desktop on Windows](https://docs.docker.com/docker-for-windows/install/)
- Make sure it's running [Linux Containers](https://docs.docker.com/docker-for-windows/#switch-between-windows-and-linux-containers)
- You should mount one of your hard drives, instructions [here](https://docs.docker.com/docker-for-windows/#file-sharing)
- Open and publish UnityServer as a Linux executable. Do not forget to *tick* "Server Build" on the Build window
- As soon as your project is built, you need to make it into a container. Go to the folder where you published your Linux build and create the following Dockerfile, properly changing the file and folder names.

```Dockerfile
FROM ubuntu:18.04
WORKDIR /game
ADD . .
CMD ["/game/UnityServer.x86_64", "-nographics", "-batchmode"]
```

- You're now ready to build your image! If you want to develop locally, you can use `docker build -t myregistry.io/mygame:0.1 .` to build your game and test it with [LocalMultiplayerAgent](https://github.com/PlayFab/LocalMultiplayerAgent). Or, you can get the proper PlayFab container registry credentials (using [this](https://docs.microsoft.com/en-us/rest/api/playfab/multiplayer/multiplayerserver/getcontainerregistrycredentials?view=playfab-rest) API call or from the Builds page on PlayFab web site). Once you do that, you can `docker build/tag/push` your container image to the PlayFab container registry and spin game servers running it.
