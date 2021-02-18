# Wrapping an existing game using GSDK

There are cases in which you want to wrap an existing game with a custom process that uses Multiplayer Servers Game Server SDK (GSDK). Specifically, the process created by the `wrapper` project integrates with GSDK and is responsible for spawning your game server executable. You may want to create this wrapper for one of the following reasons:

- you already have an existing game (or building a new one) and you want to try Multiplayer Servers service with the minimum possible effort
- you want to evaluate the MPS platform (in this case you may also see our [OpenArena sample](./OpenArena/README.md))

> This sample and corresponding technique is NOT recommended for use in production but only for evaluation/development purposes. Proper integration of your game server with the GSDK is highly recommended.

The samples require .NET Core 3.1 SDK, you can download it [here](https://dotnet.microsoft.com/download). Usage of [Visual Studio Code](https://code.visualstudio.com/) is also highly recommended.

## Wrapping an existing game server using the wrapper app

To get started, you can find two .NET Core projects in the current folder. 
- `wrapper` is a .NET Core console application that acts as a wrapper for your game server and integrates with GSDK using the [latest Nuget package](https://www.nuget.org/packages/com.playfab.csharpgsdk)
- `fakegame` is a .NET Core console application. It's meant as literally a `fake game`, it just starts ASP.NET Core Web Server [Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.1) that listens to TCP port 80. It's meant to simulate a game server that has absolutely zero knowledge of GSDK. You can use it if you don't have a game server of your own. It has two GET routes we can use, `/hello` for getting a simple response and `/hello/terminate` that can terminate the server.

### Building the wrapper

To build the `wrapper` app you should use the following .NET Core CLI command from inside the `wrapper` directory:

```bash
dotnet publish --self-contained -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The `wrapper` executable (as well as its debugging symbols) will be published into `wrapper\bin\Debug\netcoreapp3.1\win-x64\publish` directory. Next step would be to build your game server executable and package it along the `wrapper` output.

As mentioned, if you're just evaluating the platform or don't have a game server of your own, you can use `fakegame` sample. In this case, we have provided a convenient script [build.ps1](./build.ps1) that will build and package both projects (`wrapper` and `fakegame`). Script will create a `drop` folder with a .zip file containing the required files. 

### Creating the zipped game assets archive

#### If you are using build.ps1 script

You can use the zipped file that was created from `build.ps1` script during your creation of a new MPS Build.

#### If you are using your own game server

You should copy all your game server build files from wherever your `wrapper` executable is located (if you used the aforementioned `dotnet publish` command, files should be on `wrapper\bin\Debug\netcoreapp3.1\win-x64\publish` directory). You would need to zip the contents of this folder along with the files necessary to run your game server. This is the zip file that you will use with `LocalMultiplayerAgent` (if you are doing local development) and/or upload onto the MPS Service (if you need to deploy your game server on MPS service).

> A common mistake new users do is that they compress the `publish` directory. This may create issues if you use incorrect mapping, so we highly recommend you zip the **files** of the `publish` folder instead.

#### Deploying on MPS

You can use our [public documentation](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/deploying-playfab-multiplayer-server-builds) to upload your Build on the MPS service. During the creation of your Build you should upload the zipped game assets you created and use a StartGameCommand like this one:

```
# replace fakegame.exe with the name of your game server executable
C:\Assets\wrapper.exe -g C:\Assets\fakegame.exe arg1 arg2
```

You should use `port 80 TCP` in the Build configuration. Bear in mind that during the allocation (i.e. usage of RequestMultiplayerServer API) the port you will get to connect will be different than 80. This is becase MPS service will create a mapping between the Azure Load Balancer (that exposes your ports to the Public internet) to the game servers running on the Azure Virtual Machines.

### Running the wrapper using the LocalMultiplayerAgent

> Using LocalMultiplayerAgent is highly recommended if you want to test GSDK integration on your custom game servers.

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

### Next steps

You should run the [MpsAllocatorSample](../MpsAllocatorSample/README.md) to allocate/list/view Builds and game servers.