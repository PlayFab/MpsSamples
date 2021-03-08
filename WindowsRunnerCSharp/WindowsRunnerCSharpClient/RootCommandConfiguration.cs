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
        public static RootCommand GenerateCommand(Func<string, string, bool, Task> onInvoke)
        {
            var rootCommand = new RootCommand()
            {
                new Option("--titleId",
                    "Your PlayFab titleId (Hex)")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option("--buildId",
                    "Host build id (in Game Manager)")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option("--verbose",
                    "When present, print verbose results")
                {
                    Argument = new Argument<bool>()
                },
            };

            rootCommand.Handler = CommandHandler.Create(onInvoke);
            
            return rootCommand;
        }
    }
}
