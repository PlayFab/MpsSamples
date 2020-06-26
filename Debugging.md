# Debugging your Game Servers

Sometimes your game servers cannot start and the Builds are showing up as Unhealthy. In this case, you'd like to see debug your game server running on PlayFab Multiplayer Servers Virtual Machine. Or, sometimes, even though your game server is running smoothly, you may need to connect to the Virtual Machine (VM) and see various statistics/logs about your game server.

In order to connect to the VM hosting your game server (either Windows or Linux), you can get RDP/SSH credentials using [CreateRemoteUser](https://docs.microsoft.com/en-us/rest/api/playfab/multiplayer/multiplayerserver/createremoteuser?view=playfab-rest) API call or using the "Connect" button on playfab.com web application.

As soon as you connect to the VM, you can use the console of the operating system to monitor your game servers. Remember that MPS service is natively using Docker containers on both Windows and Linux to spin up game server processes. To run Docker CLI commands, you'll need an admin powershell on Windows and probably need to `sudo su -` on Linux.

**These are advanced Docker container debugging instructions, usage of them might break your game servers. You should not use these instructions unless you absolutely know what you're doing**.

### How can I get started with Docker containers?

If you're developing on Linux, you should get acquainted with the basics of Docker containers. You can watch the official "Get Started" video [here](https://docs.docker.com/get-started/) and experiment in an interactive playground [here on Katacoda](https://www.katacoda.com/courses/docker).

### Once I RDP/SSH into the VM, how can I see a list of my running game servers running in Docker containers?

Use `docker ps`. You will see container name and hashes as well as port mapping from the VM port(s) to the Docker container port(s).

### How can I see the ports used by my game servers?

This information is listed on `docker ps`. For example, you might see something like that:

```
980d7e80457265230a0bf   "/bin/sh -c ./cppLin…"   About a minute ago   Up About a minute   0.0.0.0:30000->3600/tcp, 0.0.0.0:30001->3601/udp  great_archimedes
```

In this case, port 30000 on the VM is mapped onto 3600 on the container for TCP whereas 30001 on the VM is mapped on 3601 on the container for UDP. For more information for Docker networking, check [here](https://docs.docker.com/network/).

> In both Windows and Linux, Docker containers are part of the "playfab" Docker network. You can do `docker inspect network playfab` to see information about the network.

### How can I see runtime details for a container?

Once you do `docker ps` you can grab the container name or hash and do `docker inspect <nameOrHash>`. You will see lots of information, like the state of the container, the volume binding on the host VM, port bindings, environment variables passed into the container and more.

### How can I get the logs of my game server?

You can use the command `docker logs <nameOrHash>`. These logs contain everything your app sends to standard output/standard error streams. Bear in mind that these logs exist only for *existing* containers. If your game server crashes, our monitoring process in the VM will delete this container and create a new one.

Consequently, it's a better practice to use GSDK to log from inside your game server. Check [here](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/integrating-game-servers-with-gsdk#logging-with-the-gsdk) for logging with GSDK and [here](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/archiving-and-retrieving-multiplayer-server-logs) for accessing the logs from terminated game servers.

### Can I connect "inside" a running container to see what's going on?

Yup! You can do `docker exec -it <nameOrHash> powershell` on Windows and `docker exec -it <nameOrHash> bash` on Linux. There, you get access to a command line process in the container where you can issue native commands to debug/diagnose issues.

> For this command to work on Linux, Bash shell must be installed in your base container. If it isn't, you can use Bourne shell by running `docker exec -it <nameOrHash> sh`.

### How can I see the ports that are open on my container?

Once you are connected "inside" the container, you can use `netstat -ano` on Windows and `netstat -tulpn` on Linux when you have a command line process inside your container (check the previous instruction).

> If this command doesn't work on Linux, try installing netstat by `apt update && apt install net-tools`
> On Linux, you can use `nestatst -tulpn` inside the VM to see that the ports are indeed open. Recall that VM ports will be different than container ports, you can see this port mapping with `docker ps` for your containers.

### On Linux, how can I monitor TCP and UDP packets?

You can use tcpdump utility. `apt update && apt install tcpdump` to install it. In order to use it for a specific port, you can do `tcpdump port 7777` for TCP and `tcpdump udp port 7778` for UDP. For more details, you can check [here](https://www.hugeserver.com/kb/install-use-tcpdump-capture-packets/).

Worth mentioning is the fact that you can use tcpdump both from inside the VM and from inside the container. However, pay attention to the port you will be monitoring.

### On Windows, how can I monitor TCP and UDP packets?

You can try the [Wireshark](https://www.wireshark.org/) utility.

### On Windows, how can I debug a deployed multiplayer server using Visual Studio?

We have some instructions [here](https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/allocating-game-servers-and-configuring-vs-debugging-tools#debugging-a-deployed-multiplayer-server)

### Can I contribute to this guide?

Absolutely! Feel free to open a new [Pull Request](https://github.com/PlayFab/gsdksamples/pulls) with your instructions and we'd be more than happy to review and merge.