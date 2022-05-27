using Chia.Common;
using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChiaClientUI
{
    public class Program
    {
        static string chiaClientServiceName = "ChiaClientService";
        static string chiaClientUIName = "ChiaClientUI";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseElectron(args);
                    CommonConstants.SaveDebugLog("Host Builder Created", false, true);
                });

        static bool StartProcessIfNotRunning(string processName, bool isTask = false, string taskname = "")
        {
            Console.WriteLine($"======================================");
            Console.WriteLine($"Checking status for {processName}");
            DisplayDebugStringOnConsole($"Checking status for {processName}", false, true);

            string exeName = processName + ".exe";
            if (File.Exists(exeName))
            {
                if (Process.GetProcessesByName(processName).Length == 0)
                {
                    Console.WriteLine("starting new process");

                    if (isTask)
                    {
                        DisplayDebugStringOnConsole($"Creating task {taskname}", false, true);
                        CreateAutoRunnerTask(taskname);
                    }
                    else
                    {
                        return StartProcess(exeName);
                    }
                }
                else
                    Console.WriteLine($"Process is already running.");
            }
            else
            {
                Console.WriteLine($"Exe file does not exist.");
            }
            return false;
        }

        static bool StartProcess(string fileName)
        {
            Console.WriteLine("starting");
            Process process = new Process();
            process.StartInfo.FileName = fileName; // relative path. absolute path works too.
            process.StartInfo.UseShellExecute = false;
            process.Start();
            return false;
        }

        public static void CreateAutoRunnerTask(string taskname)
        {
            try
            {
                var taskstatus = TaskExistsInScheduler(taskname, out string status);

                if (!taskstatus.taskExists)
                {
                    CommonConstants.SaveDebugLog("Task not exists", false, true);

                    string appPath = Path.Combine(System.AppContext.BaseDirectory, $"ChiaClientService.exe");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = @$"/C schtasks /create /SC ONLOGON /TN ""{taskname}"" /TR ""{appPath}"" /RL HIGHEST /RU ""NT AUTHORITY\SYSTEM""";
                    ////SCHTASKS /CREATE /SC ONLOGON /TN "SmartWindows Auto Runner" /TR "C:\Program Files\FiveRivers Technologies\SmartWindows\SmartWindowsApp.exe" /RL HIGHEST
                    startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    if (System.Environment.OSVersion.Version.Major < 6)
                    {
                        startInfo.Verb = "runas";
                    }
                    Process process = Process.Start(startInfo);
                    startInfo = null;
                    CommonConstants.SaveDebugLog("Task created", false, true);
                }
                //else
                //{
                ////if(!taskstatus.isRunning)
                try
                {
                    CommonConstants.SaveDebugLog("Changing task", false, true);

                    string appPath = Path.Combine(System.AppContext.BaseDirectory, $"ChiaClientService.exe");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = @$"/C schtasks /change /SC ONLOGON /TN ""{taskname}"" /TR ""{appPath}"" /RL HIGHEST /RU ""NT AUTHORITY\SYSTEM""";
                    ////SCHTASKS /CREATE /SC ONLOGON /TN "SmartWindows Auto Runner" /TR "C:\Program Files\FiveRivers Technologies\SmartWindows\SmartWindowsApp.exe" /RL HIGHEST
                    startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    if (System.Environment.OSVersion.Version.Major < 6)
                    {
                        startInfo.Verb = "runas";
                    }
                    Process process = Process.Start(startInfo);
                    startInfo = null;
                    CommonConstants.SaveDebugLog("Changed task", false, true);
                }
                catch (Exception ex)
                {
                    CommonConstants.SaveDebugLog($"Exception task change: {ex.Message}", false, true);
                }
                //}

                if (!taskstatus.isRunning)
                {
                    CommonConstants.SaveDebugLog("Staring task", false, true);

                    UpdateUserTaskInScheduler(taskname, "Run");
                }
                else
                {
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static (bool taskExists, bool isRunning) TaskExistsInScheduler(string taskName, out string status)
        {
            bool _taskExists = false;
            bool _isRunning = false;
            status = "";
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = @$"/C schtasks /query /TN ""{taskName}"""; //Check if task exists
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (System.Environment.OSVersion.Version.Major < 6)
                {
                    startInfo.Verb = "runas";
                }
                using (Process process = Process.Start(startInfo))
                {
                    // Read in all the text from the process with the StreamReader.
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string stdout = reader.ReadToEnd();
                        status = stdout;
                        _taskExists = stdout.Contains(taskName); //If task exists
                        _isRunning = stdout.Contains("Running");
                        stdout = null;
                        reader.Close();
                        reader.Dispose();
                    }
                }
                startInfo = null;
            }
            catch (Exception)
            {
                throw;
                //MessageBox.Show(ex.Message);
            }
            return (_taskExists, _isRunning);
        }

        public static void UpdateUserTaskInScheduler(string taskname, string action)
        {
            try
            {
                //if (TaskExistsInScheduler(taskname))
                //{
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                //startInfo.Arguments = @$"/C schtasks /query /TN ""{_autoUpdaterTaskName}"""; //Check if task exists
                startInfo.RedirectStandardOutput = false;
                startInfo.UseShellExecute = true;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (System.Environment.OSVersion.Version.Major < 6)
                {
                    startInfo.Verb = "runas";
                }
                CommonConstants.SaveDebugLog($"Action: {action}", false, true);

                switch (action)
                {
                    case "Enable":
                        startInfo.Arguments = @$"/C schtasks /Change /TN ""{taskname}""  /Enable";
                        break;

                    case "Disable":
                        startInfo.Arguments = @$"/C schtasks /Change /TN ""{taskname}"" /Disable";
                        break;

                    case "Run":
                        startInfo.Arguments = @$"/C schtasks /RUN /TN ""{taskname}""";
                        break;

                    case "End":
                        startInfo.Arguments = @$"/C schtasks /END /TN ""{taskname}""";
                        break;
                }
                Process.Start(startInfo).WaitForExit();
                CommonConstants.SaveDebugLog($"Arguments: {startInfo.Arguments}", false, true);
                startInfo = null;
                //}
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"UpdateUserTaskInScheduler: Exception: {ex.Message}");
                throw;
                //MessageBox.Show(ex.Message);
            }
        }

        public static void DisplayDebugStringOnConsole(string msg, bool display = false, bool saveToFile = false)
        {
            //if (CommonConstants.IsDebug)
            {
                if (display)
                    Console.WriteLine(msg);

                try
                {
                    if (saveToFile)
                    {
                        string path = System.IO.Path.Combine(CommonConstants.DataFolder, "Srvdebug.txt");
                        FileHandler.SaveFile_Log(path, $"{msg}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception During DebugSave: {ex.Message}");
                }
            }
        }
    }
}
