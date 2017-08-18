using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClipShare
{
    [Serializable]
    class Peer
    {
        public string machineName;
        public IPEndPoint endpoint;
        public DateTime lastSeen;

        public Peer(string machineName, IPEndPoint endpoint)
        {
            this.machineName = machineName;
            this.endpoint = endpoint;
            lastSeen = DateTime.Now;
        }
    }
}
