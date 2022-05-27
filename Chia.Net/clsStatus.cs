using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.Net
{
    public class clsStatus
    {
        static DB.Repositories.LogsRepository _logsRepo;
        public clsStatus(DB.Repositories.LogsRepository logsRepo)
        {
            _logsRepo = logsRepo;
        }

        public static Action<string> LogMessageAction;
        public static void AddLogMesssage(string msg)
        {
            if (LogMessageAction != null)
            {
                LogMessageAction(msg);
            }
            else
            {
                LogMesssage2 = (msg);
            }
        }

        static string LogMesssage2
        {
            set
            {
                try
                {
                    _logsRepo.AddLog($" {value}");
                }
                catch (Exception ex) { }
            }
        }
    }
}
