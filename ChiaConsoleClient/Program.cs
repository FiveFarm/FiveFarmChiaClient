using Chia.ClientCore.Core;
using Chia.ClientCore.Models;
using Chia.Common;
using Chia.Net.CLI_Interface;
//using ChiaClientService.Core;
using KeyCloakApi;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Chia.Net;
using Chia.DB.Repositories;
using Chia.DB.Common;
using Chia.ClientCore.Common;
using Chia.DB;

namespace ChiaConsoleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            try
            {
                //CommonConstants.IsDebug = false;
                CommonConstants.DeletePassphraseFiles();

                await ProcessChiaPoolData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadKey();
        }

        static DateTime DateChange = DateTime.MinValue;
        static async Task ProcessChiaPoolData()
        {
            string token = "", passphrase = "";
            SettingsRepository settings = new SettingsRepository();
            ChiaServerApi api = new ChiaServerApi();

            do
            {
                try
                {
                    token = "";

                    #region Input Username and Password
                    Console.Write("Please Enter Username:");
                    string user = Console.ReadLine();

                    Console.Write("Enter your Password: ");
                    string pass = CommonMethods.GetPasswordInput();

                    bool IsValidPassphrase = true;
                    do
                    {
                        IsValidPassphrase = true;
                        CommonConstants.Passphrase = "";
                        Console.Write("Enter Passphrase (Just Enter If no Passphrase Set): ");
                        passphrase = CommonMethods.GetPasswordInput();

                        if (!string.IsNullOrEmpty(passphrase))
                        {
                            if (passphrase.Length < 8)
                            {
                                IsValidPassphrase = false;
                                Console.WriteLine("Invalid Passphrase.");
                            }
                        }
                    }
                    while (!IsValidPassphrase);

                    #endregion

                    #region Authenticate

                    if (IsValidPassphrase)
                    {
                        token = api.Login(CommonConstants.AuthenticationUrl, user, pass).Result;
                        CommonConstants.Passphrase = passphrase;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to Login");
                    Console.WriteLine($"Reason: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    Console.WriteLine("=======================");
                }
            }
            while (string.IsNullOrEmpty(token));

            Worker worker = new Worker();
            settings.SetValue(SettingKeys.TokenKeyName, token);

            #endregion

            DateTime prevTime;
            while (true)
            {
                prevTime = DateTime.Now;

                try
                {
                    //Save Version on App Startup and each date change
                    if (DateChange.Date < prevTime.Date)
                    {
                        CommandLineExec.GetChiaVersion();
                        CommandLineExec.GetAndSaveChiaFolderFileList();
                        DateChange = prevTime;
                    }
                    await worker.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    clsStatus.AddLogMesssage($"Error:{ex.Message}");
                }
                while (DateTime.Now < prevTime.AddMinutes(1))
                {
                    await Task.Delay(50);
                }

                clsStatus.AddLogMesssage($"Next Call:{DateTime.Now.ToString()}{Environment.NewLine}===================================="); // .AddSeconds(nexCallAfter / 1000)}");
            }

        }
        static void ProcessCustomApiCall()
        {
            ChiaClientManager _chiaClient = new ChiaClientManager(new Chia.NET.Clients.FarmerClient(), new Chia.NET.Clients.HarvesterClient(), new Chia.NET.Clients.FullNodeClient(), new Chia.NET.Clients.WalletClient(), new NodeInfo());
            while (true)
            {
                try
                {
                    Console.Write($"Please Enter API Type:{Environment.NewLine}" +
                        $"0. Harvester{ Environment.NewLine}" +
                        $"1. FullNode{ Environment.NewLine}" +
                        $"2. Wallet{ Environment.NewLine}" +
                        $"3. Farmer{ Environment.NewLine}" +
                        $"enter your choice{ Environment.NewLine}"
                    );
                    APIType choice = (APIType)int.Parse(Console.ReadLine());

                    Console.Write("Please Enter url:");
                    string url = Console.ReadLine();

                    string parameters = "";
                    Console.Write("Enter parameters:(key1:value1,key2:value2) -> ");
                    parameters = Console.ReadLine();

                    string response = _chiaClient.GetCustomResponse(choice, url, parameters).Result;
                    Console.WriteLine($"Response:{Environment.NewLine}{response}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error:{ex.Message}");
                }
                finally
                {
                    Console.WriteLine("=========================================");
                }
            }

        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"Console client Unhandled Exception: {e.ExceptionObject.ToString()}");
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }


}
