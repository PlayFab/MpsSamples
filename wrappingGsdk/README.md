# Wrapping an existing game using GSDK

There are cases in which you want to wrap an existing game with a custom process that uses Multiplayer Servers Game Server SDK (GSDK). Specifically, this process will spawn your game server executable and integrate with GSDK. You may want to create this wrapper for one of the following reasons:

- you already have an existing game (or building a new one) and you want to try Multiplayer Servers service with the minimum possible effort
- you want to evaluate the MPS platform (in this case you may also see our [OpenArena sample](./OpenArena/README.md))

> This sample and corresponding technique is NOT meant to be used in production but only for evaluation/development purposes. Proper integration of your game server with the GSDK is highly recommended.

## Wrapping an existing game server using the wrapper app

To get started, you can find two .NET Core projects in the current folder. 
- `wrapper` is a .NET Core console application that acts as a wrapper for your game server and integrates with GSDK using the [latest Nuget package](https://www.nuget.org/packages/com.playfab.csharpgsdk)
- `fakegame` is a .NET Core console application that keeps itself alive for ever. It's meant to simulate a game server that has absolutely zero knowledge of GSDK. You can use it if you don't have a game server of your own.

### Building the wrapper

To build the `wrapper` app you should use the following .NET Core CLI command from inside the `wrapper` directory:

```bash
dotnet publish --self-contained -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The `wrapper` executable (as well as its debugging symbols) will be published into `wrapper\bin\Debug\netcoreapp3.1\win-x64\publish` directory.

Next step would be to build your game server executable and assets. As mentioned, if you're just evaluating the platform or don't have a game server of your own, you can use `fakegame` sample. In this case, you should also run the previous `dotnet publish` command in the `fakegame` folder in order to generate your fake game server executable.

### Creating the zipped game assets archive

You should copy all your game server build files into the `wrapper\bin\Debug\netcoreapp3.1\win-x64\publish` directory (or wherever your `wrapper` executable is located). There, you need to zip the entire contents of the folder.

> A common mistake new users do is that they compress the `publish` directory. This may create issues if you use incorrect mapping, so we highly recommend you zip the contents of the `publish` folder.

### Running the wrapper

You are ready! If you are using [LocalMultiplayerAgent](https://github.com/PlayFab/LocalMultiplayerAgent) with Windows Containers you need to properly configure `MultiplayerSettings.json` file. You can find an example below, pay special attention to the values of `LocalFilePath` and `StartGameCommand`. Don't forget to replace `fakegame.exe` with the name of your game server executable.

```json
...
"AssetDetails": [
    {
        "MountPath": "C:\\Assets",
        "SasTokens": null,
        "LocalFilePath": "C:\\projects\\gsdkSamples\\wrappingGsdk\\wrapper\\bin\\Debug\\netcoreapp3.1\\win-x64\\publish\\fakegameassets.zip"
    }
],
"StartGameCommand": "C:\\Assets\\wrapper.exe -g C:\\Assets\\fakegame.exe",
...
```

If you want to deploy your game on Multiplayer Servers service, you should upload your .zip asset and use a StartGameCommand like this one:

```
# replace fakegame.exe with the name of your game server executable
C:\Assets\wrapper.exe -g C:\Assets\fakegame.exe
```