using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Environment;

namespace Chia.Net.LogExplorer
{
    public class LogFileExplorer
    {
        private static List<string> InitLogLines()
        {
            string pathtoFile;
            string userFolder = GetFolderPath(SpecialFolder.UserProfile);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {

                pathtoFile = @$"{userFolder}\.chia\mainnet\log\debug.log";
            }
            else
            {
                pathtoFile = @$"{userFolder}/.chia/mainnet/log/debug.log";
            }
            //string userFolder = GetFolderPath(SpecialFolder.UserProfile);
            string[] linesarray = File.ReadAllLines(pathtoFile);
            List<string> fileLines = new List<string>(linesarray);
            return fileLines;

        }
        /*
        public static string GetLastHeight(List<string> fileLines)
        {
            string startOfHeightLine = "Updated wallet peak to height ";
            string heightLine = fileLines.Where((x) => x.Contains(startOfHeightLine)).LastOrDefault();
            string height = string.Empty;

            if (!string.IsNullOrEmpty(heightLine) && heightLine.Contains(","))
            {
                int startIndex = heightLine.IndexOf(startOfHeightLine) + startOfHeightLine.Length;
                int length = heightLine.IndexOf(",") - startIndex;
                height = heightLine.Substring(startIndex,length );
            }

            return height;
        }
        */
        public static JArray GetLatestAttemptedProofs(List<string> fileLines, JArray challenges, ref Common.CommonConstants.ErrorCodes errCode_LAP)
        {
            string firstStringToCheck = "harvester chia.harvester.harvester: INFO     ";
            string secondStringToCheck = "plots were eligible for farming ";

            List<string> validLines = fileLines.Where((x) => x.Contains(firstStringToCheck) && x.Contains(secondStringToCheck)).TakeLast(10).ToList();
            Common.CommonConstants.SaveDebugLog($"GetLatestAttemptedProofs: valid lines Length:{validLines.Count()}", false, true);

            if (errCode_LAP == Common.CommonConstants.ErrorCodes.NONE && (validLines == null || validLines.Count == 0))
                errCode_LAP = Common.CommonConstants.ErrorCodes.LogFile_Missing_RequiredData;

            JArray finalString = new JArray();
            JObject proofObject;

            foreach (var validLine in validLines)
            {
                proofObject = ParseString(validLine, challenges);

                if (proofObject != null)
                {
                    finalString.Add(proofObject);
                }
            }

            return finalString;
        }
        private static JObject ParseString(string staticString, JArray challenges)
        {
            string elligiblePlotsPriorString = "harvester chia.harvester.harvester: INFO     ";
            string elligiblePlotsPostString = "plots were eligible for farming ";
            string challengesCountPostString = "Found ";
            string totalPriorStrin = "Total ";
            string time = "";
            string elligibalPlots = "";
            string totalPlots = "";
            string proofsFound = "";
            string challange = "";
            string space = " ";
            int startIndex = 0;

            DateTime timeValue = DateTime.MinValue;
            int elligibalPlotsValue = 0;
            int proofsValue = 0;
            int totalPlotsValue = 0;


            if (staticString.Contains(space))
            {
                time = staticString.Substring(0, staticString.IndexOf(space));
                if (!DateTime.TryParse(time, out timeValue)) time = "";
            }

            if (staticString.Contains(elligiblePlotsPriorString))
            {
                startIndex = staticString.IndexOf(elligiblePlotsPriorString) + elligiblePlotsPriorString.Length;
                staticString = staticString.Substring(startIndex);
                elligibalPlots = staticString.Substring(0, staticString.IndexOf(space));
                if (!int.TryParse(elligibalPlots, out elligibalPlotsValue)) elligibalPlots = "";
            }

            if (staticString.Contains(elligiblePlotsPostString))
            {
                startIndex = staticString.IndexOf(elligiblePlotsPostString) + elligiblePlotsPostString.Length;
                staticString = staticString.Substring(startIndex);
                challange = staticString.Substring(0, staticString.IndexOf("..."));
            }

            if (staticString.Contains(challengesCountPostString))
            {
                startIndex = staticString.IndexOf(challengesCountPostString) + challengesCountPostString.Length;
                staticString = staticString.Substring(startIndex);
                proofsFound = staticString.Substring(0, staticString.IndexOf(" "));
                if (!int.TryParse(proofsFound, out proofsValue)) proofsFound = "";
            }

            if (staticString.Contains(totalPriorStrin))
            {
                startIndex = staticString.IndexOf(totalPriorStrin) + totalPriorStrin.Length;
                staticString = staticString.Substring(startIndex);
                totalPlots = staticString.Substring(0, staticString.IndexOf(" "));
                if (!int.TryParse(totalPlots, out totalPlotsValue)) totalPlots = "";
            }
            JObject record = null;
            if (challange != "" && time != "" && totalPlots != "" && elligibalPlots != "" && proofsFound != "")
            {
                record = new JObject();
                record.Add("challenge_hash", GetChallengeHash(challange, challenges));
                record.Add("total_plots", totalPlotsValue);
                record.Add("elligibal_plots", elligibalPlotsValue);
                record.Add("proofs_found", proofsValue);
                record.Add("time", time);
            }
            return record;
        }

