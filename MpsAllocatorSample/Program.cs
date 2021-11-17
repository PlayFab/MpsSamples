using PlayFab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MpsAllocator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the MpsAllocatorSample! This sample allows you to easily call frequently used APIs on your MPS Build");
            string titleId = Environment.GetEnvironmentVariable("PF_TITLEID");
            if (string.IsNullOrEmpty(titleId))
            {
                Console.WriteLine("Enter TitleID");
                titleId = Console.ReadLine();
            }

            PlayFabSettings.staticSettings.TitleId = titleId;

            string secret = Environment.GetEnvironmentVariable("PF_SECRET");
            if (string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("Enter developer secret key");
                secret = Console.ReadLine();
            }

            PlayFabSettings.staticSettings.DeveloperSecretKey = secret;

            var req = new PlayFab.AuthenticationModels.GetEntityTokenRequest();

            var res = await PlayFabAuthenticationAPI.GetEntityTokenAsync(req);

            bool exitRequested = false;
            while (!exitRequested)
            {
                int option = PrintOptions();

                switch (option)
                {
                    case 0:
                        exitRequested = true;
                        break;
                    case 1:
                        await RequestMultiplayerServer();
                        break;
                    case 2:
                        await ListBuildSummaries();
                        break;
                    case 3:
                        await GetBuild();
                        break;
                    case 4:
                        await ListMultiplayerServers();
                        break;
                    case 5:
                        await ListVirtualMachineSummaries();
                        break;
                    case 6:
                        await GetMultiplayerServerDetails();
                        break;
                    case 7:
                        await PrintServerStateInVM();
                        break;
                    default:
                        Console.WriteLine("Please enter a valid option");
                        continue;
                }
            }

        }

        static int PrintOptions()
        {
            string errorMsg = "Please enter a valid option (0-7)";
            while (true)
            {
                Console.WriteLine("----------------------------------");
                Console.WriteLine("0 to exit the app");
                Console.WriteLine("1 for RequestMultiplayerServer");
                Console.WriteLine("2 for ListBuildSummaries");
                Console.WriteLine("3 for GetBuild");
                Console.WriteLine("4 for ListMultiplayerServers");
                Console.WriteLine("5 for ListVirtualMachineSummaries");
                Console.WriteLine("6 for GetMultiplayerServerDetails");
                Console.WriteLine("7 to see server allocation status");
                Console.WriteLine("----------------------------------");
                var optionStr = Console.ReadLine();
                if (int.TryParse(optionStr, out var option))
                {
                    if (option < 0 || option > 7)
                    {
                        Console.WriteLine(errorMsg);
                        continue;
                    }

                    return option;
                }
                Console.WriteLine(errorMsg);
            }
        }

        static async Task ListBuildSummaries()
        {
            var req = new PlayFab.MultiplayerModels.ListBuildSummariesRequest();
            var res = await PlayFabMultiplayerAPI.ListBuildSummariesV2Async(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task ListMultiplayerServers()
        {
            var req = new PlayFab.MultiplayerModels.ListMultiplayerServersRequest();
            string buildID = ReadBuildIDFromInput();
            var regions = await GetRegions(buildID);
            req.Region = ReadRegionFromInput(regions);
            req.BuildId = buildID;
            var res = await PlayFabMultiplayerAPI.ListMultiplayerServersAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task ListVirtualMachineSummaries()
        {
            var req = new PlayFab.MultiplayerModels.ListVirtualMachineSummariesRequest();
            string buildID = ReadBuildIDFromInput();
            var regions = await GetRegions(buildID);
            req.Region = ReadRegionFromInput(regions);
            req.BuildId = buildID;
            var res = await PlayFabMultiplayerAPI.ListVirtualMachineSummariesAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task GetMultiplayerServerDetails()
        {
            var req = new PlayFab.MultiplayerModels.GetMultiplayerServerDetailsRequest();
            string buildID = ReadBuildIDFromInput();
            var regions = await GetRegions(buildID);
            string region = ReadRegionFromInput(regions);
            Console.WriteLine("Enter sessionId");
            string sessionID = Console.ReadLine();
            req.Region = region;
            req.BuildId = buildID;
            req.SessionId = sessionID;
            var res = await PlayFabMultiplayerAPI.GetMultiplayerServerDetailsAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task GetBuild()
        {
            var req = new PlayFab.MultiplayerModels.GetBuildRequest();
            Console.WriteLine("Enter BuildID");
            string buildID = Console.ReadLine();
            req.BuildId = buildID;
            var res = await PlayFabMultiplayerAPI.GetBuildAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task RequestMultiplayerServer()
        {
            var req2 = new PlayFab.MultiplayerModels.RequestMultiplayerServerRequest();
            req2.BuildId = ReadBuildIDFromInput();
            var regions = await GetRegions(req2.BuildId);
            string region = ReadRegionFromInput(regions);
            req2.PreferredRegions = new List<string>() { region };
            req2.SessionId = Guid.NewGuid().ToString();
            // Initial list of players (potentially matchmade) allowed to connect to the game.
            // This list is passed to the game server when requested (via GSDK) and can be used to validate players connecting to it.
            Console.WriteLine("Type initial players ID, Enter to confirm. Empty string when you're done");
            string id = Console.ReadLine();
            while (!string.IsNullOrEmpty(id))
            {
                req2.InitialPlayers ??= new List<string>();
                req2.InitialPlayers.Add(id.Trim());
                Console.WriteLine($"player with ID {id} was recorded, type the next one. Empty string when you're done");
                id = Console.ReadLine();
            }
            var res = await PlayFabMultiplayerAPI.RequestMultiplayerServerAsync(req2);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                PrettyPrintJson(res.Result);
            }
        }

        static async Task PrintServerStateInVM()
        {
            var req = new PlayFab.MultiplayerModels.ListMultiplayerServersRequest();
            string buildID = ReadBuildIDFromInput();
            var regions = await GetRegions(buildID);
            req.Region = ReadRegionFromInput(regions);
            req.BuildId = buildID;
            var res = await PlayFabMultiplayerAPI.ListMultiplayerServersAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
            }
            else
            {
                Dictionary<string, Dictionary<string, int>> vmIDsWithStates = new Dictionary<string, Dictionary<string, int>>();
                foreach (var server in res.Result.MultiplayerServerSummaries)
                {
                    if (!vmIDsWithStates.ContainsKey(server.VmId))
                    {
                        vmIDsWithStates.Add(server.VmId, new Dictionary<string, int>());
                    }

                    var statesForVm = vmIDsWithStates[server.VmId];
                    if (!statesForVm.ContainsKey(server.State))
                    {
                        statesForVm.Add(server.State, 0);
                    }

                    statesForVm[server.State]++;
                }

                foreach (var vm in vmIDsWithStates)
                {
                    Console.WriteLine($"- VM with ID:{vm.Key}");
                    foreach (var vmStates in vm.Value)
                    {
                        Console.WriteLine($"    - State:{vmStates.Key}, Count:{vmStates.Value}");
                    }
                }
            }
        }

        static void PrettyPrintJson(object obj)
        {
            string msg = JsonConvert.SerializeObject(obj, Formatting.Indented,
                new JsonConverter[] { new StringEnumConverter() });
            Console.WriteLine(msg);
        }

        static string ReadBuildIDFromInput()
        {
            while (true)
            {
                Console.WriteLine("Enter BuildID");
                string buildIDStr = Console.ReadLine();
                if (!Guid.TryParse(buildIDStr, out var buildID))
                {
                    Console.WriteLine("BuildID must be a GUID");
                    continue;
                }

                return buildIDStr;
            }
        }

        static string ReadRegionFromInput(IEnumerable<string> regions)
        {
            string region = regions.FirstOrDefault();
            if (regions.Count() > 1)
            {
                Console.WriteLine($"Enter region (options are {string.Join(",", regions)})");
                region = Console.ReadLine();
            }
            Console.WriteLine($"Using region: {region}");
            return region;
        }

        static async Task<IEnumerable<string>> GetRegions(string buildID)
        {
            var req = new PlayFab.MultiplayerModels.GetBuildRequest();
            req.BuildId = buildID;
            var res = await PlayFabMultiplayerAPI.GetBuildAsync(req);
            if (res.Error != null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
                return new string[] { };
            }
            else
            {
                return res.Result.RegionConfigurations.Select(x => x.Region);
            }
        }
    }
}
