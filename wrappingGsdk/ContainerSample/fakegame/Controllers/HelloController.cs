using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace fakegame.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HelloController : ControllerBase
    {
        IHostApplicationLifetime applicationLifetime;
        public HelloController(IHostApplicationLifetime appLifetime)
        {
            applicationLifetime = appLifetime;
        }

        [HttpGet]
        public string Get()
        {
            Console.WriteLine($"GET /hello at {DateTime.UtcNow}");
            return $"Hello from {Dns.GetHostName()}";
        }

        [HttpGet("terminate")]
        public void Terminate()
        {
            Console.WriteLine($"GET /hello/terminate at {DateTime.UtcNow}");
            applicationLifetime.StopApplication();
        }
    }
}