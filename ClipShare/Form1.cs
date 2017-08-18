using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Specialized;
using System.Threading.Tasks;

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
            var t = new Task(() =>
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
            });
            Locker.LockedExec(server.locker, t, ClipServer.LOCK_TIMEOUT);
        }

        private void ContextMenu_SendTo_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var clip = Clipboard.GetDataObject();
            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            var cd = new ClipData();
            cd.FromClipboard();
            bf.Serialize(s, cd);

            var t = new Task(() =>
            {
                notifyIcon.ShowBalloonTip(3000, "Sending clipboard...", "Sending clipboard data to " + e.ClickedItem.Text, ToolTipIcon.None);
                server.Send(server.peerDict[e.ClickedItem.Text].endpoint, s.ToArray());
            });
            Locker.LockedExec(server.locker, t, ClipServer.LOCK_TIMEOUT);
        }

        private void ContextMenu_Exit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
