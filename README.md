# MpsSamples

This repository contains samples that show how to use Azure PlayFab Multiplayer Servers Gameserver SDK (GSDK) as well as additional resources.

## Wrapping GSDK

You could try GSDK without modifying your game server (even though we don't recommend it for production deployments). You can find instructions [here](wrappingGSDK/README.md).

## MpsAllocatorSample

This is a simple .NET Core console app that lets use easily see your MPS Builds/Game Servers/VMs plus allocate a game server (uses the RequestMultiplayerServer API call). More information [here](MpsAllocatorSample/README.md).

## UnityMirror

Unity Server and Client sample that utilize the GameServer SDK.

More information [here](UnityMirror/README.md).

## OpenArena

This sample wraps the open source [OpenArena](https://openarena.fandom.com/wiki/Main_Page) game using a .NET Core app and Linux containers.

More information [here](openarena/README.md).

## Debugging Docker containers

MPS service uses Docker containers to schedule game servers. You can see some **advanced** debugging/diagnosing instructions [here](./Debugging.md).

## Matchmake Sample

The Matchmake sample logs in a configurable number of clients and attempts to matchmake them together, following the steps described in the [Single user ticket matchmaking](https://docs.microsoft.com/gaming/playfab/features/multiplayer/matchmaking/quickstart#single-user-ticket-matchmaking) sample.

## WindowsRunnerCSharp

Simple executable that integrates with PlayFab's Gameserver SDK (GSDK). It starts an http server that will respond to GET requests with a json file containing whatever configuration values it read from the GSDK. More information [here](WindowsRunnerCSharp/README.md).

## Questions

If you have any questions, feel free to engage in the repo's discussions [here](https://github.com/PlayFab/MpsSamples/discussions)or find us in Discord [here](https://discord.com/invite/gamestack)

## Community samples

Here you can find a list of samples and utilities created and supported by our community. [Let us know](https://github.com/PlayFab/gsdkSamples/issues) if you have created a sample yourself and would like to have it mentioned here.

- [PlayFabMirrorGameExample](https://github.com/natepac/playfabmirrorgameexample) by [natepac](https://github.com/natepac): Another sample using Mirror and Unity
- [PFAdmin](https://github.com/bphillips09/PFAdmin) by [bphilips09](https://github.com/bphillips09): An Admin API GUI application for PlayFab
- [PlayFab UE4 GSDK Integration](https://github.com/narthur157/playfab-gsdk-ue4) by [narthur157](https://github.com/narthur157): a plugin providing a simple GSDK integration for Unreal Engine 4
- [Integrating Photon with PlayFab Multiplayer Servers](https://doc.photonengine.com/en-us/bolt/current/demos-and-tutorials/playfab-integration/overview)
- [Unity Multiplayer - Top to Bottom](https://dev.to/robodoig/unity-multiplayer-bottom-to-top-46cj) A blog post that shows how to use Unity with DarkRift and Multiplayer Servers
