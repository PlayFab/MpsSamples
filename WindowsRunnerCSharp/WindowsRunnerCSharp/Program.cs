using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Playfab.Gaming.GSDK.CSharp;
using Newtonsoft.Json;

namespace WindowsRunnerCSharp
{
    ///-------------------------------------------------------------------------------
    ///   Simple executable that integrates with PlayFab's Gameserver SDK (GSDK).
    ///   It starts an http server that will respond to GET requests with a json file
    ///   containing whatever configuration values it read from the GSDK.
    ///-------------------------------------------------------------------------------
    class Program
    {
        private static HttpListener _listener = new HttpListener();
        const string ListeningPortKey = "game_port";
        
        const string AssetFilePath = @"C:\Assets\testassetfile.txt";
        private const string GameCertAlias = "winRunnerTestCert";

        private static List<ConnectedPlayer> players = new List<ConnectedPlayer>();
        private static int requestCount = 0;

        private static bool _isActivated = false;
        private static string _assetFileText = String.Empty;
        private static string _installedCertThumbprint = String.Empty;
        private static DateTimeOffset _nextMaintenance = DateTimeOffset.MinValue;

        static void OnShutdown()
        {
            LogMessage("Shutting down...");
            _listener.Stop();
            _listener.Close();
        }

        static bool IsHealthy()
        {
            // Should return whether this game server is healthy
            return true;
        }

        static void OnMaintenanceScheduled(DateTimeOffset time)
        {
            LogMessage($"Maintenance Scheduled at: {time}");
            _nextMaintenance = time;
        }

        static void Main(string[] args)
        {
            // GSDK Setup
            try
            {
                GameserverSDK.Start();
            }
            catch (Microsoft.Playfab.Gaming.GSDK.CSharp.GSDKInitializationException initEx)
            {
                LogMessage("Cannot start GSDK. Please make sure the MockAgent is running. ", false);
                LogMessage($"Got Exception: {initEx.ToString()}", false);
                return;
            }
            catch (Exception ex)
            {
                LogMessage($"Got Exception: {ex.ToString()}", false);
            }

            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            GameserverSDK.RegisterHealthCallback(IsHealthy);
            GameserverSDK.RegisterMaintenanceCallback(OnMaintenanceScheduled);

            // Read our asset file
            if (File.Exists(AssetFilePath))
            {
                _assetFileText = File.ReadAllText(AssetFilePath);
            }

            IDictionary<string, string> initialConfig = GameserverSDK.getConfigSettings();

            // Start the http server
            if (initialConfig?.ContainsKey(ListeningPortKey) == true)
            {
                int listeningPort = int.Parse(initialConfig[ListeningPortKey]);
                string address = $"http://*:{listeningPort}/";
                _listener.Prefixes.Add(address);
                _listener.Start();
            }
            else
            {
                LogMessage($"Cannot find {ListeningPortKey} in GSDK Config Settings. Please make sure the MockAgent is running " +
                           $"and that the MultiplayerSettings.json file includes {ListeningPortKey} as a GamePort Name.");
                return;
            }

            // Load our game certificate if it was installed
            if (initialConfig?.ContainsKey(GameCertAlias) == true)
            {
                string expectedThumbprint = initialConfig[GameCertAlias];
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificateCollection = store.Certificates.Find(X509FindType.FindByThumbprint, expectedThumbprint, false);

                if (certificateCollection.Count > 0)
                {
                    _installedCertThumbprint = certificateCollection[0].Thumbprint;
                }
                else
                {
                    LogMessage("Could not find installed game cert in LocalMachine\\My. Expected thumbprint is: " + expectedThumbprint);
                }
            }
            else
            {
                LogMessage("Config did not contain cert! Config is: " + string.Join(";", initialConfig.Select(x => x.Key + "=" + x.Value)));
            }

            Thread t = new Thread(ProcessRequests);
            t.Start();

            if (GameserverSDK.ReadyForPlayers())
            {
                _isActivated = true;

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

        /// <summary>
        /// Listens for any requests and responds with the game server's config values
        /// </summary>
        private static void ProcessRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string requestMessage = $"HTTP:Received {request.Headers.ToString()}";
                    LogMessage(requestMessage);

                    IDictionary<string, string> config = null;

                    // For each request, "add" a connected player, but limit player count to 20.
                    const int maxPlayers = 20;
                    if (players.Count < maxPlayers )
                    {
                        players.Add(new ConnectedPlayer("gamer" + requestCount));
                    }
                    else
                    {
                        LogMessage($"Player not added since max of {maxPlayers} is reached. Current request count: {requestCount}.");
                    }

                    requestCount++;
                    GameserverSDK.UpdateConnectedPlayers(players);

                    config = GameserverSDK.getConfigSettings() ?? new Dictionary<string, string>();
                    
                    config.Add("isActivated", _isActivated.ToString());
                    config.Add("assetFileText", _assetFileText);
                    config.Add("logsDirectory", GameserverSDK.GetLogsDirectory());
                    config.Add("installedCertThumbprint", _installedCertThumbprint);


                    if (_isActivated)
                    {
                        IList<string> players = GameserverSDK.GetInitialPlayers();
                        config.Add("players", players == null ? "NULL" : string.Join(", ", players));

                    }

                    config.Add("connectionInfo", JsonConvert.SerializeObject(GameserverSDK.GetGameServerConnectionInfo()));

                    if (_nextMaintenance != DateTimeOffset.MinValue)
                    {
                        config.Add("nextMaintenance", _nextMaintenance.ToLocalTime().ToString());
                    }

                    string content = JsonConvert.SerializeObject(config, Formatting.Indented);

                    response.AddHeader("Content-Type", "application/json");
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                    response.ContentLength64 = buffer.Length;
                    using (System.IO.Stream output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
                catch (HttpListenerException httpEx)
                {
                    // This one is expected if we stopped the listener because we were asked to shutdown
                    LogMessage($"Got HttpListenerException: {httpEx.ToString()}, we are being shut down.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Got Exception: {ex.ToString()}");
                }
            }
        }

        private static void LogMessage(string message, bool enableGSDKLogging = true)
        {
            Console.WriteLine(message);
            if (enableGSDKLogging)
            {
                GameserverSDK.LogMessage(message);
            }
        }
    }
}
