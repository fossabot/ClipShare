using System;
using System.Net;

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
