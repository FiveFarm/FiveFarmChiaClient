//#define DebugLogOn

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace Chia.Common
{
    public class CommonConstants
    {
        #region Class Members

        public static string Passphrase;
        public static string EmailId { get; set; }
        public static JObject Error_Codes { get => error_Codes; set => error_Codes = value; }

        private static JObject error_Codes = new JObject();

        public static void AddError_Code(string param, string val)
        {
            //JObject obj = new JObject();
            //obj.Add(param, val);
            if (!error_Codes.ContainsKey(param))
                error_Codes.Add(param, val);
            else
            { }
        }

        public static string EndPoint
        {
            get
            {
                return ManipulateEndpointSettingFile().Item1;
            }
        }
        public static string Tag
        {
            get
            {
                return ManipulateEndpointSettingFile().Item2;
            }
        }
        public static bool IsSaveDebugLog
        {
            get
            {
                bool? val = ManipulateEndpointSettingFile().Item3;
#if DebugLogOn
                return val == null ? true : (bool)val;
#else
                return val == null ? false : (bool)val;
#endif
            }
        }

        public static bool IsSavePlottingLog = true;
        public static DateTime LastLogTime_BlockFound = DateTime.MinValue;
        public static DateTime LastLogTime_PoolStopped = DateTime.MinValue;

        static string BaseUrl => $"{EndPoint}/auth/realms/{Tag}/protocol/openid-connect/";
        public const string SocketAddress = "wss://c32f076pz6.execute-api.us-east-1.amazonaws.com/";
        public static string AuthenticationUrl => $"{BaseUrl}token";
        public static string InfoUrl => $"{BaseUrl}userinfo";
        public const int PoolInterval = 60000;

        public static string CompanyName => "FiveRivers Technologies";
        public static string ProductName => "5FarmChiaClient";

        public static string PassphraseFilePath
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT ?
                 (@$"{DataFolder}\{System.Guid.NewGuid().ToString()}.sqI") :
                 (@$"{DataFolder}/{System.Guid.NewGuid().ToString()}.sqI");
            }
        }
        public static string DataFolder
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductName);
                }
                else
                {
                    string dataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string path = Path.Combine(dataFolder, ProductName);
                    return path;
                }
            }
        }

        public static void DeletePassphraseFiles()
        {
            try
            {
                var dir = new DirectoryInfo(DataFolder);
                dir.EnumerateFiles("*.sqI").ToList().ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Error during deleteing file: {ex.Message}", false, true);
            }
        }
        public static void SaveDebugLog(string msg, bool display = false, bool saveToFile = false)
        {
            if (CommonConstants.IsSaveDebugLog)
            {
                if (display)
                    Console.WriteLine(msg);

                try
                {
                    if (saveToFile)
                    {
                        string path = System.IO.Path.Combine(CommonConstants.DataFolder, $"5FarmDebug_{DateTime.Now.ToString("yyyyMMdd")}.log");
                        FileHandler.SaveFile_Log(path, $"{DateTime.Now.ToString("yyyyMMdd HHmmss")}: {msg}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception During DebugSave: {ex.Message}");
                }
            }
        }


        public static string AppVersion { get => "v1.0.38"; }
        #endregion

        #region Private Methods

        private static (string, string, bool?) ManipulateEndpointSettingFile()
        {
            try
            {
#if DebugLogOn
                (string, string, bool?) defaults = ("https://login.fivefarm.app", "Crypto", true);
#else
                (string, string, bool?) defaults = ("https://login.fivefarm.app", "Crypto", false);
#endif

                string settingsFileName = Path.Combine(DataFolder, "endpointsettings.json");
                JObject settings = null;

                if (File.Exists(Path.Combine(settingsFileName)))
                {
                    string jsonSettings = File.ReadAllText(settingsFileName);
                    settings = (JObject)JsonConvert.DeserializeObject(jsonSettings);

                    if (settings != null && !settings.ContainsKey("SaveDebugLog"))
                    {
                        settings.Add("SaveDebugLog", defaults.Item3);
                        var setting = (
                                    settings.GetValue("ApiEndPoint").ToString(),
                                    settings.GetValue("Tag").ToString(),
                                    defaults.Item3
                                    );

                        FileHandler.SaveFile(settingsFileName, settings.ToString());
                        return setting;
                    }
                    else
                    {
                        var setting = (
                                        settings.GetValue("ApiEndPoint").ToString(),
                                        settings.GetValue("Tag").ToString(),
                                        ((bool?)settings.GetValue("SaveDebugLog"))
                                        );
                        return setting;
                    }
                }
                else
                {
                    settings = new JObject();
                    settings.Add("ApiEndPoint", defaults.Item1);
                    settings.Add("Tag", defaults.Item2);
                    settings.Add("SaveDebugLog", defaults.Item3);
                    FileHandler.SaveFile(settingsFileName, settings.ToString());
                }
                return defaults;
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Exception in ManipulateEndpointSettingFile: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}", false, true);
                throw;
            }
        }

        private static (string, string) AddUpdateEndpointSettingFile((string, string) endPointSetting, string settingsFileName)
        {
            try
            {
                JObject settings = new JObject();
                settings.Add("ApiEndPoint", endPointSetting.Item1);
                settings.Add("Tag", endPointSetting.Item2);
                FileHandler.SaveFile(settingsFileName, settings.ToString());
                return endPointSetting;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

    }
    public enum ErrCodeMnCtg : int
    {
        GetBlockChainState = 1,
        GetConnections_fullnode = 2,
        GetChallenges_farmer = 3,
        farmed_unfinished_block = 4,
        poolErrorWarnings = 5,
        farmerlatestBlockChallanges = 6,
        GetNetworkInfo_fullnode = 7,
        GetFarmingStatus = 8,
        GetSyncStatus = 9,
        GetHeightInfo_wallet = 10,
        GetPoolState_farmer = 11,
        GetPointsFoundSinceStart = 12,
        GetPoolsFromPlotNFT2 = 13,
        UpdateConnectedFarmerList = 14,
        UpdateConnectedHravesterList = 15,

    }

    public enum ErrCodesSbCtg : int
    {
        NONE = -1,
        OK = 0,
        Blank_Response = 1,
        MissingRequiredData,
        Mismatched,
        Invalid,
        Syncing,

    }
}
