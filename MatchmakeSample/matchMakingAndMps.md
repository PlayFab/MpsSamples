# Matchmaking and Multiplayer Servers integration

You can have Matchmaking integrate and allocate servers on PlayFab Multiplayer Servers, you can see the documentation [here](https://learn.microsoft.com/en-us/gaming/playfab/features/multiplayer/matchmaking/multiplayer-servers).

## Preview feature - getting the matchmaking queue name after a server allocation

We have recently launched a preview feature that allows you to get the matchmaking queue name after a server allocation. The way to retrieve the queue name is to search the config dictionary for the "PF_MATCH_QUEUE_NAME" string **after server is allocated**.

### C#

 - Make sure you grab the [latest version of the C# GSDK](https://www.nuget.org/packages/com.playfab.csharpgsdk)
 - Add the following code to your game server code:

 ```csharp
 if(GameserverSDK.ReadyForPlayers())
{
    // After allocation, we can grab the session cookie from the config
    activeConfig = GameserverSDK.getConfigSettings();

    // ...
    
    // if you are using matchmaking, this value will be set
    if (activeConfig.TryGetValue("PF_MATCH_QUEUE_NAME", out string matchQueueName))
    {
        LogMessage($"The queue name from matchmaking is: {matchQueueName}");
    }
}
 ```

 ### Unity

 - Make sure you grab the [latest version of the Unity GSDK](https://github.com/PlayFab/gsdk/tree/main/UnityGsdk)
 - Register the OnActive callback in your game server code:

```csharp
 void Start () {
    PlayFabMultiplayerAgentAPI.Start();
    PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
```

 - In the callback, get the queue name from the config:

 ```csharp
 private void OnServerActive()
{
    string queueName = PlayFabMultiplayerAgentAPI.GetConfigSettings()["PF_MATCH_QUEUE_NAME"];
}
```

## Support

This is a preview feature, so please reach out to us on [Discord](https://aka.ms/msftgamedevdiscord) on "multiplayer-servers" channel if you have any questions or feedback. 