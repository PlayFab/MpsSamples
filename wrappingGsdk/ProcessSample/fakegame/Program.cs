using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


namespace fakegame
{
    class Program
    {
        static int HTTP_PORT = 56100;
        static void Main(string[] args)
        {
            Console.WriteLine($"Welcome to fake game server!");
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    Console.WriteLine($"Argument: {arg}");
                }
            }

            HTTP_PORT = int.Parse(args[2]);

            Console.WriteLine($"Welcome to fake game server!");
            Console.WriteLine($"Starting fake game server listening on {HTTP_PORT}");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://*:{HTTP_PORT}");
            });
    }
}
