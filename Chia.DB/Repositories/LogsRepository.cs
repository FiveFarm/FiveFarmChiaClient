using Chia.DB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.DB.Repositories
{
    public class LogsRepository
    {
        DBContext _db;
        public LogsRepository(DBContext db)
        {
            _db = db;
        }
        public List<ServiceLog> GetLogs()
        {
            return _db.Logs.Where(x=>x.IsProcessed==false).Take(100).ToList<ServiceLog>();
        }

        public void AddLog(string logMessage)
        {
            ServiceLog log = new ServiceLog() { LogString = logMessage, LogTime = DateTime.Now };
            _db.Logs.Add(log);
            _db.SaveChanges();
        }

        public void DeletePreviousLogs(int lastId)
        {
            var items = _db.Logs.Where(item => item.Id <= lastId);
            //foreach (var item in items)
                //_db.Logs.DeleteObject(item);
            //_db.Logs.
        }

        public void Update()
        {
            _db.SaveChanges();
        }
    }
}
