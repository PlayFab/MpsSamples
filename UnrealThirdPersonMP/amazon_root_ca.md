# Accessing Epic Online Services

You may encounter issues when trying to access Epic Online Services from inside your game server process running on a Windows VM/container. Error message may look like the following:

```
LogEOS: Warning: [LogHttp] 000001A5C806C040: invalid HTTP response code received. URL: https://api.epicgames.dev/sdk/v1/default?platformId=WIN, HTTP code: 0, content length: 0, actual payload size: 0
LogEOS: Warning: [LogHttp] 000001A5C806C040: request failed, libcurl error: 60 (Peer certificate cannot be authenticated with given CA certificates)
LogEOS: Warning: [LogHttp] 000001A5C806C040: libcurl info message cache 23 (Hostname api.epicgames.dev was found in DNS cache)
LogEOS: Warning: [LogHttp] 000001A5C806C040: libcurl info message cache 24 ( Trying 35.173.6.230...)
LogEOS: Warning: [LogHttp] 000001A5C806C040: libcurl info message cache 25 (TCP_NODELAY set)
LogEOS: Warning: [LogHttp] 000001A5C806C040: libcurl info message cache 26 (Connected to api.epicgames.dev (35.173.6.230) port 443 (#5))
```

This is due to the fact that the VM/container running your game server executable does not contain the Amazon Root Certificate Authority (CA) certificate. The Epic Online Services API TLS certificate has been issued by this CA so its absence makes the connection from the VM/container fail.

> __**Note:**__ When the game server is executed as a process, Unreal looks for the certificate in the local VM. When the game server is executed as a container, Unreal looks for the certificate in the container.

To workaround this issue you need to install the certificate manually. You can use the following steps:

- Download the certs in the DER format from [Amazon Trust Services Repository](https://www.amazontrust.com/repository/)
- You probably only need the AmazonRootCA1 certificate, but you can install all of the following: RootCA1, RootCA2, RootCA3, RootCA4 and StarField
- Create a startup.cmd file with the following contents:
```bash
certutil.exe -addstore root .\AmazonRootCA1.cer
certutil.exe -addstore root .\AmazonRootCA2.cer
certutil.exe -addstore root .\AmazonRootCA3.cer
certutil.exe -addstore root .\AmazonRootCA4.cer
certutil.exe -addstore root .\SFSRootCAG2.cer
.\gameserver.exe
```
- The first lines install the Amazon Root certificates into the Windows Trusted Root certificate store. The last line starts the game server.
- Include the startup.cmd file and the *.cer files in your Assets file, when you are creating your Build
- Modify your StartGameCommand to call the startup.cmd file (e.g. `StartGameCommand=".\startup.cmd"`)
- Don't forget to test your game server with [LocalMultiplayerAgent](https://github.com/PlayFab/MpsAgent)