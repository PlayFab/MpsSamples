using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace WindowsRunnerCSharpClient
{
    /// <summary>
    /// Used to create and run the root command for VmAgent.  
    /// </summary>
    public static class RootCommandConfiguration
    {
        public static RootCommand GenerateCommand(Func<string, int, string, Task> onInvoke)
        {
            var rootCommand = new RootCommand()
            {
                new Option("--titleId",
                    "Your PlayFab titleId (Hex)")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option("--numPlayers",
                    "Number of players to create")
                {
                    Argument = new Argument<int>(),
                    Required = true
                },
                new Option("--mmQueueName",
                    "Name of Matchmaking Queue to join")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
            };

            rootCommand.Handler = CommandHandler.Create(onInvoke);
            
            return rootCommand;
        }
    }
}
