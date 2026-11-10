namespace QuranKareem
{
    partial class DownloadAudioQuranFiles
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.fileViewPage = new System.Windows.Forms.TabPage();
            this.fileDownloadPage = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.tabControl.SuspendLayout();
            this.fileDownloadPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.fileViewPage);
            this.tabControl.Controls.Add(this.fileDownloadPage);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.tabControl.RightToLeftLayout = true;
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(808, 581);
            this.tabControl.TabIndex = 0;
            // 
            // fileViewPage
            // 
            this.fileViewPage.Location = new System.Drawing.Point(4, 22);
            this.fileViewPage.Name = "fileViewPage";
            this.fileViewPage.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.fileViewPage.Size = new System.Drawing.Size(800, 555);
            this.fileViewPage.TabIndex = 0;
            this.fileViewPage.Text = "ملفات القارئ";
            this.fileViewPage.UseVisualStyleBackColor = true;
            // 
            // fileDownloadPage
            // 
            this.fileDownloadPage.Controls.Add(this.flowLayoutPanel);
            this.fileDownloadPage.Location = new System.Drawing.Point(4, 22);
            this.fileDownloadPage.Name = "fileDownloadPage";
            this.fileDownloadPage.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.fileDownloadPage.Size = new System.Drawing.Size(800, 555);
            this.fileDownloadPage.TabIndex = 0;
            this.fileDownloadPage.Text = "طابور التحميل";
            this.fileDownloadPage.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.AutoScroll = true;
            this.flowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(800, 555);
            this.flowLayoutPanel.TabIndex = 0;
            this.flowLayoutPanel.WrapContents = false;
            // 
            // DownloadAudioQuranFiles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(808, 581);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "DownloadAudioQuranFiles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "تحميل التلاوات";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DownloadAudioQuranFiles_FormClosing);
            this.tabControl.ResumeLayout(false);
            this.fileDownloadPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage fileViewPage;
        private System.Windows.Forms.TabPage fileDownloadPage;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
    }
}
