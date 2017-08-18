using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipShare
{
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
