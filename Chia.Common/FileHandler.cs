using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.Common
{
    public class FileHandler
    {
        public static void SaveFile(string path,string content)
        {
            FileInfo file = new FileInfo(path);
            file.Directory.Create();
            File.WriteAllText(file.FullName, content);
        }      
        public static void SaveFile_Log(string path,string content)
        {
            try
            {
                using (StreamWriter wr = new StreamWriter(path, true))
                {
                    wr.WriteLine(content);
                    wr.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save log in file");
            }
        }

        public static string ReadFile(string path)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
            return string.Empty;
        }


    }
}
