using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Arke;

namespace ClipShare
{
    public partial class Form1 : Form // Using port 6078
    {
        ClipServer server;
        
        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; // Just using a form app for the controls
            this.Hide();

            server = new ClipServer();
            server.peerDict.CollectionChanged += PeerDict_CollectionChanged;
            server.Start();

            var clip = Clipboard.GetDataObject();
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            var cd = new ClipData();
            cd.FromClipboard();
            bf.Serialize(s, cd);
            
            ContextMenu_SendTo.DropDownItemClicked += ContextMenu_SendTo_DropDownItemClicked;
        }
        
        private void PeerDict_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (server.locker)
            {
                var placeholder = new ToolStripMenuItem()
                {
                    Name = "ContextMenu_SendTo_NoPeersFound",
                    Text = "No peers found...",
                    Enabled = false,
                    Size = new Size(160, 22)
                };

                ToolStripDropDown newdd = new ToolStripDropDown();
                
                if (server.peerDict.Count == 0)
                {
                    newdd.Items.Add(placeholder);
                }
                else
                {
                    foreach (var item in server.peerDict)
                    {
                        newdd.Items.Add(new ToolStripMenuItem
                        {
                            Name = "ContextMenu_SendTo_" + item.Key,
                            Text = item.Key,
                            Size = new Size(160, 22)
                        });
                    }
                }

                if (InvokeRequired && ContextMenu_SendTo.DropDown.Items != newdd.Items)
                {
                    this.Invoke(new MethodInvoker(delegate
                    {
                        ContextMenu_SendTo.DropDownItems.Clear();
                        ContextMenu_SendTo.DropDown = newdd;
                    }));
                }
                
            }
        }

        private void ContextMenu_SendTo_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var clip = Clipboard.GetDataObject();
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            var cd = new ClipData();
            cd.FromClipboard();
            bf.Serialize(s, cd);

            lock (server.locker)
            {
                notifyIcon.ShowBalloonTip(3000, "Sending clipboard...", "Sending clipboard data to " + e.ClickedItem.Text, ToolTipIcon.None);
                server.Send(server.peerDict[e.ClickedItem.Text].endpoint, s.ToArray());
            }
        }

        private void ContextMenu_Exit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    class ClipServer
    {
        // TODO:
        // Implement a callback from the receiver to confirm operation was successful

        const string DEFAULT_SERVER = "localhost";
        const int DEFAULT_PORT = 6078;
        
        ArkeTcpServer server; // Receives data
        ArkeTcpClient client; // Sends data

        public ObservableDictionary<string, Peer> peerDict = new ObservableDictionary<string, Peer>();
        public int peerTimeout = 15;
        public int peerRefreshRate = 5;

        System.Timers.Timer t = new System.Timers.Timer();

        Thread receiverThread;

        public object locker = new object();
        
        public void Start()
        {
            client = new ArkeTcpClient();
            
            server = new ArkeTcpServer(DEFAULT_PORT);
            server.MessageReceived += Server_MessageReceived;
            server.StartListening();

            t.Elapsed += T_Elapsed;
            t.Interval = peerRefreshRate * 1000;
            t.Start();
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
                var peer = data as Peer;
                lock (locker)
                {
                    if (peerDict.ContainsKey(peer.machineName))
                    {
                        peerDict[peer.machineName] = peer;
                    }
                    else
                    {
                        peerDict.Add(peer.machineName, peer);
                    }
                }
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
            ArkeMessage msg = new ArkeMessage(data);
            client.Connect(recipient.Address, 1000);
            client.Send(msg);
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
            // TODO: Poll all IPs with self-configured Peer class
            // Run as async tasks with callbacks to add Peer response?
            // ^ Too much resource usage?
        }
    }

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

    [Serializable]
    class ClipData
    {
        string machineName = Environment.MachineName; // Issues with multiple data types in a message, better off incorporating it into a single serialisable class

        Stream stream;
        System.Collections.Specialized.StringCollection stringCollection;
        Image image;
        String text;

        public void FromClipboard()
        {
            if (Clipboard.ContainsAudio())
            {
                stream = Clipboard.GetAudioStream();
            }
            else if (Clipboard.ContainsFileDropList())
            {
                stringCollection = Clipboard.GetFileDropList();
            }
            else if (Clipboard.ContainsImage())
            {
                image = Clipboard.GetImage();
            }
            else if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
            }
        }
        
        public void ToClipboard()
        {
            if (stream != null)
            {
                Thread thread = new Thread(() => Clipboard.SetAudio(stream));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            else if (stringCollection != null)
            {
                Thread thread = new Thread(() => Clipboard.SetFileDropList(stringCollection));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            else if (image != null)
            {
                Thread thread = new Thread(() => Clipboard.SetImage(image));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
            else if (text != null)
            {
                Thread thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }
    }
}
