using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chia.ClientCore.Models
{
    public class ConnectedNode
    {
        public string NodeId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public bool Status { get; set; }
    }

    public class FullNodeConnections : ConnectedNode
    {

    }
    public class FarmerConnections : ConnectedNode
    {

    }
}
