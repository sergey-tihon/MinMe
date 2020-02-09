using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;

namespace MinMe.Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // WebWindiw with Blazor Desktop
            //WebWindows.Blazor.ComponentsDesktop
            //    .Run<Startup>("MinMe.Blazor", "wwwroot/index.html");

            // WebWindow with Blazor Server
            var host = CreateHostBuilder(args).Build();
            host.Start();
            var url = host.Services.GetRequiredService<IServer>().Features
                       .Get<IServerAddressesFeature>()
                       .Addresses.Single();

            var window = new WebWindows.WebWindow("MinMe.Blazor");
            window.NavigateToUrl(url);
            window.WaitForExit();

            host.StopAsync().GetAwaiter().GetResult();

            // Classic Blazor with Electron.NET
            //CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
