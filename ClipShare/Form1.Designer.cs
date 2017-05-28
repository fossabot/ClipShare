namespace ClipShare
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ContextMenu_SendTo = new System.Windows.Forms.ToolStripMenuItem();
            this.ContextMenu_Exit = new System.Windows.Forms.ToolStripMenuItem();
            this.ContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.ContextMenu;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Clipboard Share";
            this.notifyIcon.Visible = true;
            // 
            // ContextMenu
            // 
            this.ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ContextMenu_SendTo,
            this.ContextMenu_Exit});
            this.ContextMenu.Name = "contextMenuStrip";
            this.ContextMenu.Size = new System.Drawing.Size(124, 48);
            // 
            // ContextMenu_SendTo
            // 
            this.ContextMenu_SendTo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ContextMenu_SendTo.Name = "ContextMenu_SendTo";
            this.ContextMenu_SendTo.Size = new System.Drawing.Size(123, 22);
            this.ContextMenu_SendTo.Text = "Send to...";
            // 
            // ContextMenu_Exit
            // 
            this.ContextMenu_Exit.Name = "ContextMenu_Exit";
            this.ContextMenu_Exit.Size = new System.Drawing.Size(123, 22);
            this.ContextMenu_Exit.Text = "Exit";
            this.ContextMenu_Exit.Click += new System.EventHandler(this.ContextMenu_Exit_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "Form1";
            this.ShowInTaskbar = false;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip ContextMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem ContextMenu_SendTo;
        private System.Windows.Forms.ToolStripMenuItem ContextMenu_Exit;
    }
}

