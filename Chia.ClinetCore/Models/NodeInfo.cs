using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chia.ClientCore.Models
{
    public class NodeInfo
    {
        //JsonProperty
        public string ClinetId { get; set; } = "987";
        public string ClinetName { get; set; } = "5farm";
        public string UserId { get; set; } = "789";
        public string ClinetType { get; set; } = "5Farm";
        public override string ToString()
        {
            string response = $"{{\"userId\":\"{UserId}\",\"clientId\":\"{ClinetId}\",\"clientName\":\"{ClinetName}\",\"clientType\":\"{ClinetType}\"}}";
            return response;
        }
    }
}
