In this documentation, we will cover 
1. How to build the wrapper game sample.
2. How to create zipped achieve easily with the script.
3. How to create and deploy Linux Conainer wrapper game sample to Multiplayer Servers(MPS)
4. How to use LocalMultiplayer Agent with Linux Container Wrapper sample.


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

> If PowerShell throws up an error message – <b>File cannot be loaded because running scripts is disabled on this system</b>, then you need to enable the `build.ps1` script to run on your Windows computer.  You can enable this by running the following PowerShell command in Administrative mode in advance of running `build.ps1`:

```
Set-ExecutionPolicy Unrestricted
```

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

### Using a Linux Build 

You can run the `wrapper` and the `fakegame` executable on a Linux Build, using Linux containers. 

1. Make sure you have an account on [playfab.com](https://www.playfab.com) and have enabled Multiplayer Servers
2. Git clone this repo. You should have installed [Docker](https://docs.docker.com/get-docker/) 
3. On the PlayFab developer portal, you can create a new Linux Build to get the Azure Container Registry information. It will be appear in the Multiplayer page like `docker login --username customervz4l34rmt7rnk --password XXXXXXX customervz4l34rmt7rnk.azurecr.io`. You can also use [the GetContainerRegistryCredentials API call](https://docs.microsoft.com/en-gb/rest/api/playfab/multiplayer/multiplayerserver/getcontainerregistrycredentials?view=playfab-rest) to get the ACR credentials
4. Replace the *TAG* and the *ACR* variables with your values
```bash
TAG="0.1"
ACR="customervz4l34rmt7rnk.azurecr.io"
docker login --username XXXXXX --password XXXXXXX ${ACR}
docker build -t ${ACR}/wrapper:${TAG} .
docker push ${ACR}/wrapper:${TAG}
```
You can run the above script on [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/wsl2-index).

5. Create a new MPS Build either via playfab.com or via [the CreateBuildWithCustomerContainer API call](https://docs.microsoft.com/en-gb/rest/api/playfab/multiplayer/multiplayerserver/createbuildwithcustomcontainer?view=playfab-rest). On this Build, select Linux VMs, the image:tag container image you uploaded and a single port for the game. If you are using the `fakegame`, you should pick 80/TCP. 
6. Wait for the Build to be deployed
7. To allocate a server and get IP/port, you can use the [MpsAllocator sample](../MpsAllocatorSample/README.md) or use [RequestMultiplayerServer](https://docs.microsoft.com/en-gb/rest/api/playfab/multiplayer/multiplayerserver/requestmultiplayerserver?view=playfab-rest) API call. For more information you can check the [documentation](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers)

> Of course, you can still replace `fakegame` with your game server. If you choose to do so, you'll need to modify the [Dockerfile](./Dockerfile) appropriately.

### Running the wrapper locally using the LocalMultiplayerAgent

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
