using Chia.ClientCore.Core;
using Chia.Common;
using Chia.DB;
using Chia.DB.Repositories;
using ChiaClientUI.Models;
using ElectronNET.API;
using ElectronNET.API.Entities;
using KeyCloakApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChiaClientUI
{
    public class Startup
    {

        static string chiaClientUIName = "ChiaClientUI";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        Worker wrk = null;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //IConfiguration configuration = hostContext.Configuration;

            //var workerOptions = Configuration.GetSection("AppSettings").Get<AppSettings>();

            //services.AddSingleton(workerOptions);

            //services.AddSession();
            try
            {
                services.AddSingleton<ChiaServerApi>();
                services.AddSingleton<LoginViewModel>();
                services.AddSingleton<SettingsRepository>();
                services.AddSingleton<LogsRepository>();
                //services.AddScoped<DBContext>();
                services.AddTransient<DBContext>();
                services.AddSingleton<Chia.ClientCore.Core.Worker>();
                services.AddControllersWithViews();
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"ConfigureServices: Exception: {ex.Message}", false, true);
                throw;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Task<string> ver=  Electron.App.GetVersionAsync();
            //CommonConstants.AppVersion = ver.Result;
            try
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                }
                app.UseStaticFiles();
                app.UseAuthentication();
                app.UseRouting();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
                });

                BootStrap();
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Configure: Exception: {ex.Message}", false, true);
                throw;
            }
        }

        private async void BootStrap()
        {
            try
            {
                var options = new BrowserWindowOptions
                {
                    Show = false,
                    Width = 1024, // 1200, // 1024,
                    Height = 800, // 810, //  800,
                    TitleBarStyle = TitleBarStyle.defaultStyle,
                    Maximizable = false,
                    Icon = "icon.ico"
                };
                var window = await Electron.WindowManager.CreateWindowAsync(options);

                //window.OnClose += () =>
                // {
                //     wrk = null;
                //     Console.WriteLine("Close");
                //     Console.ReadKey();
                // };
                //window.OnClosed += () =>
                //  {
                //      Console.WriteLine("OnClosed");
                //      wrk.Dispose();
                //      wrk = null;
                //      Chia.Net.clsStatus.AddLogMesssage($"Closing {DateTime.Now}");
                //      //Console.ReadKey();
                //  };
                //window.Maximize();
                //window.SetSize(1150, 900, true);
                window.SetAutoHideMenuBar(false);
                window.SetMenuBarVisibility(false);
                window.SetTitle("5Farm Client");

                window.OnReadyToShow += (async () =>
               {
                   try
                   {
                       CommonConstants.SaveDebugLog($"OnReadyToShow", false, true);
                       window.Show();
                       await RunClientService();
                   }
                   catch (Exception ex)
                   {
                       CommonConstants.SaveDebugLog($"OnReadyToShow: Exception: {ex.Message}", false, true);
                       throw;
                   }
               });

                //try
                //{
                //    if (CheckProcessIfRunning(processName: chiaClientUIName, isTask: false, taskname: ""))
                //    {
                //        Chia.Net.clsStatus.AddLogMesssage("Application Already Running..So closing");
                //        window.Close();
                //    }
                //    else
                //    {
                //        Chia.Net.clsStatus.AddLogMesssage("Starting Application");
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Bootstrap: Exception: {ex.Message}", false, true);
                throw;
            }
        }

        static bool CheckProcessIfRunning(string processName, bool isTask = false, string taskname = "")
        {
            Console.WriteLine($"Checking status for {processName}");

            string exeName = processName + ".exe";
            if (File.Exists(exeName))
            {
                if (Process.GetProcessesByName(processName).Length <= 1)
                {
                    Console.WriteLine("starting new Application");
                    return false;
                    //return StartProcess(exeName);
                }
                else
                {
                    Console.WriteLine($"Application is already running.");
                    return true;
                }
            }
            else
            {
                Console.WriteLine($"Exe file does not exist.");
                return false;
            }
        }

        private async Task RunClientService()
        {
            try
            {
                if (wrk == null)
                {
                    CommonConstants.SaveDebugLog($"Initializing Worker", false, true);
                    wrk = new Chia.ClientCore.Core.Worker();
                    CommonConstants.SaveDebugLog($"Running Client Processing", false, true);
                    await wrk.RunClientProcessing();
                }
                else
                {
                    CommonConstants.SaveDebugLog($"Worker Already Initialized", false, true);
                }
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"RunClientService: Exception: {ex.Message}", false, true);
                throw;
            }
        }
    }
}
