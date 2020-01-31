using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinMe.Blazor.Services;
using ElectronNET.API;
using Blazored.Toast;

using DocumentFormat.OpenXml.Drawing.Diagrams;

using ElectronNET.API.Entities;

using global::Blazor.Fluxor;

namespace MinMe.Blazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddBlazoredToast();

            // Services
            services.AddSingleton<DocumentService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<PartsGridService>();

            services.AddFluxor(options => {
                options.UseDependencyInjection(typeof(Startup).Assembly);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            Task.Run(async () => await  Bootstrap());
        }

        private async Task Bootstrap()
        {
            var options = new BrowserWindowOptions
            {
                WebPreferences = new WebPreferences
                {
                    NodeIntegration = false
                },
                Show = false
            };
            var mainWindow = await Electron.WindowManager.CreateWindowAsync(options);
            mainWindow.OnReadyToShow += () => mainWindow.Show();
            mainWindow.OnClosed += () => Electron.App.Exit();

            var menu = new[]
            {
                new MenuItem
                {
                    Label = "File",
                    Submenu = new[]
                    {
                        new MenuItem
                        {
                            Label = "Exit",
                            Click = () => Electron.App.Exit()
                        }
                    }
                },
            };
            //Electron.Menu.SetApplicationMenu(menu);
        }
    }
}
