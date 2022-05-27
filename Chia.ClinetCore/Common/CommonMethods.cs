using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.ClientCore.Common
{
    public class CommonMethods
    {
        public static bool ISValidResponse(string jsonData)
        {
            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(jsonData);
            return jsonObject.Value<Boolean>("success");
        }

        public static string GetInnerData(string jsonData, string key)
        {
            if (!ISValidResponse(jsonData)) return "\"\"";

            if (string.IsNullOrEmpty(key)) return jsonData;

            JObject jsonObject = (JObject)JsonConvert.DeserializeObject(jsonData);
            return jsonObject[key].ToString();
        }

        public static string GetPasswordInput()
        {
            string pass = string.Empty;
            ConsoleKey key;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                try
                {
                    if (key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        Console.Write("\b \b");
                        pass = pass[0..^1];
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        Console.Write("*");
                        pass += keyInfo.KeyChar;
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }
            } while (key != ConsoleKey.Enter);

            //ConsoleKeyInfo key;
            //do
            //{
            //    key = Console.ReadKey(true);

            //    // Backspace Should Not Work
            //    if (key.Key != ConsoleKey.Backspace)
            //    {
            //        if (key.Key != ConsoleKey.Enter)
            //        {
            //            pass += key.KeyChar;
            //            Console.Write("*");
            //        }
            //    }
            //    else
            //    {
            //        Console.Write("\b");
            //    }
            //}
            //while (key.Key != ConsoleKey.Enter);  // Stops Receving Keys Once Enter is Pressed
            Console.WriteLine();
            return pass;
        }
    }
}
