using System;
using System.Threading.Tasks;

namespace fakegame
{
    class Program
    {
        async static Task Main(string[] args)
        {
            Console.WriteLine($"Welcome to fake game server!");
            while(true)
            {
                await Task.Delay(10000);
                Console.WriteLine($"Fake game server is alive!");
            }
        }
    }
}
