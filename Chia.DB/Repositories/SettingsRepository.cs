using Chia.DB.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chia.DB.Repositories
{
    public class SettingsRepository
    {
        public string GetValue(string key)
        {
            using (DBContext db = new DBContext())
            {
                var record = db.SettingsFromDB.Where(x => x.Key.Equals(key)).FirstOrDefault();
                if (record is not null)
                {
                    db.Entry(record).Reload();
                    return record.Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public void SetValue(string key, string value)
        {
            using (DBContext _db = new DBContext())
            {
                var record = _db.SettingsFromDB.Where(x => x.Key.Equals(key)).FirstOrDefault();
                if (record is not null)
                {
                    record.Value = value;
                }
                else
                {
                    _db.SettingsFromDB.Add(new MiscSettings { Key = key, Value = value });
                }
                _db.SaveChanges();
                Thread.Sleep(500);
            }
        }

    }
}
