using System;
using Microsoft.Deployment.WindowsInstaller;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Net;
using Microsoft.Win32;
using System.Threading;

//using AutoWindowManagement.Common;
//using Gappalytics.Sample;
//using AutoWindowManagement.Common;
using TestFormsApp;

namespace CustomAction
{
    public class CustomActions
    {

        #region Constants
        //const string ChiaServiceProcessName = "ChiaClientService";
        const string ChiaUIProcessName = "ChiaClientUI";
        //const string ExeName = "SmartWindowsApp.exe";
        const string VersionFilePath = "https://chia-installer.s3.amazonaws.com/currentVersion.txt";
        const string RegistryAddress64 = @"SOFTWARE\WOW6432Node\FiveRivers Technologies\5FarmChiaClient";
        const string RegistryAddress32 = @"SOFTWARE\FiveRivers Technologies\5FarmChiaClient";
        #endregion

        [CustomAction]
        public static ActionResult DeleteAppDataFolder(Session session)
        {
            try
            {
                session.Log("Begin DeleteAppDataFolder");
                //string dynamicFolderPath = 
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), session["Manufacturer"], session["ProductName"]);// @"C:\ProgramData\FiveRiversTechnologies\SmartWindows";
                //MessageBox.Show(dynamicFolderPath+Environment.NewLine+folderPath);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                string message = string.Format("ERROR While deleting SmartWindows AppData Folder {0}", ex.ToString());
                session.Log(message);
                StartForm(message, "Error");
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult KillRunningApp(Session session)
        {
            try
            {
                //KillProcess(ChiaServiceProcessName);
                KillProcess(ChiaUIProcessName);

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                string message = $"ERROR While Closing running app. {ex.ToString()}";
                session.Log(message);
                StartForm(message, "Error");
                //System.Windows.Forms.MessageBox.Show(message, "Error");
                return ActionResult.Failure;
            }
        }

        //ProductVersion
        static string globalMessage = string.Empty;
        static string globalCaption = string.Empty;
        private static void StartForm(string message, string caption)
        {
            try
            {
                globalMessage = message;
                globalCaption = caption;

                Thread thread = new Thread(StartTh);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private static void StartTh()
        {
            var windo = new MainWindow(globalMessage, globalCaption);
            windo.ShowDialog();
        }

        [CustomAction]
        public static ActionResult InstalledVersionCheck(Session session)
        {
            try
            {
                string installedVersion = GetInstalledVersion();// session["ProductVersion"];
                string versionToBeInstalled = GetVersionToBeInstalled();


                session["ProductVersion"] = versionToBeInstalled;
                //new MessageBoxCustom("Testing", "Error").ShowDialog();

                //MessageBox.Show($"Installed:{installedVersion} ToBeInstalled:{versionToBeInstalled}");
                if (installedVersion == string.Empty) return ActionResult.Success;
                if (versionToBeInstalled == string.Empty)
                {
                    string message = "Error While Getting Application version";
                    StartForm(message, "Error");

                    return ActionResult.Failure;
                }
                if (installedVersion.CompareTo(versionToBeInstalled) >= 0)
                {
                    string message = $"Application version {installedVersion} is already installed on this system. If you wish to install again, please uninstall the current version first.";
                    StartForm(message, "Already Installed!");
                    return ActionResult.UserExit;
                }
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                string message = string.Format("Error While checking Application installed version. {0}", ex.ToString());
                session.Log(message);
                //MessageBox.Show(ex.Message);
                StartForm(message, "Error");
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult InstallationCompletionCheck(Session session)
        {
            try
            {
                //new CustomActionForms.MessageBoxCustom().ShowDialog();

                string chiaUiFileTobeChecked = Path.Combine(session["APPDIR"], $"{ChiaUIProcessName}.exe");
                //string chiaServiceFileTobeChecked = Path.Combine(session["APPDIR"], $"{ChiaServiceProcessName}.exe");

                string installedVersion = GetInstalledVersion();// session["ProductVersion"];
                string versionToBeInstalled = GetVersionToBeInstalled();
                //MessageBox.Show($"Condition1:{fileTobeChecked} , {installedVersion} , {versionToBeInstalled}", "Information");
                if (!File.Exists(chiaUiFileTobeChecked) || 
                    //!File.Exists(chiaServiceFileTobeChecked) || 
                    !installedVersion.Equals(versionToBeInstalled))
                {
                    StartForm("Application installation failed. Please check your internet or contact support team.", "Error");
                    return ActionResult.Failure;
                }

                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                string message = $"Error While checking  installation Completion. {ex.ToString()}" ;
                session.Log(message);
                StartForm(message, "Error");
                return ActionResult.Failure;
            }
        }

        /*
        [CustomAction]
        public static ActionResult UnInstallTracking(Session session)
        {
            try
            {
                //new CustomActionForms.MessageBoxCustom().ShowDialog();
                //[CommonAppDataFolder][Manufacturer]\[ProductName]
                string fileTobeChecked = Path.Combine(session["CommonAppDataFolder"], session["Manufacturer"], "SmartWindows", "analytics_attributes.txt");
                //StartForm(fileTobeChecked + "-- " + Process.GetCurrentProcess().Id, "AnalyticsFile");
                AnalyticsAttributes obj = null;
                if (File.Exists(fileTobeChecked))
                {
                    try
                    {
                        obj = Serializer.DeSerializeObject<AnalyticsAttributes>(fileTobeChecked);
                    }
                    catch (Exception)
                    {
                    }
                }

                if(obj == null)
                {
                    obj = new AnalyticsAttributes(new Random().Next(), 1, DateTime.Now);
                }

                if (obj != null)
                {
                    AnalyticsHelper.Attributes = obj;
                }

                AnalyticsHelper.Current.PostPageView("Uninstall", true);
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                string message = string.Format("Error While logging uninstall to Google Analytics. {0}", ex.Message.ToString());
                session.Log(message);
                StartForm(message, "Error");
                return ActionResult.Failure;
            }
        }
        */
        #region Private Methods
        private static void KillProcess(string processName)
        {
            Process[] process = Process.GetProcessesByName(processName);
            if (process != null && process.Length > 0)
            {
                foreach (var p in process)
                    p.Kill();
            }
        }
        private static string RegistryAddress
        {
            get
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    return RegistryAddress64;
                }
                else
                {
                    return RegistryAddress32;
                }
            }
        }

        private static string GetInstalledVersion()
        {
            string keyValue = string.Empty;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryAddress))
            {
                if (key != null)
                {
                    Object o = key.GetValue("Version");
                    if (o != null)
                        keyValue = o.ToString();
                }
            }
            return keyValue;
        }

        private static string GetVersionToBeInstalled()
        {

            try
            {
                WebClient client = new WebClient();
                var version = client.DownloadString(VersionFilePath);
                return version;
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message,"Error");
                return string.Empty;
            }
        }

        #endregion
    }
}
