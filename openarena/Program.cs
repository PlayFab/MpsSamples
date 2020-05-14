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
            
            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            GameserverSDK.RegisterHealthCallback(IsHealthy);
            GameserverSDK.RegisterMaintenanceCallback(OnMaintenanceScheduled);

            gameProcess = StartProcess("/opt/startup.sh");
            
            gameProcess.OutputDataReceived += DataReceived;
            gameProcess.ErrorDataReceived += DataReceived;
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
                GameserverSDK.Start();
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
            else if (e.Data.Contains("ClientBegin:"))
            {
                players.Add(new ConnectedPlayer("gamer"+ new Random().Next(0,21)));
                GameserverSDK.UpdateConnectedPlayers(players);
            }
            else if (e.Data.Contains("ClientDisconnect:"))
            {
                players.RemoveAt(new Random().Next(0, players.Count));
                GameserverSDK.UpdateConnectedPlayers(players);
            }
            else if (e.Data.Contains("AAS shutdown"))
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
            return gameProcess != null;
        }

        static void OnMaintenanceScheduled(DateTimeOffset time)
        {
            LogMessage($"Maintenance Scheduled at: {time}");
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            GameserverSDK.LogMessage(message);
        }
    }
}
