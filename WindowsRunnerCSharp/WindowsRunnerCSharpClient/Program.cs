using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.QoS;

namespace WindowsRunnerCSharpClient
{
    /// <summary>
    ///   Simple executable that integrates with PlayFab's SDK.
    ///   It allocates a game server and makes an http request to that game server
    /// </summary>
    public class Program
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        
        public static Task Main(string[] args)
        {
            RootCommand rootCommand = RootCommandConfiguration.GenerateCommand(Run);

            return rootCommand.InvokeAsync(args);
        }

        private static async Task Run(string titleId, string playerId, string buildId, bool verbose)
        {
            PlayFabApiSettings settings = new PlayFabApiSettings() {TitleId = titleId};
            PlayFabClientInstanceAPI clientApi = new PlayFabClientInstanceAPI(settings);

            // Login
            var loginRequest = new LoginWithCustomIDRequest()
            {
                CustomId = playerId,
                CreateAccount = true
            };
            PlayFabResult<LoginResult> login = await clientApi.LoginWithCustomIDAsync(loginRequest);
            if (login.Error != null)
            {
                Console.WriteLine(login.Error.ErrorMessage);
                throw new Exception($"Login failed with HttpStatus={login.Error.HttpStatus}");
            }
            Console.WriteLine($"Logged in player {login.Result.PlayFabId} (CustomId={playerId})");
            Console.WriteLine();

            // Measure QoS
            Stopwatch sw = Stopwatch.StartNew();
            PlayFabQosApi qosApi = new PlayFabQosApi(settings, clientApi.authenticationContext);
            QosResult qosResult = await qosApi.GetQosResultAsync(250, degreeOfParallelism:4, pingsPerRegion:10);
            if (qosResult.ErrorCode != 0)
            {
                Console.WriteLine(qosResult.ErrorMessage);
                throw new Exception($"QoS ping failed with ErrorCode={qosResult.ErrorCode}");
            }
            
            Console.WriteLine($"Pinged QoS servers in {sw.ElapsedMilliseconds}ms with results:");

            if (verbose)
            {
                string resultsStr = JsonConvert.SerializeObject(qosResult.RegionResults, Formatting.Indented);
                Console.WriteLine(resultsStr);
            }

            int timeouts = qosResult.RegionResults.Sum(x => x.NumTimeouts);
            Console.WriteLine(string.Join(Environment.NewLine,
                qosResult.RegionResults.Select(x => $"{x.Region} - {x.LatencyMs}ms")));

            Console.WriteLine($"NumTimeouts={timeouts}");
            Console.WriteLine();
            
            // Allocate a server
            // You will get a unique server if you specify a unique SessionId in the call to RequestMultiplayerServers
            string sessionId = Guid.NewGuid().ToString();
            List<string> preferredRegions = qosResult.RegionResults
                .Where(x => x.ErrorCode == (int) QosErrorCode.Success)
                .Select(x => x.Region).ToList();
            
            PlayFabMultiplayerInstanceAPI mpApi = new PlayFabMultiplayerInstanceAPI(settings, clientApi.authenticationContext);
            PlayFabResult<RequestMultiplayerServerResponse> server =
                await mpApi.RequestMultiplayerServerAsync(new RequestMultiplayerServerRequest()
                    {
                        BuildId = buildId,
                        PreferredRegions = preferredRegions,
                        SessionId = sessionId
                    }
                );
            if (server.Error != null)
            {
                Console.WriteLine(server.Error.ErrorMessage);
                throw new Exception($"Allocation failed with HttpStatus={server.Error.HttpStatus}");
            }

            string serverLoc = $"{server.Result.IPV4Address}:{server.Result.Ports[0].Num}";
            Console.WriteLine($"Allocated server {serverLoc}");

            // Issue Http request against the server
            using (HttpResponseMessage getResult = await HttpClient.GetAsync("http://" + serverLoc))
            {
                getResult.EnsureSuccessStatusCode();
                
                Console.WriteLine("Received response:");
                string responseStr = await getResult.Content.ReadAsStringAsync();
                
                Console.WriteLine(responseStr);
                Console.WriteLine();
            }
        }
    }
}