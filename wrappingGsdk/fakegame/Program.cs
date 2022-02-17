using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


namespace fakegame
{
    class Program
    {
        const int HTTP_PORT = 80;
        static void Main(string[] args)
        {
            Console.WriteLine($"Welcome to fake game server!");
            if(args.Length > 0)
            {
                foreach(string arg in args)
                {
                    Console.WriteLine($"Argument: {arg}");
                } 
            }

            // Port Number is passed from Wrapper.
            // When game server is running as process, Port Number will be mapped internally ( users don't need to specify a number). 
            // When game server is running in a container, Port Number is already set as it must be defined in build configuration. 
            HTTP_PORT = int.Parse(args[2]);

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
