# PlayFab Matchmaking and Multiplayer Servers integration

Azure PlayFab Matchmaking has the option to automatically create and allocate a game server on Azure PlayFab Multiplayer Servers after a match. To learn more, see [Integrating with PlayFab Multiplayer Servers](https://learn.microsoft.com/gaming/playfab/features/multiplayer/matchmaking/multiplayer-servers).

## Getting the matchmaking queue name after a server allocation

You can get the matchmaking queue name after a server allocation. The way to retrieve the queue name is to search the config dictionary for the "PF_MATCH_QUEUE_NAME" string **after server is allocated**.

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

 ### Unreal Engine

 - Make sure you grab the [latest version of the Unreal Engine GSDK](https://github.com/PlayFab/gsdk/tree/main/UnrealPlugin)
 - Register the OnGSDKServerActive callback in your game server code. See the following code sample as well as the setup guide here:
 [GSDK project setup](https://github.com/PlayFab/gsdk/blob/main/UnrealPlugin/ThirdPersonMPGSDKSetup.md)

 ```cpp
    // Add this code in the Init method of your GameInstance class
    FOnGSDKServerActive_Dyn OnGSDKServerActive;
    OnGSDKServerActive.BindDynamic(this, &YourGameInstanceClassName::OnGSDKServerActive);
 ```

  - In the callback, get the queue name from the config:

```cpp
void UShooterGameInstance::OnGSDKServerActive()
{
    FString queueName = UGSDKUtils::GetConfigValue("PF_MATCH_QUEUE_NAME");
}
```


## Support

This is a preview feature, so please reach out to us on [Discord](https://aka.ms/msftgamedevdiscord) on "playfab-chat" channel if you have any questions or feedback. 
