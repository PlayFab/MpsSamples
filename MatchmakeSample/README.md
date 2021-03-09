# MatchmakeSample

## Dependencies
* .Net Core 3.1

## Prerequisites
* Understanding of C#
* PlayFab Title with Matchmaking enabled

## Overview
MatchmakeSample is a sample executable that integrates with PlayFab's Matchmaking system. It logs in a configurable number of clients and attempts to matchmake them together, following the steps described in the [Single user ticket matchmaking](https://docs.microsoft.com/gaming/playfab/features/multiplayer/matchmaking/quickstart#single-user-ticket-matchmaking) sample.

## Getting Started Guide

### Running this sample locally
1. Get your PlayFab TitleId and a Matchmake queue name for your title
1. Build the solution, navigate to the binary output folder, and find the MatchmakeSample.dll output file
1. Run the MatchmakeSample with the command line:
    1. Depending on the number of players chosen and your queue settings, they may be matched into one or multiple groups

```dotnet MatchmakeSample.dll --titleId <Your TitleId> --numPlayers 10 --mmQueueName <Name of Matchmake queue for your title>```

With no parameters, this command will show these options:
```
Option '--titleId' is required.
Option '--numPlayers' is required.
Option '--mmQueueName' is required.

Usage:
  MatchmakeSample [options]

Options:
  --titleId <titleid>            Your PlayFab titleId (Hex)
  --numPlayers <numplayers>      Number of players to create
  --mmQueueName <mmqueuename>    Name of Matchmaking Queue to join
  --version                      Display version information
```