        public static JArray GetLatestFarmedBlocks(List<string> fileLines, JArray challenges, ref Common.CommonConstants.ErrorCodes errCode_LAP)
        {
            string firstStringBlockFound = "Farmed unfinished_block ";
            string secondStringBlockFound = ", cost:";

            List<string> validLines = fileLines.Where((x) => x.Contains(firstStringBlockFound) && x.Contains(secondStringBlockFound)).TakeLast(10).ToList();
            Common.CommonConstants.SaveDebugLog($"GetLatestFarmedBlocks: ValidLines Length:{validLines.Count()}", false, true);

            JArray finalString = new JArray();
            JObject blockObject;

            foreach (var validLine in validLines)
            {
                blockObject = ParseString_Block(validLine, challenges);

                if (blockObject != null)
                {
                    finalString.Add(blockObject);
                }
            }

            return finalString;
        }
        private static JObject ParseString_Block(string staticString, JArray challenges)
        {
            string challengePriorString = "Farmed unfinished_block ";
            string spPriorString = "SP: ";
            string validationTimePriorString = ", validation time: ";
            string costPriorString = ", cost: ";

            string time = "";
            string challange = "";
            string sp = "";
            string validationTime = "";
            string cost = "";
            string space = " ";
            int startIndex = 0;

            DateTime timeValue = DateTime.MinValue;
            int spValue = 0;
            int proofsValue = 0;
            int totalPlotsValue = 0;


            if (staticString.Contains(space))
            {
                time = staticString.Substring(0, staticString.IndexOf(space));
                if (!DateTime.TryParse(time, out timeValue)) time = "";
            }

            if (staticString.Contains(challengePriorString))
            {
                startIndex = staticString.IndexOf(challengePriorString) + challengePriorString.Length;
                staticString = staticString.Substring(startIndex);
                challange = staticString.Substring(0, staticString.IndexOf(spPriorString));
            }

            if (staticString.Contains(spPriorString))
            {
                startIndex = staticString.IndexOf(spPriorString) + spPriorString.Length;
                staticString = staticString.Substring(startIndex);
                sp = staticString.Substring(0, staticString.IndexOf(validationTimePriorString));
                if (!int.TryParse(sp, out spValue)) sp = "";
            }

            if (staticString.Contains(validationTimePriorString))
            {
                startIndex = staticString.IndexOf(validationTimePriorString) + validationTimePriorString.Length;
                staticString = staticString.Substring(startIndex);
                validationTime = staticString.Substring(0, staticString.IndexOf(costPriorString));
            }

            if (staticString.Contains(costPriorString))
            {
                startIndex = staticString.IndexOf(costPriorString) + costPriorString.Length;
                staticString = staticString.Substring(startIndex);
                cost = staticString.Trim();
            }

            JObject record = null;
            if (challange != "" && time != "" && sp != "" && cost != "")
            {
                record = new JObject();
                record.Add("challenge_hash", GetChallengeHash(challange, challenges));
                record.Add("time", time);
                record.Add("sp", totalPlotsValue);
                record.Add("validation_time", validationTime);
                record.Add("cost", proofsValue);
            }
            return record;
        }

