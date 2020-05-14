using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Playfab.Gaming.GSDK.CSharp;

namespace openarena
{
    class Program
    {
        private static Process gameProcess;
        private static List<ConnectedPlayer> players = new List<ConnectedPlayer>();
        static void Main(string[] args)
        {
            Console.WriteLine("OpenArena for Azure PlayFab Multiplayer Servers");
            
            // GSDK event handlers - we're setting them on the startup of the app
            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            GameserverSDK.RegisterHealthCallback(IsHealthy);
            GameserverSDK.RegisterMaintenanceCallback(OnMaintenanceScheduled);

            // here we're starting the script that initiates the game process
            gameProcess = StartProcess("/opt/startup.sh");
            
            // event handlers to process the output from the game
            gameProcess.OutputDataReceived += DataReceived;
            gameProcess.ErrorDataReceived += DataReceived;
            // start reading output (stdout/stderr) from the game
            gameProcess.BeginOutputReadLine();
            gameProcess.BeginErrorReadLine();

            gameProcess.WaitForExit();
        }


        public static Process StartProcess(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            return process;
        }

        public static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            if(e.Data.Contains("Opening IP socket"))
            {
                // Call this while your game is initializing; it will start sending a heartbeat to our agent and put the game server in an Initializing state
                GameserverSDK.Start();
                // Call this when your game is done initializing and players can connect
                // Note: This is a blocking call, and will return when this game server is either allocated or terminated
                if(GameserverSDK.ReadyForPlayers())
                {
                    // After allocation, we can grab the session cookie from the config
                    IDictionary<string, string> activeConfig = GameserverSDK.getConfigSettings();

                    if (activeConfig.TryGetValue(GameserverSDK.SessionCookieKey, out string sessionCookie))
                    {
                        LogMessage($"The session cookie from the allocation call is: {sessionCookie}");
                    }
                }
                else
                {
                    // No allocation happened, the server is getting terminated (likely because there are too many already in standing by)
                    LogMessage("Server is getting terminated.");
                }
            }
            else if (e.Data.Contains("ClientBegin:")) // new player connected
            {
                players.Add(new ConnectedPlayer("gamer" + new Random().Next(0,21)));
                GameserverSDK.UpdateConnectedPlayers(players);
            }
            else if (e.Data.Contains("ClientDisconnect:")) // player disconnected
            {
                players.RemoveAt(new Random().Next(0, players.Count));
                GameserverSDK.UpdateConnectedPlayers(players);
            }
            else if (e.Data.Contains("AAS shutdown")) // game changes map
            {
                players.Clear();
                GameserverSDK.UpdateConnectedPlayers(players);
            }
        }

        static void OnShutdown()
        {
            LogMessage("Shutting down...");
            gameProcess?.Kill();
            Environment.Exit(0);
        }

        static bool IsHealthy()
        {
            // returns whether this game server process is healthy
            // here we're doing a simple check if our game wrapper is still alive
            return gameProcess != null;
        }

        static void OnMaintenanceScheduled(DateTimeOffset time)
        {
            LogMessage($"Maintenance Scheduled at: {time}");
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            // This will add your log line to the GSDK log file, alongside other information logged by the GSDK
            GameserverSDK.LogMessage(message);
        }
    }
}
