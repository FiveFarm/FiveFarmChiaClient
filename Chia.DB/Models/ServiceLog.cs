using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.DB.Models
{
    public class ServiceLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime LogTime { get; set; }
        public DateTime ViewTime { get; set; }
        public string LogString { get; set; }
        public bool IsProcessed { get; set; }
    }
}
