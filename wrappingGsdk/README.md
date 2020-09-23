# Wrapping an existing game using GSDK

There are cases in which you want to wrap an existing game with a custom process that uses Multiplayer Servers Game Server SDK (GSDK). Specifically, this process will spawn your game server executable and integrate with GSDK. You may want to create this wrapper for one of the following reasons:

- you already have an existing game (or building a new one) and you want to try Multiplayer Servers service with the minimum possible effort
- you want to evaluate the MPS platform (in this case you may also see our [OpenArena sample](./OpenArena/README.md))

> This sample and corresponding technique is NOT meant to be used in production but only for evaluation/development purposes. Proper integration of your game server with the GSDK is highly recommended.

The samples require .NET Core 3.1 SDK, you can download it [here](https://dotnet.microsoft.com/download).

## Wrapping an existing game server using the wrapper app

To get started, you can find two .NET Core projects in the current folder. 
- `wrapper` is a .NET Core console application that acts as a wrapper for your game server and integrates with GSDK using the [latest Nuget package](https://www.nuget.org/packages/com.playfab.csharpgsdk)
- `fakegame` is a .NET Core console application that starts [Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.1) ASP.NET Core Web Server that listens to `/Hello` endpoint. It's meant to simulate a game server that has absolutely zero knowledge of GSDK. You can use it if you don't have a game server of your own.

### Building the wrapper

To build the `wrapper` app you should use the following .NET Core CLI command from inside the `wrapper` directory:

```bash
dotnet publish --self-contained -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The `wrapper` executable (as well as its debugging symbols) will be published into `wrapper\bin\Debug\netcoreapp3.1\win-x64\publish` directory.

Next step would be to build your game server executable and assets. As mentioned, if you're just evaluating the platform or don't have a game server of your own, you can use `fakegame` sample. In this case, we have provided a convenient script [build.ps1](./build.ps1) that you can run to publish and zip both executables in the `drop` folder. 

### Creating the zipped game assets archive

#### If you are using build.ps1 script

You can use the zipped file that was created from `build.ps1` script to create a new MPS Build.

#### If you are using your own game server

You should copy all your game server build files wherever your `wrapper` executable is located. There, you need to zip the entire contents of the folder. This is the zip file that you will use with `LocalMultiplayerAgent` (if you are doing local development) and upload onto the MPS Service (if you need to deploy your game server on MPS).

> A common mistake new users do is that they compress the `publish` directory. This may create issues if you use incorrect mapping, so we highly recommend you zip the contents of the `publish` folder instead.

### Running the wrapper

If you are using [LocalMultiplayerAgent](https://github.com/PlayFab/LocalMultiplayerAgent) with Windows Containers you need to properly configure `MultiplayerSettings.json` file. You can find an example below, pay special attention to the values of `LocalFilePath` and `StartGameCommand`. Don't forget to replace `fakegame.exe` with the name of your game server executable.

```json
...
"AssetDetails": [
    {
        "MountPath": "C:\\Assets",
        "SasTokens": null,
        "LocalFilePath": "C:\\projects\\gsdkSamples\\wrappingGsdk\\drop\\gameassets.zip"
    }
],
"StartGameCommand": "C:\\Assets\\wrapper.exe -g C:\\Assets\\fakegame.exe",
...
// if you are using fakegameserver you should also configure port mapping for port 80
"PortMappingsList": [
            [
                {
                    "NodePort": 56100,
                    "GamePort": {
                        "Name": "game_port",
                        "Number": 80,
                        "Protocol": "TCP"
                    }
                }
            ]
        ]
```

You are now ready to test with `LocalMultiplayerAgent`! If you have configured it correctly, as soon as `LocalMultiplayerAgent` launches your game server, you can connect to it via `curl http://localhost:56100/Hello`.

#### Deploying on MPS

You can use our [public documentation](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/deploying-playfab-multiplayer-server-builds) to upload your Build on the MPS service. During the creation of your Build you should upload the zipped game assets you created and use a StartGameCommand like this one:

```
# replace fakegame.exe with the name of your game server executable
C:\Assets\wrapper.exe -g C:\Assets\fakegame.exe arg1 arg2
```