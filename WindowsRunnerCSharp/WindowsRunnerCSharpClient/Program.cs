using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using PlayFab.QoS;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WindowsRunnerCSharpClient
{
    /// <summary>
    ///   Simple executable that integrates with PlayFab's SDK.
    ///   It allocates a game server and makes an http request to that game server
    /// </summary>
    public class Program
    {
        private static readonly PlayFabApiSettings settings = new PlayFabApiSettings();
        private static readonly List<Player> players = new List<Player>();

        public static Task Main(string[] args)
        {
            RootCommand rootCommand = RootCommandConfiguration.GenerateCommand(Run);

            return rootCommand.InvokeAsync(args);
        }

        private static async Task Run(string titleId, string buildId, bool verbose)
        {
            settings.TitleId = titleId;

            // Only 1 player in this sample
            Player hostPlayer = new Player(Guid.NewGuid().ToString(), settings);
            await Login(hostPlayer);
            players.Add(hostPlayer);

            VerifyNumPlayers(1, "hosting");
            QosResult qosResult = await MeasureQos(hostPlayer, verbose);
            string serverLoc = await AllocateServer(hostPlayer, qosResult, buildId);
            await ConnectToServer(hostPlayer, serverLoc);
        }

        private static void VerifyNumPlayers(int numRequired, string feature)
        {
            if (players.Count < numRequired)
            {
                Console.WriteLine($"Need at least {numRequired} player(s) to test {feature}, playerCount: {players.Count}");
                throw new Exception($"Need at least {numRequired} player(s) to test {feature}, playerCount: {players.Count}");
            }
        }

        private static TResult VerifyPlayFabCall<TResult>(PlayFabResult<TResult> playFabResult, string throwMsg) where TResult : PlayFab.Internal.PlayFabResultCommon
        {
            if (playFabResult.Error != null)
            {
                Console.WriteLine(playFabResult.Error.GenerateErrorReport());
                throw new Exception($"{throwMsg} HttpStatus={playFabResult.Error.HttpStatus}");
            }
            return playFabResult.Result;
        }

        private static async Task Login(Player player)
        {
            var loginRequest = new LoginWithCustomIDRequest()
            {
                CustomId = player.customId,
                CreateAccount = true
            };
            PlayFabResult<LoginResult> login = await player.clientApi.LoginWithCustomIDAsync(loginRequest);
            LoginResult loginResult = VerifyPlayFabCall(login, "Login failed");
            Console.WriteLine($"Logged in player {login.Result.PlayFabId}, CustomId={loginRequest.CustomId}");
        }

        private static async Task<QosResult> MeasureQos(Player player, bool verbose)
        {
            Stopwatch sw = Stopwatch.StartNew();
            QosResult qosResult = await player.qosApi.GetQosResultAsync(250, degreeOfParallelism: 4, pingsPerRegion: 10);
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

            return qosResult;
        }

        private static async Task<string> AllocateServer(Player player, QosResult qosResult, string buildId)
        {
            // You will get a unique server if you specify a unique SessionId in the call to RequestMultiplayerServers
            string sessionId = Guid.NewGuid().ToString();
            List<string> preferredRegions = qosResult.RegionResults
                .Where(x => x.ErrorCode == (int)QosErrorCode.Success)
                .Select(x => x.Region).ToList();

            var serverRequest = new RequestMultiplayerServerRequest()
            {
                BuildId = buildId,
                PreferredRegions = preferredRegions,
                SessionId = sessionId
            };
            PlayFabResult<RequestMultiplayerServerResponse> serverResult = await player.mpApi.RequestMultiplayerServerAsync(serverRequest);
            RequestMultiplayerServerResponse server = VerifyPlayFabCall(serverResult, "Allocation failed");

            string serverLoc = $"{server.IPV4Address}:{server.Ports[0].Num}";
            Console.WriteLine($"Allocated server {serverLoc}");
            return serverLoc;
        }

        private static async Task ConnectToServer(Player player, string serverLoc)
        {
            // Issue Http request against the server
            using (HttpResponseMessage getResult = await player.httpClient.GetAsync("http://" + serverLoc))
            {
                getResult.EnsureSuccessStatusCode();

                Console.WriteLine("Received response:");
                string responseStr = await getResult.Content.ReadAsStringAsync();

                Console.WriteLine(responseStr);
            }
        }
    }

    public class Player
    {
        public readonly string customId;
        public readonly HttpClient httpClient;
        public readonly PlayFabAuthenticationContext context;
        public readonly PlayFabClientInstanceAPI clientApi;
        public readonly PlayFabMultiplayerInstanceAPI mpApi;
        public readonly PlayFabQosApi qosApi;

        public Player(string customId, PlayFabApiSettings settings)
        {
            this.customId = customId;
            httpClient = new HttpClient();
            context = new PlayFabAuthenticationContext();
            clientApi = new PlayFabClientInstanceAPI(settings, context);
            mpApi = new PlayFabMultiplayerInstanceAPI(settings, context);
            qosApi = new PlayFabQosApi(settings, context);
        }
    }
}
