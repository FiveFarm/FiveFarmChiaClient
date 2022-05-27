using Chia.Common;
using Chia.DB.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.DB
{
    public class DBContext : DbContext
    {
        public DbSet<ServiceLog> Logs { get; set; }
        public DbSet<MiscSettings> SettingsFromDB { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                CommonConstants.SaveDebugLog($"OnConfiguring",false, true);
                string dbPath = CommonConstants.DataFolder;

                if (!Directory.Exists(dbPath))
                {
                    CommonConstants.SaveDebugLog($"OnConfiguring: Creating folder: ",false, true);
                    Directory.CreateDirectory(dbPath);
                }
                string dbName = "chia.db";
                string path = Path.Combine(dbPath, dbName);
                optionsBuilder.UseSqlite($"DataSource={path}");
                //optionsBuilder.UseLazyLoadingProxies();
                CommonConstants.SaveDebugLog($"OnConfiguring: UseSqlite",false, true);
                base.OnConfiguring(optionsBuilder);
                CommonConstants.SaveDebugLog($"OnConfiguring: base called",false, true);
            }
            catch (Exception ex)
            {
                CommonConstants.SaveDebugLog($"Exception in OnConfiguring: {ex.Message}",false, true);
                throw;
            }
        }
    }
}
