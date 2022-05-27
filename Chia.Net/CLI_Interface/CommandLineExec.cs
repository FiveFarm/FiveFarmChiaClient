using Chia.Common;
using Chia.DB.Repositories;
using Chia.NET.Clients;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Environment;

namespace Chia.Net.CLI_Interface
{
    public class CommandLineExec
    {
        static List<string> CommandsWithPassphrase = new List<string>()
        {
            "keys show","init",
            "plotnft show","farm challenges", "farm summary", "show -s"
        };

        static string CHIA = "";

        static Dictionary<string, string> PoolNameList = new Dictionary<string, string>();
        static string fingerprint = "";

        static Process cmd = new Process();
        static CommandLineExec()
        {
            CHIA = "chia";
        }

        static string eOut = "";
        private static void StartProcess()
        {
            eOut = "";
            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                cmd.StartInfo.FileName = "/bin/bash";///bin/bash
            }
            else
            {
                cmd.StartInfo.FileName = "cmd.exe";///bin/bash
            }
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardError = true;
            cmd.StartInfo.ErrorDialog = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.EnableRaisingEvents = true;

            cmd.OutputDataReceived += Cmd_OutputDataReceived;

            cmd.Start();
        }

        private static void Cmd_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine($"DataReceived: {e.Data}");
            }
        }

        private static string GetChiaPath()
        {
            string startPath = $@"{GetFolderPath(SpecialFolder.UserProfile)}\AppData\Local\chia-blockchain\";
            string endPath = @"resources\app.asar.unpacked\daemon\";
            string path = "";

            if (!Directory.Exists(startPath)) return string.Empty;

            string[] installedApps = Directory.GetDirectories(startPath).Where(x => x.StartsWith(Path.Combine(startPath, "app-"))).ToArray();
            string appVersionFolder = "";

            foreach (var dir in installedApps)
            {
                if (File.Exists(Path.Combine(dir, "Chia.exe")))
                {
                    appVersionFolder = dir;
                    break;
                }
            }

            path = Path.Combine(appVersionFolder, endPath);

            if (!Directory.Exists(path)) return string.Empty;

            return path;

        }

        private static string GetChiaPathLinux()
        {
            string startPath = $@"/usr/lib/chia-blockchain/";
            string endPath = @"resources/app.asar.unpacked/daemon/";
            string path = "";

            if (!Directory.Exists(startPath)) return string.Empty;

            path = Path.Combine(startPath, endPath);

            if (!Directory.Exists(path)) return string.Empty;

            return path;
        }

        static int WaitTimeoutCount = 0;
        static bool IsPassphraseFile = false;
        static bool TryWithoutPassphrase = false;

        static bool StartedAll = false;
        private static string ReadCommand(string command, bool IsNonChiaCmd = false)
        {
            try
            {
                StartProcess();//new_farming_info
                string keyringpath = "";
                int timeout = 10;

                #region Linux
                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    timeout = 20;
                    string cmmd = "";
                    string path = GetChiaPathLinux();

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        CHIA = "./chia";
                        cmd.StandardInput.WriteLine($"cd {path}");
                        Common.CommonConstants.SaveDebugLog($"chia path: {path}", false, true);

                        if (IsNonChiaCmd)
                        {
                            return Get_Save_FileList_Linux(command);
                        }
                        keyringpath = MakePassphraseStringLinux(command);
                    }
                    else
                    {
                        ///home/frt/.chia/mainnet
                        string bashPath = Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "chia-blockchain");
                        Common.CommonConstants.SaveDebugLog($"Bash path:{bashPath}", false, true);
                        cmd.StandardInput.WriteLine($"cd {bashPath}");

                        if (IsNonChiaCmd)
                        {
                            return Get_Save_FileList_Linux(command);
                        }
                        cmd.StandardInput.WriteLine(". ./activate");
                        keyringpath = MakePassphraseStringLinux(command);
                        cmmd = $"{CHIA} {keyringpath}init";
                        cmd.StandardInput.WriteLine(cmmd);
                        string dky = string.IsNullOrWhiteSpace(keyringpath) ? "NoPF" : "PF";
                        Common.CommonConstants.SaveDebugLog($"Command: [{dky}] init", false, true);

                        //if(!StartedAll)
                        {
                            cmd.StandardInput.WriteLine($"{CHIA} start farmer");
                            StartedAll = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(command))
                    {
                        cmmd = $"{CHIA} {keyringpath}{command}";
                        string dky = string.IsNullOrWhiteSpace(keyringpath) ? "NoPF" : "PF";
                        Common.CommonConstants.SaveDebugLog($"Command: [{dky}] {command}", false, true);
                        cmd.StandardInput.WriteLine(cmmd);
                    }

                    cmd.StandardInput.Flush();
                    cmd.StandardInput.Close();
                    DateTime st = DateTime.Now;
                    cmd.WaitForExit((timeout * 1000));
                    TimeSpan ts = DateTime.Now - st;
                    bool SuggestionDisplay = true;

                    if (ts.TotalSeconds > timeout)
                    {
                        Common.CommonConstants.SaveDebugLog($"CommandTimeout: {ts.TotalSeconds}: {timeout}", false, true);
                        if (CommandsWithPassphrase.Contains((command.Replace("chia ", ""))))
                        {
                            if (keyringpath.Length > 0)
                            {
                                clsStatus.AddLogMesssage("Invalid Passphrase....");
                                clsStatus.AddLogMesssage("Please logout and login with Valid Passphrase.");
                            }
                            else
                            {
                                clsStatus.AddLogMesssage("Failed to Connect with Chia. Passphrase Required.");
                                clsStatus.AddLogMesssage("Please logout and login with Passphrase.");
                            }
                            SuggestionDisplay = false;
                        }
                        Common.CommonConstants.SaveDebugLog($"Returned value: blank due to timeout & invalid pf", false, true);
                        return "";
                    }
                    else
                    {
                        Common.CommonConstants.SaveDebugLog($"CommandResponseTime: {ts.TotalSeconds} ss", false, true);
                    }
                    Common.CommonConstants.SaveDebugLog($"Reading Result.", false, true);
                    string result = cmd.StandardOutput.ReadToEnd();
                    Common.CommonConstants.SaveDebugLog($"Result Length: {result.Length}", false, true);
                    bool IsPfTxt = result.Contains("passphrase");
                    bool cmdWithPf = CommandsWithPassphrase.Contains(command);
                    CommonConstants.SaveDebugLog($"Pf Flags=> pftx:{IsPfTxt}, sgn:{SuggestionDisplay}, cwpf:{cmdWithPf}", false, true);

                    if (IsPfTxt && SuggestionDisplay && cmdWithPf)
                    {
                        if (result.Contains("Incorrect") || result.Contains("Invalid"))
                        {
                            if (!string.IsNullOrWhiteSpace(keyringpath))
                            {
                                Common.CommonConstants.SaveDebugLog($"rzlt:PF Incrt,", false, true);
                                clsStatus.AddLogMesssage(" Invalid Passphrase,");
                                clsStatus.AddLogMesssage(" Please logout and login with Valid Passphrase,");
                            }
                            else
                            {
                                Common.CommonConstants.SaveDebugLog($"rzlt:PF blnk,", false, true);
                                clsStatus.AddLogMesssage(" Failed to Connect with Chia. Passphrase Required,");
                                clsStatus.AddLogMesssage(" Please logout and login with Passphrase,");
                            }
                        }
                        else
                        {
                            Common.CommonConstants.SaveDebugLog($"Is Valid PF? Contains pf but no invalid/incorrect,", false, true);
                        }
                    }

                    if (result.Contains("Incorrect") || result.Contains("Invalid") || result.Contains("Exception") || result.Contains("Error"))
                        Common.CommonConstants.SaveDebugLog($"Something Wrong with CLI command Result ?:{Environment.NewLine}{result}", false, true);
                    return result;
                }
                #endregion

                #region  Windows
                else //Windows
                {
                    string path = GetChiaPath();
                    CommonConstants.SaveDebugLog($"chia path: {path}", false, true);

                    if (string.IsNullOrEmpty(path)) return string.Empty;

                    if (IsNonChiaCmd)
                    {
                        return Get_Save_FileList_Win(command, path);
                    }

                    keyringpath = MakePassphraseString(command);
                    cmd.StandardInput.WriteLine($@"{path}\chia {keyringpath} {command}");
                    string dky = string.IsNullOrWhiteSpace(keyringpath) ? "NoPF" : "PF";
                    Common.CommonConstants.SaveDebugLog($"Command: [{dky}] {command}", false, true);

                    cmd.StandardInput.Flush();
                    cmd.StandardInput.Close();

                    DateTime st = DateTime.Now;
                    cmd.WaitForExit((timeout * 1000));
                    TimeSpan ts = DateTime.Now - st;
                    bool SuggestionDisplay = true;

                    if (ts.TotalSeconds >= timeout)
                    {
                        Common.CommonConstants.SaveDebugLog($"CommandTimeout: {ts.TotalSeconds}: {timeout}", false, true);

                        if (CommandsWithPassphrase.Contains((command.Replace("chia ", ""))))
                        {
                            WaitTimeoutCount++;

                            string Err = "";
                            if (WaitTimeoutCount >= 2 && (IsPassphraseFile || TryWithoutPassphrase))
                            {
                                Err = "Please logout and login with Valid Passphrase";
                            }
                            else
                            {
                                if (IsPassphraseFile)
                                {
                                    clsStatus.AddLogMesssage("Invalid Passphrase.");
                                    TryWithoutPassphrase = true;
                                }
                                else
                                {
                                    clsStatus.AddLogMesssage("Failed to Connect with Chia. Passphrase Required.");
                                    clsStatus.AddLogMesssage("Please logout and login with Valid Passphrase.");
                                }
                                SuggestionDisplay = false;
                            }
                            if (Err.Length > 0)
                                clsStatus.AddLogMesssage(Err);
                            CommonConstants.SaveDebugLog($"Returned value: blank due to timeout & invalid/blank pf", false, true);
                            return "";
                        }
                    }
                    else
                    {
                        Common.CommonConstants.SaveDebugLog($"CommandResponseTime: {ts.TotalSeconds} ss", false, true);
                    }
                    Common.CommonConstants.SaveDebugLog($"Reading Result.", false, true);
                    string result = cmd.StandardOutput.ReadToEnd();
                    Common.CommonConstants.SaveDebugLog($"Result Length: {result.Length}", false, true);
                    bool IsPfTxt = result.Contains("passphrase");
                    bool cmdWithPf = CommandsWithPassphrase.Contains(command);
                    CommonConstants.SaveDebugLog($"Pf Flags=> pftx:{IsPfTxt}, sgn:{SuggestionDisplay}, cwpf:{cmdWithPf}", false, true);

                    if (IsPfTxt && SuggestionDisplay && cmdWithPf)
                    {
                        if (result.Contains("Incorrect") || result.Contains("Invalid"))
                        {
                            if (!string.IsNullOrWhiteSpace(keyringpath))
                            {
                                Common.CommonConstants.SaveDebugLog($"rzlt:PF Incrt,", false, true);
                                clsStatus.AddLogMesssage(" Invalid Passphrase,");
                                clsStatus.AddLogMesssage(" Please logout and login with Valid Passphrase,");
                            }
                            else
                            {
                                Common.CommonConstants.SaveDebugLog($"rzlt:PF blnk,", false, true);
                                clsStatus.AddLogMesssage(" Failed to Connect with Chia. Passphrase Required,");
                                clsStatus.AddLogMesssage(" Please logout and login with Passphrase,");
                            }
                        }
                        else
                        {
                            Common.CommonConstants.SaveDebugLog($"Is Valid PF? Contains pf but no invalid/incorrect,", false, true);
                        }
                    }

                    if (result.ToLower().Contains("passphrase"))
                    {
                        if (result.Contains("Incorrect") || result.Contains("Invalid") || result.Contains("Exception") || result.Contains("Error"))
                            Common.CommonConstants.SaveDebugLog($"Something Wrong with CLI command Result ?:{Environment.NewLine}Result: {result}", false, true);
                    }
                    return result;
                }
                #endregion
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                CommonConstants.DeletePassphraseFiles();
            }
        }

        static string Get_Save_FileList_Linux(string command)
        {
            Common.CommonConstants.SaveDebugLog($"System Command: {command}", false, true);
            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            Common.CommonConstants.SaveDebugLog($"Wait for Exit for System Command: {command}", false, true);
            cmd.WaitForExit(15000);
            Common.CommonConstants.SaveDebugLog($"Reading output for System Command: {command}", false, true);
            string fileList = cmd.StandardOutput.ReadToEnd();
            CommonConstants.SaveDebugLog($"Files List:{Environment.NewLine}{fileList}", false, true);
            return "";
        }

        static string Get_Save_FileList_Win(string command, string path)
        {
            Common.CommonConstants.SaveDebugLog($"System Command: {command}", false, true);
            cmd.StandardInput.WriteLine($@"{command} {path}");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            Common.CommonConstants.SaveDebugLog($"Wait for Exit for System Command: {command}", false, true);
            cmd.WaitForExit(15000);
            Common.CommonConstants.SaveDebugLog($"Reading output for System Command: {command}", false, true);
            string fileList = cmd.StandardOutput.ReadToEnd();
            CommonConstants.SaveDebugLog($"Files List:{Environment.NewLine}{fileList}", false, true);
            return "";
        }
        static string MakePassphraseString(string cmd)
        {
            string filepath = @$"{Common.CommonConstants.PassphraseFilePath}";
            string keyringpath = "";

            if (CommandsWithPassphrase.Contains((cmd.Replace("chia ", "")))) //cmd.Contains(keyShowCmd))
            {
                if (!string.IsNullOrEmpty(Common.CommonConstants.Passphrase))
                {
                    using (FileStream fs = System.IO.File.Create(filepath))
                    {
                        Byte[] passph = new System.Text.UTF8Encoding(true).GetBytes(Common.CommonConstants.Passphrase);
                        fs.Write(passph, 0, passph.Length);
                        fs.Flush();
                        TryWithoutPassphrase = false;
                    }
                }
                if (!TryWithoutPassphrase && System.IO.File.Exists(filepath))
                {
                    IsPassphraseFile = true;
                    keyringpath = @$"--passphrase-file ""{filepath}"""; // @"--passphrase-file C:\Users\m.azeem\.chia_keys/keyring.txt";
                    if (cmd.Contains(CommandsWithPassphrase[0]))
                        clsStatus.AddLogMesssage("Trying to Connect with Chia using Passphrase.");
                }
                else
                {
                    if (cmd.Contains(CommandsWithPassphrase[0]))
                        clsStatus.AddLogMesssage("Trying to Connect with Chia without Passphrase.");
                }
            }
            return keyringpath;
        }
        static string MakePassphraseStringLinux(string cmd)
        {
            string keyringpath = "";

            if (CommandsWithPassphrase.Contains((cmd.Replace("chia ", "")))) //cmd.Contains(keyShowCmd))
            {

                if (!TryWithoutPassphrase && !string.IsNullOrEmpty(Common.CommonConstants.Passphrase))
                {
                    IsPassphraseFile = true;
                    keyringpath = @$"--passphrase-file <(echo -n '{Common.CommonConstants.Passphrase}') "; // @"--passphrase-file C:\Users\m.azeem\.chia_keys/keyring.txt";
                    if (cmd.Contains(CommandsWithPassphrase[0]))
                    {
                        clsStatus.AddLogMesssage("Trying to Connect with Chia using Passphrase.");
                    }
                }
                else
                {
                    if (cmd.Contains(CommandsWithPassphrase[0]))
                    {
                        clsStatus.AddLogMesssage("Trying to Connect with Chia without Passphrase.");
                    }
                    TryWithoutPassphrase = false;
                }
            }
            return keyringpath;
        }

        public static JArray GetChallenges(out Common.CommonConstants.ErrorCodes ErrCode)
        {
            ErrCode = Common.CommonConstants.ErrorCodes.NONE;
            string result = ReadCommand("farm challenges");

            if (string.IsNullOrEmpty(result))
                ErrCode = Common.CommonConstants.ErrorCodes.Blank_Response;

            IEnumerable<string> lines = result.Split(new char[] { '\n' });
            Common.CommonConstants.SaveDebugLog($"GetChallenges: Length:{lines.Count()}", false, true);

            JArray finalString = new JArray();
            JObject currentObject;
            string indexLabel = "Index";
            string hashLabel = "Hash";

            int indexIndex = -1;
            int hashIndex = -1;

            string indexValue = "";
            string hashValue = "";

            string newString = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith(hashLabel) && line.Contains(indexLabel))
                {
                    newString = line.Replace(" ", "").Replace("\r", "");

                    indexIndex = newString.IndexOf(indexLabel);
                    hashIndex = newString.IndexOf(hashLabel);

                    indexValue = newString.Substring(indexIndex + indexLabel.Length + 1);
                    hashValue = newString.Substring(hashIndex + hashLabel.Length + 1, indexIndex - hashLabel.Length - 1);
                    currentObject = new JObject();
                    currentObject.Add(indexLabel, indexValue);
                    currentObject.Add(hashLabel, hashValue);
                    finalString.Add(currentObject);
                }
            }

            if (!string.IsNullOrEmpty(result) && finalString.Count <= 0)
                ErrCode = Common.CommonConstants.ErrorCodes.CLI_Missing_RequiredData;

            return finalString;
        }

        public static string GetFarmingStatus(out int totalHarvesterPlots)
        {
            string result = ReadCommand("farm summary");

            string[] lines = result.Split(new char[] { '\n' });

            totalHarvesterPlots = 0;
            JObject finalResponse = new JObject();
            string stringBeforeCollon = string.Empty;
            char collon = ':';
            int indexOfCollon = -1;

            JArray harvesters = new JArray();
            JObject singleHarvester;
            string newString = string.Empty;
            string valueString = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart(' ').TrimEnd('\r');
                if (line.Contains(collon))
                {
                    newString = line;//.Replace("\r", "");
                    indexOfCollon = newString.IndexOf(collon);
                    stringBeforeCollon = newString.Substring(0, indexOfCollon);
                    valueString = stringBeforeCollon switch
                    {
                        "Farming status" => newString.Substring(indexOfCollon + 1),
                        "Total chia farmed" => newString.Substring(indexOfCollon + 1).Replace(" ", ""),
                        "User transaction fees" => newString.Substring(indexOfCollon + 1).Replace(" ", ""),
                        "Block rewards" => newString.Substring(indexOfCollon + 1).Replace(" ", ""),
                        "Plot count for all harvesters" => newString.Substring(indexOfCollon + 1).Replace(" ", ""),
                        "Total size of plots" => newString.Substring(indexOfCollon + 1),
                        "Estimated network space" => newString.Substring(indexOfCollon + 1),
                        "Expected time to win" => newString.Substring(indexOfCollon + 1),
                        _ => ""
                    };

                    if (stringBeforeCollon.Equals("Plot count for all harvesters"))
                    {
                        totalHarvesterPlots = Convert.ToInt32(valueString);
                    }

                    if (valueString != string.Empty)
                    {
                        finalResponse.Add(stringBeforeCollon.ToLower().Replace(" ", "_"), valueString);
                    }
                }
                line = line.TrimStart(' ').TrimEnd('\r');
                if (line.StartsWith("Local Harvester") || line.StartsWith("Remote Harvester for IP:"))
                {
                    i++;
                    string harvesterStringToFind = " plots of size: ";
                    string harvesterDetailLine = lines[i].TrimStart(' ').TrimEnd('\r');
                    singleHarvester = new JObject();
                    if (line.StartsWith("Local Harvester"))
                    {
                        singleHarvester.Add("Address", "Local");
                    }
                    if (line.StartsWith("Remote Harvester for IP:"))
                    {
                        singleHarvester.Add("Address", line.Substring("Remote Harvester for IP: ".Length));
                    }
                    if (harvesterDetailLine.Contains(harvesterStringToFind))
                    {
                        singleHarvester.Add("plots_count", harvesterDetailLine.Substring(0, harvesterDetailLine.IndexOf(harvesterStringToFind)));
                        singleHarvester.Add("plots_size", harvesterDetailLine.Substring(harvesterDetailLine.IndexOf(harvesterStringToFind) + harvesterStringToFind.Length));
                    }
                    harvesters.Add(singleHarvester);
                }
            }
            if (harvesters.Count > 0)
            {
                finalResponse.Add("harvesters", harvesters);
            }
            return finalResponse.ToString();
        }

        public static JObject GetSyncStatus(ref string prevStatus)
        {
            JObject syncAlert = new JObject();

            try
            {
                string result = ReadCommand("show -s");

                if (string.IsNullOrEmpty(prevStatus) || !result.Contains(prevStatus))
                {
                    string startStr = "Current Blockchain Status: ";
                    string endStr = "Peak:";

                    if (result.Contains(startStr) && result.Contains(endStr))
                    {
                        int strtIndex = result.IndexOf(startStr) + startStr.Length;
                        int endIndex = result.IndexOf(endStr);
                        int countToCopy = endIndex - strtIndex;
                        string status = result.Substring(strtIndex, countToCopy);

                        if (status.Contains("Syncing"))
                        {
                            syncAlert.Add("synced", "Syncing");
                            prevStatus = "Syncing";
                        }
                        else
                        {
                            status = status.Trim();
                            syncAlert.Add("synced", status);
                            prevStatus = status;
                        }
                    }
                    else
                    {
                        CommonConstants.SaveDebugLog($"Warning: Wrong Sync Status-> Current Read: {result.Trim()}, Prev: {prevStatus}", false, true);
                        syncAlert.Add("synced", string.IsNullOrWhiteSpace(prevStatus) ? "Unknown" : prevStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                clsStatus.AddLogMesssage($"GetSyncStatus: {ex.Message}");
            }
            return syncAlert;
        }

        SettingsRepository _settings;
        public static string GetFingerPrint(string dfolder)
        {
            ChiaClientSettings.DataFolder = dfolder;
            string start = "Fingerprint:";
            string result = ReadCommand("keys show");
            IEnumerable<string> lines = result.Split(new char[] { '\n' });

            foreach (var line in lines)
            {
                if (line.StartsWith(start))
                {
                    fingerprint = line.Substring(start.Length).TrimStart(' ').Trim('\r');
                    return fingerprint;
                }
            }
            return string.Empty;
        }

        public static Dictionary<string, int> GetOGAndProtablePlotCount()
        {
            string OgString = "Pool public key:";
            string portablestring = $"{OgString} None:";
            string result = ReadCommand("chia plots check -n 5");
            int totalPlotCount = 0, OgPlotCount = 0, PortablePlotCount = 0;
            Dictionary<string, int> keyValuePairs;
            if (result.Contains("plots of size"))
            {
                IEnumerable<string> lines = result.Split(new char[] { '\n' });

                foreach (var line in lines)
                {
                    if (line.Contains(portablestring))
                    {
                        PortablePlotCount++;
                    }
                    else if (line.Contains(OgString))
                    {
                        OgPlotCount++;
                    }

                    if (line.Contains("Found plot"))
                    {
                        totalPlotCount++;
                    }
                }

                Common.CommonConstants.SaveDebugLog($"TL:{totalPlotCount},OG:{OgPlotCount},Portable:{PortablePlotCount}", false, true);
                if (totalPlotCount != (OgPlotCount + PortablePlotCount))
                {
                    Common.CommonConstants.SaveDebugLog($"Wrong Plots Count: Total != OG + Portable", false, true);
                }
            }
            keyValuePairs = new Dictionary<string, int> { { "Total", totalPlotCount }, { "OG", OgPlotCount }, { "Portable", PortablePlotCount } };

            return keyValuePairs;
        }

        public static string GetChiaVersion()
        {
            string result = ReadCommand("version");
            CommonConstants.SaveDebugLog(result, false, true);

            try
            {
                string[] spltdRslt = null;
                if (result != null && result.Contains("chia  version"))
                {
                    spltdRslt = result.Split("chia  version");
                    if (spltdRslt != null && spltdRslt.Length > 1)
                    {
                        string[] lines = spltdRslt[1].Split(new char[] { '\n' });

                        foreach (var item in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(item) && item.Contains("."))
                            {
                                result = item.Trim('\r');
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"EXCEPTION in parsing: GetChiaVersion: {ex.Message}", false, true);
            }

            result = $"Installed Chia Version: {result}";
            clsStatus.AddLogMesssage(result);

            return result;
        }
        public static void GetAndSaveChiaFolderFileList()
        {
            try
            {
                string sysCmd = Environment.OSVersion.Platform == PlatformID.Win32NT ? "dir /o:d" : "ls -l";
                ReadCommand(sysCmd, true);
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Exception in GetAndSaveChiaFolderFileList: {ex.Message}", false, true);
                throw;
            }
        }

        static string poolStrStart = "Wallet id ";

        public async static Task<(JArray, JObject)> GetPoolsFromPlotNFT2(int totalPlots, Dictionary<string, PoolInfo> launcherId_PoolMiscInfo_Pair)
        {
            string result = ReadCommand("plotnft show");
            CommonConstants.SaveDebugLog($"Before Parsing, Pools Data Read: {result}", false, true);
            string[] poolsdata = result.Split(poolStrStart);

            Common.CommonConstants.SaveDebugLog($"After split, Total pools count: {(poolsdata == null ? 0 : (poolsdata.Count() - 1))}", false, true);

            string stringBeforeCollon = string.Empty;
            char collon = ':';
            int indexOfCollon = -1;

            JArray Pools = new JArray();
            JObject farmInfo = new JObject();
            string newString = string.Empty;
            string valueString = string.Empty;
            double totalBalance = 0;
            long totalPoolsPoints_24h = 0;
            long totalPortablePlots = 0;
            long totalPortablePlotsSelfPool = 0;
            bool IsSelf_Pool = false;
            string currentState = "";
            try
            {
                foreach (var pd in poolsdata)
                {
                    if (pd.Contains("Current state") && pd.Contains("Launcher ID"))
                    {
                        IsSelf_Pool = false;
                        Common.CommonConstants.SaveDebugLog($"Valid pool found", false, true);
                        string[] lines = pd.Split(new char[] { '\n' });
                        JObject singlePool = new JObject();

                        Common.CommonConstants.SaveDebugLog($"Single Pool Data Lines count:{lines.Length}");

                        foreach (var line in lines)
                        {
                            try
                            {
                                indexOfCollon = -1;
                                newString = line;
                                // Common.CommonConstants.SaveDebugLog($"newstr: {newString}", false, false);

                                if (newString.Contains(collon))
                                {
                                    indexOfCollon = newString.IndexOf(collon);
                                    stringBeforeCollon = newString.Substring(0, indexOfCollon);
                                    //Common.CommonConstants.SaveDebugLog($"Contains: Colonindex:{indexOfCollon}: 1stringb4:{stringBeforeCollon}", false, false);

                                    if (!stringBeforeCollon.Contains(" "))
                                    {
                                        stringBeforeCollon = "wallet_id";
                                        valueString = newString.Substring(0, indexOfCollon);
                                        try
                                        {
                                            var test = Convert.ToInt32(valueString.Trim());
                                            //Common.CommonConstants.SaveDebugLog($"Forwallet: val:{valueString}: 2stringb4:{stringBeforeCollon}");
                                        }
                                        catch (Exception ex)
                                        {
                                            Common.CommonConstants.SaveDebugLog($"Warning: Invalid wallet no. so skip: val:{valueString}: 2stringb4:{stringBeforeCollon}, newstr:{newString}, EXCEPTION: {ex.Message}", false, true);
                                            continue;
                                        }
                                    }

                                    switch (stringBeforeCollon)
                                    {
                                        //case "Wallet id": valueString = walletid; break;
                                        case "wallet_id":
                                            break;
                                        case "Current state":
                                            //Common.CommonConstants.SaveDebugLog($"sw1: newstr:{newString}: colonIndx:{indexOfCollon}", false, false);
                                            valueString = newString.Substring(indexOfCollon + 1);
                                            //Common.CommonConstants.SaveDebugLog($"sw2: valuestring:{valueString}", false, false);

                                            if (valueString.Contains("FARMING_TO_POOL"))
                                            {

                                            }
                                            else if (valueString.Contains("SELF_POOLING"))
                                            {
                                                IsSelf_Pool = true;
                                            }
                                            else
                                            {
                                                Common.CommonConstants.SaveDebugLog($"Unknown Pooling Status:{valueString}", false, true);
                                                currentState = valueString;
                                            }
                                            break;
                                        case "Launcher ID": valueString = newString.Substring(indexOfCollon + 1); break;
                                        case "Number of plots":
                                            valueString = newString.Substring(indexOfCollon + 1);
                                            totalPortablePlots += Convert.ToInt64(valueString);
                                            if (IsSelf_Pool)
                                                totalPortablePlotsSelfPool += Convert.ToInt64(valueString);
                                            break;
                                        case "Current pool URL": valueString = newString.Substring(indexOfCollon + 1); break;
                                        case "Current difficulty": valueString = newString.Substring(indexOfCollon + 1); break;
                                        case "Points found (24h)": valueString = newString.Substring(indexOfCollon + 1); totalPoolsPoints_24h += Convert.ToInt64(valueString); break;
                                        case "Points balance": valueString = newString.Substring(indexOfCollon + 1); totalBalance += Convert.ToDouble(valueString); break;
                                        case "Percent Successful Points (24h)": valueString = newString.Substring(indexOfCollon + 1); break;
                                        case "Payout instructions (pool will pay to this address)": valueString = newString.Substring(indexOfCollon + 1); stringBeforeCollon = "Payout address"; break;
                                        case "Current state from block height": valueString = ""; break;
                                        case "Target address (not for plotting)": valueString = ""; break;
                                        case "Owner public key": valueString = ""; break;
                                        case "Pool contract address (use ONLY for plotting - do not send money to this address)": valueString = ""; break;
                                        case "Target state": valueString = ""; break;
                                        case "Target pool URL": valueString = ""; break;
                                        case "Relative lock height": valueString = ""; break;
                                        case "Claimable balance": valueString = ""; break;
                                        default:
                                            if (!string.IsNullOrWhiteSpace(stringBeforeCollon))
                                                Common.CommonConstants.SaveDebugLog($"stringBeforeCollon NotFound: {stringBeforeCollon}", false, true);
                                            valueString = ""; break;
                                    }

                                    //Common.CommonConstants.SaveDebugLog($"b4 Adding: Val:{valueString}: 3stringb4:{stringBeforeCollon}", false, true);

                                    if (valueString != string.Empty && !string.IsNullOrWhiteSpace(stringBeforeCollon))
                                    {
                                        valueString = valueString.Trim();
                                        string key = stringBeforeCollon.ToLower().Replace(" ", "_");
                                        //Common.CommonConstants.SaveDebugLog($"Adding key:{key}", false, true);
                                        singlePool.Add(key, valueString);

                                        #region Points since start
                                        if (key.Contains("launcher_id"))
                                        {
                                            string pointsStartVal = "";
                                            try
                                            {
                                                foreach (var k in launcherId_PoolMiscInfo_Pair)
                                                {
                                                    if (k.Key.Contains(valueString))
                                                    {
                                                        k.Value.FarmingStatus = currentState;
                                                        pointsStartVal = k.Value.PointsSinceStart;
                                                        break;
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Common.CommonConstants.SaveDebugLog($"Unable to find launch id:{valueString}, EXCEPTION: {ex.Message}", false, true);
                                            }
                                            if (string.IsNullOrWhiteSpace(pointsStartVal))
                                                pointsStartVal = "0";

                                            singlePool.Add("points_found_since_start", pointsStartVal);

                                            //Common.CommonConstants.SaveDebugLog($"Adding: {"pointsStartVal"}:{pointsStartVal}");
                                        }
                                        #endregion

                                        #region pool name
                                        else if (key.Contains("current_pool_url"))
                                        {
                                            string poolname = "";
                                            try
                                            {
                                                if (PoolNameList.ContainsKey(valueString)) //If already read
                                                {
                                                    //Common.CommonConstants.SaveDebugLog($"pool url: {valueString}");
                                                    poolname = PoolNameList[valueString];
                                                    //Common.CommonConstants.SaveDebugLog($"pool Name: {poolname}");
                                                }
                                                else
                                                {
                                                    Common.CommonConstants.SaveDebugLog($"pool url to Read: {valueString}", false, true);
                                                    PoolClient poolClient = new PoolClient(valueString);
                                                    var pinfo = await poolClient.GetPoolInfo();
                                                    //Common.CommonConstants.SaveDebugLog($"pool info read: {pinfo}");
                                                    poolname = GetPoolName(pinfo);
                                                    //Common.CommonConstants.SaveDebugLog($"pool Name read: {poolname}");

                                                    if (string.IsNullOrEmpty(poolname) || poolname.Contains("N/A"))
                                                    {
                                                    }
                                                    else
                                                    {
                                                        PoolNameList.Add(valueString, poolname); //Added to dictionary. Next time no need to read again
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Common.CommonConstants.SaveDebugLog($"ExceptioninPoolNameGetting: ({valueString}) :{ex.Message}", false, true);
                                                poolname = "N/A.";
                                            }

                                            if (string.IsNullOrWhiteSpace(poolname))
                                                poolname = "N/A";

                                            singlePool.Add("pool_name", poolname);
                                        }
                                        #endregion

                                        #region estimated plot size
                                        else if (key.Contains("number_of_plots"))
                                        {
                                            if (string.IsNullOrWhiteSpace(valueString)) valueString = "0";
                                            try
                                            {
                                                int plotcount = Convert.ToInt32(valueString.Trim());
                                                double estimatedPlotSize = plotcount * 101.4;
                                                singlePool.Add("estimated_plot_size", estimatedPlotSize.ToString());
                                            }
                                            catch (Exception ex)
                                            {
                                                singlePool.Add("estimated_plot_size", "0.00");

                                            }
                                        }
                                        #endregion

                                    }
                                    else
                                    {
                                        Common.CommonConstants.SaveDebugLog($"GetPoolsFromPlotNFT2: Found Empty: not adding: Val:{valueString}: 4stringb4:{stringBeforeCollon}", false, true);
                                    }
                                }
                                else
                                {
                                    Common.CommonConstants.SaveDebugLog($"GetPoolsFromPlotNFT2: NotProcessing:{line}", false, true);
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.CommonConstants.SaveDebugLog($"GetPoolsFromPlotNFT2: EXCEPTION in pd loop: {ex.Message}", false, true);
                            }
                        }
                        Pools.Add(singlePool);
                    }
                    else
                    {
                        Common.CommonConstants.SaveDebugLog($"GetPoolsFromPlotNFT2: NOTFOUND Current state:{pd}", false, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.CommonConstants.SaveDebugLog($"GetPoolsFromPlotNFT: Exception: {ex.Message}", false, true);

            }

            long ogPlotCount = (totalPlots < totalPortablePlots) ? 0 : totalPlots - totalPortablePlots;
            farmInfo.Add("total_balance", totalBalance);
            farmInfo.Add("total_pools_connected", Pools.Count);
            farmInfo.Add("total_pool_points", totalPoolsPoints_24h);
            farmInfo.Add("total_og_plots_count", ogPlotCount);
            farmInfo.Add("total_portable_plots_count", totalPortablePlots);
            farmInfo.Add("total_portable_plots_count_self_pool", totalPortablePlotsSelfPool);

            Common.CommonConstants.SaveDebugLog($"TotalPlot:{totalPlots}, Portable:{totalPortablePlots}, Selfpool:{totalPortablePlotsSelfPool}, OG:{ogPlotCount}", false, true);
            (JArray, JObject) rzlt = new(Pools, farmInfo);
            return rzlt;
        }

        private static string GetPoolName(string poolinfo)
        {
            try
            {
                string[] lines = poolinfo.Split(new string[] { "\"," }, StringSplitOptions.TrimEntries);

                string stringBeforeCollon = string.Empty;
                char collon = ':';
                int indexOfCollon = -1;

                string newString = string.Empty;
                string valueString = string.Empty;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].TrimStart(' ').TrimEnd('\r').Replace("{", "");
                    if (line.Contains(collon))
                    {
                        newString = line;
                        indexOfCollon = newString.IndexOf(collon);
                        stringBeforeCollon = newString.Substring(0, indexOfCollon);
                        //Common.CommonConstants.SaveDebugLog($"without Trimm:{stringBeforeCollon}");
                        //stringBeforeCollon = stringBeforeCollon.Trim().Replace("\"", "");
                        //Common.CommonConstants.SaveDebugLog($"Trimmed:{stringBeforeCollon}");

                        //valueString = stringBeforeCollon switch
                        //{
                        //    "name" => newString.Substring(indexOfCollon + 1),
                        //    "minimum_difficulty" => "",
                        //    "relative_lock_height" => "",
                        //    "protocol_version" => "",
                        //    "fee" => "",
                        //    "description" => "",
                        //    "target_puzzle_hash" => "",
                        //    "authentication_token_timeout" => "",
                        //    _ => $"default:{stringBeforeCollon}:{indexOfCollon}"
                        //};

                        if (stringBeforeCollon.Contains("name"))
                        {
                            valueString = newString.Substring(indexOfCollon + 2);
                            return valueString;
                        }
                        else if (valueString.Contains("default"))
                        {
                            Common.CommonConstants.SaveDebugLog($"Name Line:{line}, ColIndex:{indexOfCollon}, Strb4:{stringBeforeCollon}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.CommonConstants.SaveDebugLog($"GetPoolName: {ex.Message}{Environment.NewLine}Returning N/A..", false, true);
            }
            return "N/A..";
        }
    }
}
