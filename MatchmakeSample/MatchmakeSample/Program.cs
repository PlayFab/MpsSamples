using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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

        private static async Task Run(string titleId, int numPlayers, string mmQueueName)
        {
            settings.TitleId = titleId;

            for (int i = 0; i < numPlayers; i++)
            {
                Player eachPlayer = new Player(Guid.NewGuid().ToString(), settings);
                await Login(eachPlayer);
                players.Add(eachPlayer);
            }

            VerifyNumPlayers(2, "matchmaking");
            foreach (Player eachPlayer in players)
            {
                await CreateMatchmakeTicket(eachPlayer, mmQueueName);
            }
            bool success = true;
            foreach (Player eachPlayer in players)
            {
                success &= await WaitForTicket(eachPlayer, mmQueueName);
            }
            VerifySuccess(success, "Matchmake-All Players");
            foreach (Player eachPlayer in players)
            {
                await GetFinalMatch(eachPlayer, mmQueueName);
            }
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

        private static void VerifySuccess(bool success, string eventName)
        {
            if (success)
            {
                Console.WriteLine($"{eventName} succeeded.");
            }
            else
            {
                Console.WriteLine($"{eventName} did not succeed.");
                throw new Exception($"{eventName} did not succeed.");
            }
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

        private static async Task CreateMatchmakeTicket(Player player, string mmQueueName)
        {
            var createRequest = new CreateMatchmakingTicketRequest
            {
                QueueName = mmQueueName,
                GiveUpAfterSeconds = 15,
                Creator = new MatchmakingPlayer { Entity = new PlayFab.MultiplayerModels.EntityKey { Id = player.context.EntityId, Type = player.context.EntityType } } // Why isn't this just the caller?
            };
            PlayFabResult<CreateMatchmakingTicketResult> ticketResult = await player.mpApi.CreateMatchmakingTicketAsync(createRequest);
            CreateMatchmakingTicketResult ticket = VerifyPlayFabCall(ticketResult, "Failed to create matchmake ticket");
            player.mmTicketId = ticket.TicketId;
        }

        private static async Task<bool> WaitForTicket(Player player, string mmQueueName)
        {
            var getTicketRequest = new GetMatchmakingTicketRequest
            {
                TicketId = player.mmTicketId,
                QueueName = mmQueueName // Why isn't this discoverable from the ticketId?
            };

            bool success = false;
            for (int i = 0; i < 10; i++)
            {
                PlayFabResult<GetMatchmakingTicketResult> ticketResult = await player.mpApi.GetMatchmakingTicketAsync(getTicketRequest);
                GetMatchmakingTicketResult ticket = VerifyPlayFabCall(ticketResult, "Matchmake ticket poll failed.");
                if (ticket.Status == "Matched")
                {
                    player.mmMatchId = ticket.MatchId;
                    player.ticket = ticket;
                    success = true;
                    break;
                }
                System.Threading.Thread.Sleep(6000);
                // "WaitingForMatch", "Matched"
            }
            VerifySuccess(success, $"Matchmake for {player.context.PlayFabId}");
            return success;
        }

        private static async Task<GetMatchResult> GetFinalMatch(Player player, string mmQueueName)
        {
            var getRequest = new GetMatchRequest
            {
                QueueName = mmQueueName,
                MatchId = player.mmMatchId
            };
            PlayFabResult<GetMatchResult> ticketResult = await player.mpApi.GetMatchAsync(getRequest);
            GetMatchResult match = VerifyPlayFabCall(ticketResult, "Failed to get final matchmake ticket");
            player.match = match;
            Console.WriteLine($"{player.context.PlayFabId} matched: {match.MatchId}");
            return match;
        }
    }

    public class Player
    {
        public readonly string customId;
        public readonly HttpClient httpClient;
        public readonly PlayFabAuthenticationContext context;
        public readonly PlayFabClientInstanceAPI clientApi;
        public readonly PlayFabMultiplayerInstanceAPI mpApi;

        public string mmTicketId;
        public string mmMatchId;
        public GetMatchmakingTicketResult ticket;
        public GetMatchResult match;

        public Player(string customId, PlayFabApiSettings settings)
        {
            this.customId = customId;
            httpClient = new HttpClient();
            context = new PlayFabAuthenticationContext();
            clientApi = new PlayFabClientInstanceAPI(settings, context);
            mpApi = new PlayFabMultiplayerInstanceAPI(settings, context);
        }
    }
}
