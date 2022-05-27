using DeviceId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chia.Common
{
    public class NodeIdentification
    {
        public static string GetHostName()
        {
            return Environment.MachineName;
        }

        public static string GetClientId(string fingerPrint,string userId)
        {
            return $"{NodeIdentification.GetMotherBoardSerial()}-{fingerPrint}-{userId}";
        }

        static string GetMotherBoardSerial()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Product, SerialNumber FROM Win32_BaseBoard");
                // Executing the query...  
                // Because the machine has a single Motherboard, then a single object (row) returned.  
                ManagementObjectCollection information = searcher.Get();
                foreach (ManagementObject obj in information)
                {
                    var serial = obj["SerialNumber"];
                    return ComputeHash(serial.ToString());
                }
                // For the typical use of disposable objects enclose it in a using statement instead.  
                searcher.Dispose();
                return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        static string ComputeHash(string sSourceData)
        {
            byte[] tmpSource;
            byte[] tmpHash;
            //Create a byte array from source data
            tmpSource = ASCIIEncoding.ASCII.GetBytes(sSourceData);

            //Compute hash based on source data
            tmpHash = new MD5CryptoServiceProvider().ComputeHash(tmpSource);
            string hashString = ByteArrayToString(tmpHash);
            return hashString;
        }
        static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
    }
}
