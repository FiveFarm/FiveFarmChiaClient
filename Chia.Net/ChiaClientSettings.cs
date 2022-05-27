using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Chia.Net
{
    public class ChiaClientSettings
    {
        public static string Passphrase = "";
        public static string DataFolder = "";
        private static string ConfigFilePath
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".chia\mainnet\config\Config.yaml");
                }
                else
                {
                    return "/home/frt/5farmChiaClient";
                }
            }

        }

        public static void UpdateLogLevel()
        {
            if (!File.Exists(ConfigFilePath)) return;
            // Setup the input
            string fileText = File.ReadAllText(ConfigFilePath);
            var input = new StringReader(fileText);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Modify the stream
            var invoice = (YamlMappingNode)yaml.Documents[0].RootNode;
            invoice = ((YamlMappingNode)invoice.Children["logging"]);
            invoice.Children["log_level"] = "INFO";

            using (TextWriter textWriter = new StreamWriter(ConfigFilePath))
            {
                yaml.Save(textWriter, assignAnchors: false);
            }


        }
    }
}
