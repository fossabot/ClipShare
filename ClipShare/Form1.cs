using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace ClipShare
{
    public partial class Form1 : Form // Using port 6078
    {
        ClipServer server;
        Timer timer = new Timer();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; // Just using a form app for the controls
            server = new ClipServer();
            server.Startup();
            server.Listen();

            timer.Interval = 1000;
            timer.Tick += UpdateClipboard;
            timer.Start();
        }

        private void ContextMenu_Exit_Click(object sender, EventArgs e)
        {
            timer.Stop();
            Close();
        }

        private void UpdateClipboard(object sender, EventArgs e)
        {
            while (server.serverSocket.Available > 0)
            {
                Tuple<object,int> dataTuple = server.ReceiveData();

                switch (dataTuple.Item2)
                {
                    case 0: Clipboard.SetData(DataFormats.WaveAudio, dataTuple.Item1); break;
                    case 1: Clipboard.SetData(DataFormats.Bitmap, dataTuple.Item1); break;
                    case 2: Clipboard.SetData(DataFormats.Rtf, dataTuple.Item1); break;
                    default: break;
                }
            }
        }

        private void UpdateClientList(object sender, EventArgs e)
        {
            // TODO: Implement a UDP receiver to listen for client broadcasts
        }

        private void Broadcast()
        {
            
        }
    }

    public class ClipServer
    {
        const string DEFAULT_SERVER = "localhost";
        const int DEFAULT_PORT = 6078;
        const int BRDCAST_PORT = 6079;

        // Server socket stuff 
        public System.Net.Sockets.Socket serverSocket;

        // Client socket stuff 
        System.Net.Sockets.Socket clientSocket;

        // Broadcast server stuff
        UdpClient udpClient;

        public string Startup()
        {
            // The chat server always starts up on the localhost, using the default port 
            IPHostEntry hostInfo = Dns.GetHostEntry(DEFAULT_SERVER);
            IPAddress serverAddr = hostInfo.AddressList[0];
            var serverEndPoint = new IPEndPoint(serverAddr, DEFAULT_PORT);
            var broadcastEndPoint = new IPEndPoint(serverAddr, BRDCAST_PORT);

            // Create a listener socket and bind it to the endpoint 
            serverSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            serverSocket.Bind(serverEndPoint);

            // Do the same with the broadcast server
            udpClient = new UdpClient(DEFAULT_SERVER, BRDCAST_PORT);

            return serverSocket.LocalEndPoint.ToString();
        }

        public string Listen()
        {
            int backlog = 0;
            try
            {
                serverSocket.Listen(backlog);
                return "Server listening";
            }
            catch (Exception ex)
            {
                return "Failed to listen" + ex.ToString();
            }
        }

        public bool SendData(object data, int dataType, IPAddress remoteHost)
        {
            if (data == null)
            {
                return false;
            }

            // The chat client always starts up on the localhost, using the default port 
            var remoteEndPoint = new IPEndPoint(remoteHost, DEFAULT_PORT);

            // Create a client socket and connect it to the endpoint 
            clientSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            clientSocket.Connect(remoteEndPoint);

            var ms = new MemoryStream();
            var f = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            f.Serialize(ms, data);
            byte[] dataBytesBuffer = ms.ToArray();
            byte[] bufferLength = BitConverter.GetBytes(dataBytesBuffer.Length);
            byte[] dataTypeFlag = BitConverter.GetBytes(dataType); // 0: Audio, 1: Image, 2: Text
            var dataBytes = bufferLength.Concat(dataTypeFlag).Concat(dataBytesBuffer);

            clientSocket.Send(dataBytes.ToArray());
            clientSocket.Close();

            return true;
        }

        public Tuple<object, int> ReceiveData()
        {
            System.Net.Sockets.Socket receiveSocket;

            receiveSocket = serverSocket.Accept();

            byte[] buffer = new byte[4];
            receiveSocket.Receive(buffer);
            var messageLen = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[1];
            receiveSocket.Receive(buffer);
            var dataType = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[messageLen];
            receiveSocket.Receive(buffer);

            receiveSocket.Close();

            var f = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var data = f.Deserialize(new MemoryStream(buffer));

            return new Tuple<object,int>(data, dataType);
        }
    }
}