        private static string GetChallengeHash(string challenge, JArray fullChallengHashes)
        {
            var challengHashFound = fullChallengHashes.Where(x => x["Hash"].ToString().StartsWith($"0x{challenge}")).FirstOrDefault();

            if (challengHashFound == null) return challenge;

            return challengHashFound["Hash"].ToString();
        }

        static DateTime LastLogTime = new DateTime();
        public static JArray GetPoolErrorWarnings(List<string> fileLines)
        {
            List<string> strsToSearch = new List<string>();
            strsToSearch.Add("WARNING  No pool specific difficulty has been set for");
            strsToSearch.Add("No pool specific authentication_token_timeout has been set for");
            strsToSearch.Add("ERROR    Error in pooling");
            strsToSearch.Add("WARNING  GET /farmer response:");
            strsToSearch.Add("WARNING  POST /farmer response:");
            strsToSearch.Add("ERROR Exception in GET /pool_info");
            strsToSearch.Add("harvester chia.harvester.harvester: WARNING Not farming any plots on this harvester");
            strsToSearch.Add("farmer chia.farmer.farmer         : ERROR    Harvester ");
            strsToSearch.Add(": INFO     Websocket exception. Closing websocket with chia_harvester code =");

            List<string> ErrorWarningLines = new List<string>();

            foreach (var str in strsToSearch)
            {
                List<string> validLines = fileLines.Where((x) => x.Contains(str)).TakeLast(2).ToList();
                ErrorWarningLines.AddRange(validLines);
            }
            Common.CommonConstants.SaveDebugLog($"GetPoolErrorWarnings: ErrorWarningLines Length:{ErrorWarningLines.Count()}", false, true);

            JArray finalString = new JArray();
            JObject blockObject;

            foreach (var validLine in ErrorWarningLines)
            {
                blockObject = ParseString_PoolError(validLine, ref strsToSearch);

                if (blockObject != null)
                {
                    finalString.Add(blockObject);
                }
            }

            return finalString;
        }

        private static JObject ParseString_PoolError(string staticString, ref List<string> ErrorWarningLines)
        {
            //2021-06-27T09:05:05.145 full_node chia.full_node.full_node: INFO :four_leaf_clover: Farmed unfinished_block bc07be0c9c13ded2ccc7a9849614ae4692bd7f945362730fe57805319558fb83, SP: 17, 
            //validation time: 0.057682037353515625, cost: 172875678

            string time = "";
            string msg = "";
            string space = " ";

            DateTime timeValue = DateTime.MinValue;

            if (staticString.Contains(space))
            {
                time = staticString.Substring(0, staticString.IndexOf(space));
                if (!DateTime.TryParse(time, out timeValue)) time = "";
            }

            msg = staticString.Substring(staticString.IndexOf(space));

            JObject record = null;
            DateTime OnePoint5Days = DateTime.Now.AddHours(-12);

            OnePoint5Days = new DateTime(OnePoint5Days.Year, OnePoint5Days.Month, OnePoint5Days.Day, 0, 0, 0);

            if (!string.IsNullOrEmpty(time) && timeValue != DateTime.MinValue)
            {
                //If Already reported log found then skip it //Compare with Time
                if (timeValue >= OnePoint5Days && timeValue > LastLogTime) //Only "Todays + 12 hours of Prev day" Selected Error/Warnings will be reported
                {
                    if (msg != "" && time != "")
                    {
                        record = new JObject();
                        record.Add("time", time);
                        record.Add("message", msg);
                        LastLogTime = timeValue;
                    }
                }
            }
            return record;
        }

        public static (JArray, JArray, JArray) GetDataFromLog(JArray challenges, ref Common.CommonConstants.ErrorCodes errCode_LAP)
        {
            List<string> fileLines = InitLogLines();

            JArray attemptedProofs = GetLatestAttemptedProofs(fileLines, challenges, ref errCode_LAP);
            JArray farmed_unfinished_blocks = GetLatestFarmedBlocks(fileLines, challenges, ref errCode_LAP);
            JArray poolErrorWarnings = GetPoolErrorWarnings(fileLines);
            //string height = GetLastHeight(fileLines);

            return (attemptedProofs, farmed_unfinished_blocks, poolErrorWarnings);

        }
    }
}
