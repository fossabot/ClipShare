using Arke;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace ClipShare
{
    class ClipServer
    {
        // TODO: Implement a callback from the receiver to confirm operation was successful
        // TODO: Rework locks into something that won't cause a deadlock on dictionary update events
        // TODO: Move locker and make it public

        const string DEFAULT_SERVER = "localhost";
        const int DEFAULT_PORT = 6078;
        public const int LOCK_TIMEOUT = 10; // To public or not to public? That is the question.

        ArkeTcpServer server; // Receives data
        ArkeTcpClient client; // Sends data
        UdpClient uclient;

        public ObservableDictionary<string, Peer> peerDict = new ObservableDictionary<string, Peer>();
        public int peerTimeout = 15;
        public int peerRefreshRate = 5;

        System.Timers.Timer t = new System.Timers.Timer();

        CancellationToken UDPCancellationToken;

        public object locker = new object();

        public void Start()
        {
            client = new ArkeTcpClient();

            server = new ArkeTcpServer(DEFAULT_PORT);
            server.MessageReceived += Server_MessageReceived;
            server.StartListening();

            uclient = new UdpClient(DEFAULT_PORT);
            uclient.EnableBroadcast = true;
            UDPCancellationToken = new CancellationToken(false);
            ReceiveUDPAsync(UDPCancellationToken);

            t.Elapsed += T_Elapsed;
            t.Interval = peerRefreshRate * 1000;
            t.Start();
        }

        public async Task ReceiveUDPAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var message = await uclient.ReceiveAsync();

                var bf = new BinaryFormatter();
                var s = new MemoryStream();
                s.Write(message.Buffer, 0, message.Buffer.Length);
                s.Position = 0;
                var data = bf.Deserialize(s);
                s.SetLength(0);

                if (data is Peer)
                {
                    var peer = data as Peer;
                    if (peer.machineName != Environment.MachineName)
                    {
                        peer.endpoint = message.RemoteEndPoint;
                        ProcessPeer(peer);
                    }
                }
            }
        }

        private void Server_MessageReceived(ArkeMessage message, ArkeTcpServerConnection connection)
        {
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(message.GetContentAsBytes(), 0, message.GetContentAsBytes().Length);
            s.Position = 0;
            var data = bf.Deserialize(s);
            s.SetLength(0);

            if (data is ClipData) // Receive ClipData
            {
                (data as ClipData).ToClipboard();
            }
            else if (data is Peer) // Receive discovery
            {
                ProcessPeer(data as Peer);
            }
            else if (data is string) // Receive error message
            {
                Console.WriteLine(data as string);
            }
            else // Send error message
            {
                bf.Serialize(s, "Unsupported data type");
                connection.Send(new ArkeMessage(s.ToArray()));
            }

            s.Dispose();
        }

        private void ProcessPeer(Peer peer)
        {
            var t = new Task(() =>
            {
                if (peerDict.ContainsKey(peer.machineName))
                {
                    peerDict[peer.machineName] = peer;
                }
                else
                {
                    peerDict.Add(peer.machineName, peer);
                }
            });
            Locker.LockedExec(locker, t, LOCK_TIMEOUT);
        }

        public void Stop()
        {
            t.Stop();
            server.StopListening();
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CleanPeers();
            RefreshPeers();
        }

        public void Send(IPEndPoint recipient, byte[] data)
        {
            ArkeMessage msg = new ArkeMessage(data);
            client.Connect(recipient.Address, 1000);
            client.Send(msg);
        }

        public void Send(IPEndPoint recipient, object data)
        {
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            bf.Serialize(s, data);

            ArkeMessage msg = new ArkeMessage(s.ToArray());
            ArkeTcpClient ac = new ArkeTcpClient();

            try
            {
                ac.Connect(recipient.Address, 1000);
                ac.Send(msg);
            }
            catch (Exception e) { }
        }

        private void CleanPeers()
        {
            Task t = new Task(() =>
            {
                var rList = new List<string>();

                foreach (var entry in peerDict)
                {
                    var diff = DateTime.Now.Subtract(entry.Value.lastSeen);
                    if (diff.Seconds > peerTimeout)
                    {
                        rList.Add(entry.Key);
                    }
                }

                foreach (var entry in rList)
                {
                    peerDict.Remove(entry);
                }
            });
            Locker.LockedExec(locker, t, LOCK_TIMEOUT);
        }

        private void RefreshPeers()
        {
            var localPeer = new Peer(Environment.MachineName, new IPEndPoint(server.Address, DEFAULT_PORT));

            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            bf.Serialize(s, localPeer);

            uclient.Send(s.ToArray(), s.ToArray().Length, new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT));
        }
    }
}
