In this documentaton, we will cover 
1. How to build the wrapper game sample.
2. How to create zipped achieve easily with the script.
3. How to deploy process-based wrapper game sample to Multiplayer Servers(MPS)
4. How to use LocalMultiplayer Agent with wrapper sample.

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
wrapper.exe -g fakegame.exe arg1 arg2
```

You should use `PortName:RealPort Protocol:TCP` in the Build configuration. 
Bear in mind that during the allocation (i.e. usage of RequestMultiplayerServer API), a Port number will be assigned to your game server via GSDK.
In wrapper sample, you can check how game server grabs the port number from GSDK configuration.
This is becase MPS service will create a mapping between the Azure Load Balancer (that exposes your ports to the Public internet) to the game servers running on the Azure Virtual Machines.


### Running the wrapper locally using the LocalMultiplayerAgent

> Using LocalMultiplayerAgent is highly recommended if you want to test GSDK integration on your custom game servers.

If you are using [LocalMultiplayerAgent](https://github.com/PlayFab/LocalMultiplayerAgent) with Windows Containers you need to properly configure `MultiplayerSettings.json` file. You can find an example below, pay special attention to the values of `LocalFilePath` and `StartGameCommand`. Don't forget to replace `fakegame.exe` with the name of your game server executable.

```json
...
"AssetDetails": [
    {
        "MountPath": "",
        "SasTokens": null,
        "LocalFilePath": "C:\\projects\\gsdkSamples\\wrappingGsdk\\drop\\gameassets.zip"
    }
],
"StartGameCommand": "wrapper.exe -g fakegame.exe",
...
// if you are using fakegameserver you should also configure port mapping for port 80
"PortMappingsList": [
            [
                {
                    "NodePort": 56100,
                    "GamePort": {
                        "Name": "RealPort",
                        "Number": 80, // port number will be ignored unless you run LocalMultiplayer Agent for Container mode.
                        "Protocol": "TCP"
                    }
                }
            ]
        ]
```

You are now ready to test with `LocalMultiplayerAgent`! If you have configured it correctly, as soon as `LocalMultiplayerAgent` launches your game server, you can connect to it via `curl http://localhost:56100/Hello`.

### Next steps

You should run the [MpsAllocatorSample](../MpsAllocatorSample/README.md) to allocate/list/view Builds and game servers.