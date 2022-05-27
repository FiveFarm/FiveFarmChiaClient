using Chia.Common;
using Chia.DB;
using Chia.DB.Common;
using Chia.DB.Repositories;
using ChiaClientUI.Models;
using ElectronNET.API;
using KeyCloakApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChiaClientUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SettingsRepository _settings;
        private readonly LogsRepository _logsRepo;

        public HomeController(ILogger<HomeController> logger, SettingsRepository settings, LogsRepository logsRepo, DBContext dB, LoginViewModel loginModel)
        {
            //dB.Database.EnsureDeleted();
            //dB.Database.EnsureCreated();

            CommonConstants.SaveDebugLog($"Home: InitStart", false, true);
            _logger = logger;
            _settings = settings;
            _logsRepo = logsRepo;
            CommonConstants.SaveDebugLog($"Home: InitDone", false, true);
            //if (ViewData != null && ViewData.TryGetValue(user_name, out object userEmail) && userEmail != null && userEmail.ToString().Length > 0)
            //{
            //    loginModel.UserName = userEmail.ToString();
            //}
        }

        public IActionResult Index()
        {
            ViewData["AppVersion"] = CommonConstants.AppVersion;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel loginModel, [FromServices] ChiaServerApi serverApi)
        {
            if (ModelState.IsValid)
            {
                string url = CommonConstants.AuthenticationUrl;
                var result = string.Empty;
                bool IsValidPassphrase = true;
                try
                {
                    CommonConstants.Passphrase = "";

                    if (!string.IsNullOrEmpty(loginModel.Passphrase))
                    {
                        if (loginModel.Passphrase.Length < 8)
                        {
                            IsValidPassphrase = false;
                            ModelState.AddModelError(string.Empty, "Invalid Passphrase");
                        }
                        else
                        {
                        }
                    }
                    if (IsValidPassphrase)
                    {
                        result = await serverApi.Login(url, loginModel.UserName, loginModel.Password);
                    }
                }
                catch (Exception ex)
                {
                    CommonConstants.SaveDebugLog($"Exception during Login: {ex.Message}", false, true);
                }

                string userName = loginModel.UserName;

                if (!string.IsNullOrEmpty(result))
                {
                    if (userName.Contains("@"))
                    {
                        userName = userName.Substring(0, userName.IndexOf("@"));
                    }
                    CommonConstants.SaveDebugLog($"After Login: Setting TokenKeyName", false, true);
                    _settings.SetValue(SettingKeys.TokenKeyName, result);
                    CommonConstants.Passphrase = loginModel.Passphrase;

                    try
                    {
                        CommonConstants.SaveDebugLog($"Deleting sqI file", false, true);
                        CommonConstants.DeletePassphraseFiles();
                    }
                    catch (Exception ex)
                    {
                        CommonConstants.SaveDebugLog($"OnConfiguring: UseSqlite", false, true);
                    }

                    return RedirectToAction("dashboard", "home");
                }

                loginModel.UserName = userName;
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }

            return View(loginModel);
        }

        public IActionResult Dashboard()
        {
            CommonConstants.SaveDebugLog($"Dashboard", false, true);
            ViewData["AppVersion"] = CommonConstants.AppVersion;
            var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
            CommonConstants.SaveDebugLog($"WindowManger.BrowserWindow", false, true);
            Electron.IpcMain.On("async-msg", (args) =>
            {
                try
                {
                    CommonConstants.SaveDebugLog($"GettingLogs", false, true);
                    var logs = _logsRepo.GetLogs();
                    CommonConstants.SaveDebugLog($"GetLogs OK", false, true);
                    StringBuilder builder = new StringBuilder();
                    foreach (var logEntry in logs)
                    {
                        builder.Append($"{logEntry.LogTime} : {logEntry.LogString}{(logEntry.LogString.EndsWith(Environment.NewLine) ? "" : Environment.NewLine)}");
                        logEntry.IsProcessed = true;
                    }
                    _logsRepo.Update();

                    if (builder.Length > 0)
                        Electron.IpcMain.Send(mainWindow, "asynchronous-reply", builder.ToString());
                    //else
                    //    Electron.IpcMain.Send(mainWindow, "asynchronous-reply", "----");
                }
                catch (Exception ex)
                {
                    CommonConstants.SaveDebugLog($"asynchronous-reply Error: {ex.Message}", false, true);
                    Electron.IpcMain.Send(mainWindow, "asynchronous-reply", $"Error:{ex.Message}");
                }

            });
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
