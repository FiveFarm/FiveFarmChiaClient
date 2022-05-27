using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.Common
{
    public class ChiaTaskModel
    {
        public string nodeId { get; set; }
        public string description { get; set; }
        public string taskId { get; set; }
        public JObject data { get; set; }

        public string type { get; set; }
        public string status { get; set; }
        public string userId { get; set; }
        //userId

        public override string ToString()
        {

            string response =
                //$"\"action\":\"onNodeMessage\"," +
                $"{{" +
                $"\"action\":\"onTaskUpdate\"," +
                $"\"taskId\":\"{taskId}\"," +
                $"\"userId\":\"{userId}\"," +
                $"\"status\":\"{status}\","+
                $"\"description\":\"{description}\"," +
                $"\"responseData\":{data}" +
                $"}}";

            return response;
        }
    }
}
