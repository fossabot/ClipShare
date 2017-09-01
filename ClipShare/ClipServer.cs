using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Lidgren.Network;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.ObjectModel;

namespace ClipShare
{
    class ClipServer
    {
        // TODO:
        // Implement a callback from the receiver to confirm operation was successful

        const string DEFAULT_SERVER = "localhost";
        const int DEFAULT_PORT = 6078;

        NetPeerConfiguration snpc;
        NetPeerConfiguration cnpc;
        NetServer server; // Receives data
        NetClient client; // Sends data

        public ObservableDictionary<string, Peer> peerDict = new ObservableDictionary<string, Peer>();
        public int peerTimeout = 15;
        public int peerRefreshRate = 5;

        System.Timers.Timer t = new System.Timers.Timer();

        Thread receiverThread;

        public object locker = new object();
        
        public void Start()
        {
            snpc = new NetPeerConfiguration("ClipShare");
            snpc.Port = DEFAULT_PORT;
            snpc.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            snpc.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);

            server = new NetServer(snpc);
            server.Start();

            cnpc = new NetPeerConfiguration("ClipShare");
            cnpc.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

            client = new NetClient(cnpc);
            client.Start();

            Task task = Task.Run(() =>
            {
                receiverThread = Thread.CurrentThread;

                while (true)
                {
                    bool netAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

                    
                    IPAddress localIP = null;
                    try
                    {
                        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                        {
                            socket.Connect("8.8.8.8", 65530); // Reliant on Google still existing
                            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                            localIP = endPoint.Address;
                        }
                    }
                    catch (Exception) {}
                    

                    if (netAvailable)
                    {
                        NetIncomingMessage msg = null;
                        while ((msg = server.ReadMessage()) != null)
                        {
                            if (msg.SenderEndPoint.Address.ToString() != localIP.ToString())
                            {

                                switch (msg.MessageType)
                                {
                                    case NetIncomingMessageType.VerboseDebugMessage:
                                    case NetIncomingMessageType.DebugMessage:
                                    case NetIncomingMessageType.WarningMessage:
                                    case NetIncomingMessageType.ErrorMessage:
                                        Console.WriteLine(msg.ReadString());
                                        break;
                                    case NetIncomingMessageType.DiscoveryRequest:
                                        NetOutgoingMessage response = server.CreateMessage();
                                        response.Write(Environment.MachineName);
                                        server.SendDiscoveryResponse(response, msg.SenderEndPoint);
                                        break;
                                    case NetIncomingMessageType.DiscoveryResponse:
                                        ProcessDiscovResp(msg.SenderEndPoint, msg.ReadString());
                                        break;
                                    case NetIncomingMessageType.UnconnectedData:
                                        ProcessData(msg.Data);
                                        break;
                                    default:
                                        Console.WriteLine("Unhandled type: " + msg.MessageType);
                                        break;
                                }
                            }
                        }

                        while ((msg = client.ReadMessage()) != null)
                        {
                            if (msg.SenderEndPoint.Address.ToString() != localIP.ToString())
                            {

                                switch (msg.MessageType)
                                {
                                    case NetIncomingMessageType.VerboseDebugMessage:
                                    case NetIncomingMessageType.DebugMessage:
                                    case NetIncomingMessageType.WarningMessage:
                                    case NetIncomingMessageType.ErrorMessage:
                                        Console.WriteLine(msg.ReadString());
                                        break;
                                    case NetIncomingMessageType.DiscoveryRequest:
                                        NetOutgoingMessage response = server.CreateMessage();
                                        response.Write(Environment.MachineName);
                                        server.SendDiscoveryResponse(response, msg.SenderEndPoint);
                                        break;
                                    case NetIncomingMessageType.DiscoveryResponse:
                                        ProcessDiscovResp(msg.SenderEndPoint, msg.ReadString());
                                        break;
                                    case NetIncomingMessageType.UnconnectedData:
                                        ProcessData(msg.Data);
                                        break;
                                    default:
                                        Console.WriteLine("Unhandled type: " + msg.MessageType);
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1); // Sleep to avoid excessive CPU usage
                    }
                }
            });

            t.Elapsed += T_Elapsed;
            t.Interval = peerRefreshRate * 1000;
            t.Start();
        }

        public void Stop()
        {
            t.Stop();
            receiverThread.Abort();
            server.Shutdown("");
            client.Shutdown("");
        }

        private void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CleanPeers();
            RefreshPeers();
        }

        [STAThread]
        private void ProcessData(byte[] data)
        {
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            s.Write(data, 0, data.Length);
            s.Position = 0;
            var cd = bf.Deserialize(s) as ClipData; // Should be ClipData, typecast to confirm

            if (cd == null) return; // Clearly not receiving ClipData, return

            cd.ToClipboard();
        }

        private void ProcessDiscovResp(IPEndPoint endpoint, string machineName)
        {
            lock(locker)
            {
                if (peerDict.ContainsKey(machineName))
                {
                    peerDict[machineName] = new Peer(machineName, endpoint);
                }
                else
                {
                    peerDict.Add(machineName, new Peer(machineName, endpoint));
                }
            }
        }

        public void Send(IPEndPoint recipient, byte[] data)
        {
            var msg = client.CreateMessage();
            msg.Write(data);
            client.SendUnconnectedMessage(msg, recipient);
        }

        private void CleanPeers()
        {
            lock(locker)
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

                foreach(var entry in rList)
                {
                    peerDict.Remove(entry);
                }
            }
        }

        private void RefreshPeers()
        {
            client.DiscoverLocalPeers(DEFAULT_PORT);
        }
    }
}
