namespace QuranKareem
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
            this.Surah = new System.Windows.Forms.NumericUpDown();
            this.Page = new System.Windows.Forms.NumericUpDown();
            this.Ayah = new System.Windows.Forms.NumericUpDown();
            this.Quarter = new System.Windows.Forms.NumericUpDown();
            this.Surahs = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.folder = new System.Windows.Forms.FolderBrowserDialog();
            this.AyahRepeat = new System.Windows.Forms.NumericUpDown();
            this.AyahRepeatCheck = new System.Windows.Forms.CheckBox();
            this.SurahRepeat = new System.Windows.Forms.NumericUpDown();
            this.time5 = new System.Windows.Forms.Label();
            this.panel = new Guna.UI2.WinForms.Guna2Panel();
            this.SurahRepeatCheck = new System.Windows.Forms.CheckBox();
            this.pause = new System.Windows.Forms.Button();
            this.Rate = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.guna2BorderlessForm1 = new Guna.UI2.WinForms.Guna2BorderlessForm(this.components);
            this.guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.exitForm = new Guna.UI2.WinForms.Guna2Button();
            this.minimize = new Guna.UI2.WinForms.Guna2Button();
            this.label8 = new System.Windows.Forms.Label();
            this.Juz = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.Hizb = new System.Windows.Forms.NumericUpDown();
            this.copy = new System.Windows.Forms.Button();
            this.searchList = new System.Windows.Forms.ListBox();
            this.search = new System.Windows.Forms.Button();
            this.searchText = new System.Windows.Forms.TextBox();
            this.searchClose = new System.Windows.Forms.Button();
            this.tafasir = new System.Windows.Forms.ComboBox();
            this.tafseerCopy = new System.Windows.Forms.Button();
            this.saveRTF = new System.Windows.Forms.Button();
            this.saveRichText = new System.Windows.Forms.SaveFileDialog();
            this.rtb = new System.Windows.Forms.RichTextBox();
            this.latest = new Guna.UI2.WinForms.Guna2Button();
            this.stop = new System.Windows.Forms.Button();
            this.quranPic = new System.Windows.Forms.PictureBox();
            this.volume = new Guna.UI2.WinForms.Guna2TrackBar();
            this.label11 = new System.Windows.Forms.Label();
            this.addNewMoqrea = new System.Windows.Forms.Button();
            this.addShaykhInfo = new Guna.UI2.WinForms.Guna2Button();
            this.ShaykhDesc = new System.Windows.Forms.Button();
            this.lExt = new System.Windows.Forms.Label();
            this.extension = new System.Windows.Forms.TextBox();
            this.lComment = new System.Windows.Forms.Label();
            this.comment = new System.Windows.Forms.TextBox();
            this.descSave = new System.Windows.Forms.Button();
            this.timestampChangeEventCheck = new System.Windows.Forms.CheckBox();
            this.endAyatCheck = new System.Windows.Forms.CheckBox();
            this.combiner = new System.Windows.Forms.Button();
            this.splitAll = new System.Windows.Forms.Button();
            this.srtFile = new System.Windows.Forms.Button();
            this.saveSRTFile = new System.Windows.Forms.SaveFileDialog();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.dark = new Guna.UI2.WinForms.Guna2Button();
            this.spellingErrors = new System.Windows.Forms.CheckBox();
            this.pageZoom = new Guna.UI2.WinForms.Guna2Button();
            this.moshaf = new System.Windows.Forms.ComboBox();
            this.discri = new System.Windows.Forms.Button();
            this.pageSrtCheck = new System.Windows.Forms.CheckBox();
            this.videoEditor = new System.Windows.Forms.Button();
            this.youtubeCaptions = new System.Windows.Forms.Button();
            this.preventWordSelection = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.Surah)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Page)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Ayah)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Quarter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AyahRepeat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SurahRepeat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Rate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Juz)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Hizb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.quranPic)).BeginInit();
            this.SuspendLayout();
            // 
            // Surah
            // 
            this.Surah.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Surah.Location = new System.Drawing.Point(92, 48);
            this.Surah.Maximum = new decimal(new int[] {
            114,
            0,
            0,
            0});
            this.Surah.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Surah.Name = "Surah";
            this.Surah.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Surah.Size = new System.Drawing.Size(95, 28);
            this.Surah.TabIndex = 1;
            this.Surah.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Surah.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Surah.ValueChanged += new System.EventHandler(this.Surah_ValueChanged);
            // 
            // Page
            // 
            this.Page.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Page.Location = new System.Drawing.Point(92, 201);
            this.Page.Maximum = new decimal(new int[] {
            604,
            0,
            0,
            0});
            this.Page.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Page.Name = "Page";
            this.Page.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Page.Size = new System.Drawing.Size(95, 28);
            this.Page.TabIndex = 5;
            this.Page.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Page.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Page.ValueChanged += new System.EventHandler(this.Page_ValueChanged);
            // 
            // Ayah
            // 
            this.Ayah.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Ayah.Location = new System.Drawing.Point(92, 244);
            this.Ayah.Maximum = new decimal(new int[] {
            7,
            0,
            0,
            0});
            this.Ayah.Name = "Ayah";
            this.Ayah.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Ayah.Size = new System.Drawing.Size(95, 28);
            this.Ayah.TabIndex = 6;
            this.Ayah.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Ayah.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Ayah.ValueChanged += new System.EventHandler(this.Ayah_ValueChanged);
            // 
            // Quarter
            // 
            this.Quarter.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Quarter.Location = new System.Drawing.Point(92, 158);
            this.Quarter.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
            this.Quarter.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Quarter.Name = "Quarter";
            this.Quarter.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Quarter.Size = new System.Drawing.Size(95, 28);
            this.Quarter.TabIndex = 4;
            this.Quarter.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Quarter.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Quarter.ValueChanged += new System.EventHandler(this.Quarter_ValueChanged);
            // 
            // Surahs
            // 
            this.Surahs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Surahs.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Surahs.FormattingEnabled = true;
            this.Surahs.Items.AddRange(new object[] {
            "1",
            "2",
            "3"});
            this.Surahs.Location = new System.Drawing.Point(92, 13);
            this.Surahs.Name = "Surahs";
            this.Surahs.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Surahs.Size = new System.Drawing.Size(95, 29);
            this.Surahs.TabIndex = 0;
            this.Surahs.SelectedIndexChanged += new System.EventHandler(this.Surahs_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label1.Location = new System.Drawing.Point(197, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 22);
            this.label1.TabIndex = 10;
            this.label1.Text = "السورة";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label2.Location = new System.Drawing.Point(197, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 22);
            this.label2.TabIndex = 11;
            this.label2.Text = "السورة";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label3.Location = new System.Drawing.Point(211, 160);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 22);
            this.label3.TabIndex = 12;
            this.label3.Text = "الربع";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label4.Location = new System.Drawing.Point(197, 203);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 22);
            this.label4.TabIndex = 13;
            this.label4.Text = "الصفحة";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label5.Location = new System.Drawing.Point(211, 246);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 22);
            this.label5.TabIndex = 14;
            this.label5.Text = "الآية";
            // 
            // folder
            // 
            this.folder.SelectedPath = "audios";
            this.folder.ShowNewFolderButton = false;
            // 
            // AyahRepeat
            // 
            this.AyahRepeat.Font = new System.Drawing.Font("Tahoma", 13F);
            this.AyahRepeat.Location = new System.Drawing.Point(51, 440);
            this.AyahRepeat.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.AyahRepeat.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.AyahRepeat.Name = "AyahRepeat";
            this.AyahRepeat.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.AyahRepeat.Size = new System.Drawing.Size(55, 28);
            this.AyahRepeat.TabIndex = 14;
            this.AyahRepeat.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.AyahRepeat.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // AyahRepeatCheck
            // 
            this.AyahRepeatCheck.AutoSize = true;
            this.AyahRepeatCheck.BackColor = System.Drawing.Color.Transparent;
            this.AyahRepeatCheck.Font = new System.Drawing.Font("Tahoma", 13F);
            this.AyahRepeatCheck.Location = new System.Drawing.Point(133, 442);
            this.AyahRepeatCheck.Name = "AyahRepeatCheck";
            this.AyahRepeatCheck.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.AyahRepeatCheck.Size = new System.Drawing.Size(99, 26);
            this.AyahRepeatCheck.TabIndex = 13;
            this.AyahRepeatCheck.Text = "تكرار الآية";
            this.AyahRepeatCheck.UseVisualStyleBackColor = false;
            this.AyahRepeatCheck.CheckedChanged += new System.EventHandler(this.Repeat_CheckedChanged);
            // 
            // SurahRepeat
            // 
            this.SurahRepeat.Font = new System.Drawing.Font("Tahoma", 13F);
            this.SurahRepeat.Location = new System.Drawing.Point(51, 406);
            this.SurahRepeat.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.SurahRepeat.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.SurahRepeat.Name = "SurahRepeat";
            this.SurahRepeat.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.SurahRepeat.Size = new System.Drawing.Size(55, 28);
            this.SurahRepeat.TabIndex = 12;
            this.SurahRepeat.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.SurahRepeat.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // time5
            // 
            this.time5.AutoSize = true;
            this.time5.BackColor = System.Drawing.Color.Transparent;
            this.time5.Font = new System.Drawing.Font("Tahoma", 13F);
            this.time5.Location = new System.Drawing.Point(27, 286);
            this.time5.Name = "time5";
            this.time5.Size = new System.Drawing.Size(117, 22);
            this.time5.TabIndex = 29;
            this.time5.Text = "00:00:00.000";
            // 
            // panel
            // 
            this.panel.AutoScroll = true;
            this.panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
            this.panel.Location = new System.Drawing.Point(825, 46);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(263, 821);
            this.panel.TabIndex = 0;
            // 
            // SurahRepeatCheck
            // 
            this.SurahRepeatCheck.AutoSize = true;
            this.SurahRepeatCheck.BackColor = System.Drawing.Color.Transparent;
            this.SurahRepeatCheck.Font = new System.Drawing.Font("Tahoma", 13F);
            this.SurahRepeatCheck.Location = new System.Drawing.Point(112, 406);
            this.SurahRepeatCheck.Name = "SurahRepeatCheck";
            this.SurahRepeatCheck.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.SurahRepeatCheck.Size = new System.Drawing.Size(120, 26);
            this.SurahRepeatCheck.TabIndex = 11;
            this.SurahRepeatCheck.Text = "تكرار السورة";
            this.SurahRepeatCheck.UseVisualStyleBackColor = false;
            this.SurahRepeatCheck.CheckedChanged += new System.EventHandler(this.Repeat_CheckedChanged);
            // 
            // pause
            // 
            this.pause.Location = new System.Drawing.Point(213, 285);
            this.pause.Name = "pause";
            this.pause.Size = new System.Drawing.Size(45, 23);
            this.pause.TabIndex = 7;
            this.pause.Text = "||";
            this.pause.UseVisualStyleBackColor = true;
            this.pause.Click += new System.EventHandler(this.Pause_Click);
            // 
            // Rate
            // 
            this.Rate.DecimalPlaces = 3;
            this.Rate.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Rate.Increment = new decimal(new int[] {
            10,
            0,
            0,
            196608});
            this.Rate.Location = new System.Drawing.Point(55, 365);
            this.Rate.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            65536});
            this.Rate.Minimum = new decimal(new int[] {
            6,
            0,
            0,
            65536});
            this.Rate.Name = "Rate";
            this.Rate.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Rate.Size = new System.Drawing.Size(95, 28);
            this.Rate.TabIndex = 10;
            this.Rate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Rate.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Rate.ValueChanged += new System.EventHandler(this.Rate_ValueChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label7.Location = new System.Drawing.Point(156, 367);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 22);
            this.label7.TabIndex = 36;
            this.label7.Text = "السرعة";
            // 
            // guna2BorderlessForm1
            // 
            this.guna2BorderlessForm1.BorderRadius = 15;
            this.guna2BorderlessForm1.ContainerControl = this;
            this.guna2BorderlessForm1.DockIndicatorTransparencyValue = 0.8D;
            this.guna2BorderlessForm1.DragForm = false;
            this.guna2BorderlessForm1.TransparentWhileDrag = true;
            // 
            // guna2HtmlLabel1
            // 
            this.guna2HtmlLabel1.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel1.Font = new System.Drawing.Font("Times New Roman", 30F, System.Drawing.FontStyle.Bold);
            this.guna2HtmlLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.guna2HtmlLabel1.IsSelectionEnabled = false;
            this.guna2HtmlLabel1.Location = new System.Drawing.Point(9, 855);
            this.guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            this.guna2HtmlLabel1.Size = new System.Drawing.Size(263, 47);
            this.guna2HtmlLabel1.TabIndex = 37;
            this.guna2HtmlLabel1.Text = "<span style=\"color: red\">Q</span>uran <span style=\"color: blue\">K</span>areem";
            this.guna2HtmlLabel1.Click += new System.EventHandler(this.Guna2HtmlLabel1_Click);
            // 
            // exitForm
            // 
            this.exitForm.BackColor = System.Drawing.Color.Transparent;
            this.exitForm.BorderRadius = 4;
            this.exitForm.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.exitForm.Font = new System.Drawing.Font("Tahoma", 12F);
            this.exitForm.ForeColor = System.Drawing.Color.White;
            this.exitForm.Location = new System.Drawing.Point(1052, 11);
            this.exitForm.Name = "exitForm";
            this.exitForm.ShadowDecoration.Depth = 15;
            this.exitForm.ShadowDecoration.Enabled = true;
            this.exitForm.Size = new System.Drawing.Size(36, 28);
            this.exitForm.TabIndex = 38;
            this.exitForm.Text = "×";
            this.exitForm.Click += new System.EventHandler(this.ExitForm_Click);
            // 
            // minimize
            // 
            this.minimize.BackColor = System.Drawing.Color.Transparent;
            this.minimize.BorderRadius = 4;
            this.minimize.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.minimize.Font = new System.Drawing.Font("Tahoma", 12F);
            this.minimize.ForeColor = System.Drawing.Color.White;
            this.minimize.Location = new System.Drawing.Point(1015, 21);
            this.minimize.Name = "minimize";
            this.minimize.ShadowDecoration.Depth = 15;
            this.minimize.ShadowDecoration.Enabled = true;
            this.minimize.Size = new System.Drawing.Size(30, 10);
            this.minimize.TabIndex = 37;
            this.minimize.Click += new System.EventHandler(this.Minimize_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label8.Location = new System.Drawing.Point(208, 92);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(46, 22);
            this.label8.TabIndex = 42;
            this.label8.Text = "الجزء";
            // 
            // Juz
            // 
            this.Juz.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Juz.Location = new System.Drawing.Point(92, 90);
            this.Juz.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.Juz.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Juz.Name = "Juz";
            this.Juz.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Juz.Size = new System.Drawing.Size(95, 28);
            this.Juz.TabIndex = 2;
            this.Juz.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Juz.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Juz.ValueChanged += new System.EventHandler(this.Juz_ValueChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label9.Location = new System.Drawing.Point(200, 126);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(54, 22);
            this.label9.TabIndex = 44;
            this.label9.Text = "الحزب";
            // 
            // Hizb
            // 
            this.Hizb.Font = new System.Drawing.Font("Tahoma", 13F);
            this.Hizb.Location = new System.Drawing.Point(92, 124);
            this.Hizb.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.Hizb.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Hizb.Name = "Hizb";
            this.Hizb.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.Hizb.Size = new System.Drawing.Size(95, 28);
            this.Hizb.TabIndex = 3;
            this.Hizb.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Hizb.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.Hizb.ValueChanged += new System.EventHandler(this.Hizb_ValueChanged);
            // 
            // copy
            // 
            this.copy.Font = new System.Drawing.Font("Tahoma", 12F);
            this.copy.Location = new System.Drawing.Point(152, 578);
            this.copy.Name = "copy";
            this.copy.Size = new System.Drawing.Size(90, 42);
            this.copy.TabIndex = 16;
            this.copy.Text = "نسخ الآية";
            this.copy.UseVisualStyleBackColor = true;
            this.copy.Click += new System.EventHandler(this.Copy_Click);
            // 
            // searchList
            // 
            this.searchList.FormattingEnabled = true;
            this.searchList.Location = new System.Drawing.Point(12, 570);
            this.searchList.Name = "searchList";
            this.searchList.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.searchList.Size = new System.Drawing.Size(279, 290);
            this.searchList.TabIndex = 48;
            this.searchList.Visible = false;
            this.searchList.SelectedIndexChanged += new System.EventHandler(this.SearchList_SelectedIndexChanged);
            // 
            // search
            // 
            this.search.Font = new System.Drawing.Font("Tahoma", 12F);
            this.search.Location = new System.Drawing.Point(153, 533);
            this.search.Name = "search";
            this.search.Size = new System.Drawing.Size(90, 32);
            this.search.TabIndex = 18;
            this.search.Text = "بحث";
            this.search.UseVisualStyleBackColor = true;
            this.search.Click += new System.EventHandler(this.Search_Click);
            // 
            // searchText
            // 
            this.searchText.Font = new System.Drawing.Font("Tahoma", 12F);
            this.searchText.Location = new System.Drawing.Point(12, 503);
            this.searchText.Name = "searchText";
            this.searchText.Size = new System.Drawing.Size(262, 27);
            this.searchText.TabIndex = 20;
            this.searchText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // searchClose
            // 
            this.searchClose.Font = new System.Drawing.Font("Tahoma", 12F);
            this.searchClose.Location = new System.Drawing.Point(46, 533);
            this.searchClose.Name = "searchClose";
            this.searchClose.Size = new System.Drawing.Size(90, 32);
            this.searchClose.TabIndex = 19;
            this.searchClose.Text = "الغاء";
            this.searchClose.UseVisualStyleBackColor = true;
            this.searchClose.Visible = false;
            this.searchClose.Click += new System.EventHandler(this.SearchClose_Click);
            // 
            // tafasir
            // 
            this.tafasir.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tafasir.Font = new System.Drawing.Font("Tahoma", 13F);
            this.tafasir.FormattingEnabled = true;
            this.tafasir.Location = new System.Drawing.Point(16, 701);
            this.tafasir.Name = "tafasir";
            this.tafasir.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.tafasir.Size = new System.Drawing.Size(262, 29);
            this.tafasir.TabIndex = 22;
            this.tafasir.SelectedIndexChanged += new System.EventHandler(this.Tafasir_SelectedIndexChanged);
            // 
            // tafseerCopy
            // 
            this.tafseerCopy.Font = new System.Drawing.Font("Tahoma", 12F);
            this.tafseerCopy.Location = new System.Drawing.Point(168, 734);
            this.tafseerCopy.Name = "tafseerCopy";
            this.tafseerCopy.Size = new System.Drawing.Size(90, 36);
            this.tafseerCopy.TabIndex = 23;
            this.tafseerCopy.Text = "نسخ";
            this.tafseerCopy.UseVisualStyleBackColor = true;
            this.tafseerCopy.Click += new System.EventHandler(this.TafseerCopy_Click);
            // 
            // saveRTF
            // 
            this.saveRTF.Font = new System.Drawing.Font("Tahoma", 12F);
            this.saveRTF.Location = new System.Drawing.Point(22, 734);
            this.saveRTF.Name = "saveRTF";
            this.saveRTF.Size = new System.Drawing.Size(126, 36);
            this.saveRTF.TabIndex = 24;
            this.saveRTF.Text = "قراءة سريعة";
            this.saveRTF.UseVisualStyleBackColor = true;
            this.saveRTF.Click += new System.EventHandler(this.SaveRTF_Click);
            // 
            // saveRichText
            // 
            this.saveRichText.FileName = "tafseer";
            this.saveRichText.Filter = "Rich Text File|*.rtf";
            // 
            // rtb
            // 
            this.rtb.Font = new System.Drawing.Font("Times New Roman", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtb.Location = new System.Drawing.Point(-39, 706);
            this.rtb.Name = "rtb";
            this.rtb.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.rtb.Size = new System.Drawing.Size(50, 51);
            this.rtb.TabIndex = 57;
            this.rtb.Text = "Tafseer";
            this.rtb.Visible = false;
            // 
            // latest
            // 
            this.latest.BackColor = System.Drawing.Color.Transparent;
            this.latest.BorderRadius = 16;
            this.latest.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.latest.ForeColor = System.Drawing.Color.White;
            this.latest.Location = new System.Drawing.Point(183, 784);
            this.latest.Name = "latest";
            this.latest.Size = new System.Drawing.Size(76, 32);
            this.latest.TabIndex = 28;
            this.latest.Text = "البرنامج";
            this.latest.Click += new System.EventHandler(this.Latest_Click);
            // 
            // stop
            // 
            this.stop.Image = global::QuranKareem.Properties.Resources.stop;
            this.stop.Location = new System.Drawing.Point(162, 285);
            this.stop.Name = "stop";
            this.stop.Size = new System.Drawing.Size(45, 23);
            this.stop.TabIndex = 8;
            this.stop.UseVisualStyleBackColor = true;
            this.stop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // quranPic
            // 
            this.quranPic.BackColor = System.Drawing.Color.Transparent;
            this.quranPic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.quranPic.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.quranPic.Cursor = System.Windows.Forms.Cursors.Hand;
            this.quranPic.Location = new System.Drawing.Point(297, 5);
            this.quranPic.Name = "quranPic";
            this.quranPic.Size = new System.Drawing.Size(510, 900);
            this.quranPic.TabIndex = 2;
            this.quranPic.TabStop = false;
            this.quranPic.MouseClick += new System.Windows.Forms.MouseEventHandler(this.QuranPic_MouseClick);
            this.quranPic.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.QuranPic_MouseWheel);
            // 
            // volume
            // 
            this.volume.BackColor = System.Drawing.Color.Transparent;
            this.volume.Location = new System.Drawing.Point(18, 324);
            this.volume.Name = "volume";
            this.volume.Size = new System.Drawing.Size(140, 23);
            this.volume.TabIndex = 9;
            this.volume.ThumbColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(113)))), ((int)(((byte)(255)))));
            this.volume.ValueChanged += new System.EventHandler(this.Volume_ValueChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.Color.Transparent;
            this.label11.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label11.Location = new System.Drawing.Point(170, 325);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(98, 22);
            this.label11.TabIndex = 61;
            this.label11.Text = "حجم الصوت";
            // 
            // addNewMoqrea
            // 
            this.addNewMoqrea.Font = new System.Drawing.Font("Tahoma", 12F);
            this.addNewMoqrea.Location = new System.Drawing.Point(905, 873);
            this.addNewMoqrea.Name = "addNewMoqrea";
            this.addNewMoqrea.Size = new System.Drawing.Size(145, 32);
            this.addNewMoqrea.TabIndex = 31;
            this.addNewMoqrea.Text = "إضافة شيخ جديد";
            this.addNewMoqrea.UseVisualStyleBackColor = true;
            this.addNewMoqrea.Click += new System.EventHandler(this.AddNewMoqrea_Click);
            // 
            // addShaykhInfo
            // 
            this.addShaykhInfo.BorderRadius = 16;
            this.addShaykhInfo.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.addShaykhInfo.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.addShaykhInfo.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.addShaykhInfo.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.addShaykhInfo.Enabled = false;
            this.addShaykhInfo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.addShaykhInfo.ForeColor = System.Drawing.Color.White;
            this.addShaykhInfo.Location = new System.Drawing.Point(1056, 873);
            this.addShaykhInfo.Name = "addShaykhInfo";
            this.addShaykhInfo.Size = new System.Drawing.Size(32, 32);
            this.addShaykhInfo.TabIndex = 32;
            this.addShaykhInfo.Text = "!";
            this.addShaykhInfo.Click += new System.EventHandler(this.AddShaykhInfo_Click);
            // 
            // ShaykhDesc
            // 
            this.ShaykhDesc.Enabled = false;
            this.ShaykhDesc.Font = new System.Drawing.Font("Tahoma", 12F);
            this.ShaykhDesc.Location = new System.Drawing.Point(825, 873);
            this.ShaykhDesc.Name = "ShaykhDesc";
            this.ShaykhDesc.Size = new System.Drawing.Size(74, 32);
            this.ShaykhDesc.TabIndex = 30;
            this.ShaykhDesc.Text = "الوصف";
            this.ShaykhDesc.UseVisualStyleBackColor = true;
            this.ShaykhDesc.EnabledChanged += new System.EventHandler(this.ShaykhDesc_);
            this.ShaykhDesc.Click += new System.EventHandler(this.ShaykhDesc_);
            // 
            // lExt
            // 
            this.lExt.AutoSize = true;
            this.lExt.BackColor = System.Drawing.Color.Transparent;
            this.lExt.Font = new System.Drawing.Font("Tahoma", 13F);
            this.lExt.Location = new System.Drawing.Point(222, 679);
            this.lExt.Name = "lExt";
            this.lExt.Size = new System.Drawing.Size(62, 22);
            this.lExt.TabIndex = 64;
            this.lExt.Text = "الامتداد";
            this.lExt.Visible = false;
            // 
            // extension
            // 
            this.extension.Font = new System.Drawing.Font("Tahoma", 13F);
            this.extension.Location = new System.Drawing.Point(18, 676);
            this.extension.Name = "extension";
            this.extension.Size = new System.Drawing.Size(198, 28);
            this.extension.TabIndex = 25;
            this.extension.Visible = false;
            // 
            // lComment
            // 
            this.lComment.AutoSize = true;
            this.lComment.BackColor = System.Drawing.Color.Transparent;
            this.lComment.Font = new System.Drawing.Font("Tahoma", 13F);
            this.lComment.Location = new System.Drawing.Point(232, 711);
            this.lComment.Name = "lComment";
            this.lComment.Size = new System.Drawing.Size(52, 22);
            this.lComment.TabIndex = 66;
            this.lComment.Text = "تعليق";
            this.lComment.Visible = false;
            // 
            // comment
            // 
            this.comment.Font = new System.Drawing.Font("Tahoma", 13F);
            this.comment.Location = new System.Drawing.Point(18, 708);
            this.comment.Name = "comment";
            this.comment.Size = new System.Drawing.Size(198, 28);
            this.comment.TabIndex = 26;
            this.comment.Visible = false;
            // 
            // descSave
            // 
            this.descSave.Font = new System.Drawing.Font("Tahoma", 12F);
            this.descSave.Location = new System.Drawing.Point(61, 740);
            this.descSave.Name = "descSave";
            this.descSave.Size = new System.Drawing.Size(155, 35);
            this.descSave.TabIndex = 27;
            this.descSave.Text = "حفظ و تنظيف";
            this.descSave.UseVisualStyleBackColor = true;
            this.descSave.Visible = false;
            this.descSave.Click += new System.EventHandler(this.DescSave_Click);
            // 
            // timestampChangeEventCheck
            // 
            this.timestampChangeEventCheck.AutoSize = true;
            this.timestampChangeEventCheck.BackColor = System.Drawing.Color.Transparent;
            this.timestampChangeEventCheck.Checked = true;
            this.timestampChangeEventCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.timestampChangeEventCheck.Location = new System.Drawing.Point(837, 28);
            this.timestampChangeEventCheck.Name = "timestampChangeEventCheck";
            this.timestampChangeEventCheck.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.timestampChangeEventCheck.Size = new System.Drawing.Size(154, 17);
            this.timestampChangeEventCheck.TabIndex = 34;
            this.timestampChangeEventCheck.Text = "التحسس من تغيير التوقيتات";
            this.timestampChangeEventCheck.UseVisualStyleBackColor = false;
            this.timestampChangeEventCheck.Visible = false;
            // 
            // endAyatCheck
            // 
            this.endAyatCheck.AutoSize = true;
            this.endAyatCheck.BackColor = System.Drawing.Color.Transparent;
            this.endAyatCheck.Location = new System.Drawing.Point(865, 11);
            this.endAyatCheck.Name = "endAyatCheck";
            this.endAyatCheck.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.endAyatCheck.Size = new System.Drawing.Size(108, 17);
            this.endAyatCheck.TabIndex = 33;
            this.endAyatCheck.Text = "وضعية نهاية الآيات";
            this.endAyatCheck.UseVisualStyleBackColor = false;
            this.endAyatCheck.Visible = false;
            this.endAyatCheck.CheckedChanged += new System.EventHandler(this.EndAyatCheck_CheckedChanged);
            // 
            // combiner
            // 
            this.combiner.Location = new System.Drawing.Point(923, 3);
            this.combiner.Name = "combiner";
            this.combiner.Size = new System.Drawing.Size(83, 22);
            this.combiner.TabIndex = 36;
            this.combiner.Text = "تجميع الآيات";
            this.combiner.UseVisualStyleBackColor = true;
            this.combiner.Click += new System.EventHandler(this.Combiner_Click);
            // 
            // splitAll
            // 
            this.splitAll.Location = new System.Drawing.Point(923, 24);
            this.splitAll.Name = "splitAll";
            this.splitAll.Size = new System.Drawing.Size(83, 22);
            this.splitAll.TabIndex = 35;
            this.splitAll.Text = "تقطيع السورة";
            this.splitAll.UseVisualStyleBackColor = true;
            this.splitAll.Click += new System.EventHandler(this.SplitAll_Click);
            // 
            // srtFile
            // 
            this.srtFile.Font = new System.Drawing.Font("Tahoma", 8F);
            this.srtFile.Location = new System.Drawing.Point(138, 674);
            this.srtFile.Name = "srtFile";
            this.srtFile.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.srtFile.Size = new System.Drawing.Size(96, 25);
            this.srtFile.TabIndex = 21;
            this.srtFile.Text = "ملف SRT";
            this.srtFile.UseVisualStyleBackColor = true;
            this.srtFile.Click += new System.EventHandler(this.SrtFile_Click);
            // 
            // saveSRTFile
            // 
            this.saveSRTFile.FileName = "Surah";
            this.saveSRTFile.Filter = "SubRip Subtitle|*.srt";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label10.Location = new System.Drawing.Point(26, 126);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(44, 22);
            this.label10.TabIndex = 80;
            this.label10.Text = "Hizb";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.Color.Transparent;
            this.label12.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label12.Location = new System.Drawing.Point(29, 92);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(36, 22);
            this.label12.TabIndex = 79;
            this.label12.Text = "Juz";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.Color.Transparent;
            this.label13.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label13.Location = new System.Drawing.Point(26, 246);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(49, 22);
            this.label13.TabIndex = 78;
            this.label13.Text = "Ayah";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.Transparent;
            this.label14.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label14.Location = new System.Drawing.Point(26, 203);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(48, 22);
            this.label14.TabIndex = 77;
            this.label14.Text = "Page";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Transparent;
            this.label15.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label15.Location = new System.Drawing.Point(19, 160);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(69, 22);
            this.label15.TabIndex = 76;
            this.label15.Text = "Quarter";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.BackColor = System.Drawing.Color.Transparent;
            this.label16.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label16.Location = new System.Drawing.Point(21, 50);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(55, 22);
            this.label16.TabIndex = 75;
            this.label16.Text = "Surah";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.BackColor = System.Drawing.Color.Transparent;
            this.label17.Font = new System.Drawing.Font("Tahoma", 13F);
            this.label17.Location = new System.Drawing.Point(21, 16);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(55, 22);
            this.label17.TabIndex = 74;
            this.label17.Text = "Surah";
            // 
            // dark
            // 
            this.dark.BorderRadius = 16;
            this.dark.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.dark.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.dark.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.dark.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.dark.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.dark.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.dark.ForeColor = System.Drawing.Color.White;
            this.dark.Location = new System.Drawing.Point(107, 784);
            this.dark.Name = "dark";
            this.dark.Size = new System.Drawing.Size(70, 32);
            this.dark.TabIndex = 81;
            this.dark.Text = "Dark";
            this.dark.Click += new System.EventHandler(this.Dark_Click);
            // 
            // spellingErrors
            // 
            this.spellingErrors.AutoSize = true;
            this.spellingErrors.Location = new System.Drawing.Point(168, 483);
            this.spellingErrors.Name = "spellingErrors";
            this.spellingErrors.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.spellingErrors.Size = new System.Drawing.Size(99, 17);
            this.spellingErrors.TabIndex = 82;
            this.spellingErrors.Text = "مع أخطاء إملائية";
            this.spellingErrors.UseVisualStyleBackColor = true;
            // 
            // pageZoom
            // 
            this.pageZoom.BorderRadius = 16;
            this.pageZoom.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.pageZoom.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.pageZoom.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.pageZoom.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.pageZoom.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.pageZoom.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.pageZoom.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.pageZoom.Location = new System.Drawing.Point(31, 784);
            this.pageZoom.Name = "pageZoom";
            this.pageZoom.Size = new System.Drawing.Size(70, 32);
            this.pageZoom.TabIndex = 84;
            this.pageZoom.Text = "Zoom";
            this.pageZoom.Click += new System.EventHandler(this.PageZoom_Click);
            // 
            // moshaf
            // 
            this.moshaf.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.moshaf.Font = new System.Drawing.Font("Tahoma", 13F);
            this.moshaf.FormattingEnabled = true;
            this.moshaf.Items.AddRange(new object[] {
            " * المصاحف المصورة ..."});
            this.moshaf.Location = new System.Drawing.Point(16, 824);
            this.moshaf.Name = "moshaf";
            this.moshaf.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.moshaf.Size = new System.Drawing.Size(262, 29);
            this.moshaf.TabIndex = 85;
            this.moshaf.SelectedIndexChanged += new System.EventHandler(this.Moshaf_SelectedIndexChanged);
            // 
            // discri
            // 
            this.discri.Font = new System.Drawing.Font("Tahoma", 12F);
            this.discri.Location = new System.Drawing.Point(56, 584);
            this.discri.Name = "discri";
            this.discri.Size = new System.Drawing.Size(64, 32);
            this.discri.TabIndex = 86;
            this.discri.Text = "تلوين";
            this.discri.UseVisualStyleBackColor = true;
            this.discri.Click += new System.EventHandler(this.Discri_Click);
            // 
            // pageSrtCheck
            // 
            this.pageSrtCheck.AutoSize = true;
            this.pageSrtCheck.Location = new System.Drawing.Point(37, 678);
            this.pageSrtCheck.Name = "pageSrtCheck";
            this.pageSrtCheck.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.pageSrtCheck.Size = new System.Drawing.Size(93, 17);
            this.pageSrtCheck.TabIndex = 87;
            this.pageSrtCheck.Text = "للصفحة الحالية";
            this.pageSrtCheck.UseVisualStyleBackColor = true;
            // 
            // videoEditor
            // 
            this.videoEditor.Location = new System.Drawing.Point(824, 24);
            this.videoEditor.Name = "videoEditor";
            this.videoEditor.Size = new System.Drawing.Size(100, 22);
            this.videoEditor.TabIndex = 88;
            this.videoEditor.Text = "Quran Template";
            this.videoEditor.UseVisualStyleBackColor = true;
            this.videoEditor.Click += new System.EventHandler(this.VideoEditor_Click);
            // 
            // youtubeCaptions
            // 
            this.youtubeCaptions.Location = new System.Drawing.Point(824, 3);
            this.youtubeCaptions.Name = "youtubeCaptions";
            this.youtubeCaptions.Size = new System.Drawing.Size(100, 22);
            this.youtubeCaptions.TabIndex = 89;
            this.youtubeCaptions.Text = "Youtube Captions";
            this.youtubeCaptions.UseVisualStyleBackColor = true;
            this.youtubeCaptions.Click += new System.EventHandler(this.YoutubeCaptions_Click);
            // 
            // preventWordSelection
            // 
            this.preventWordSelection.AutoSize = true;
            this.preventWordSelection.BackColor = System.Drawing.Color.Transparent;
            this.preventWordSelection.Font = new System.Drawing.Font("Tahoma", 13F);
            this.preventWordSelection.Location = new System.Drawing.Point(63, 634);
            this.preventWordSelection.Name = "preventWordSelection";
            this.preventWordSelection.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.preventWordSelection.Size = new System.Drawing.Size(174, 26);
            this.preventWordSelection.TabIndex = 90;
            this.preventWordSelection.Text = "منع القراءة بالكلمات";
            this.preventWordSelection.UseVisualStyleBackColor = false;
            this.preventWordSelection.CheckedChanged += new System.EventHandler(this.PreventWordSelection_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 910);
            this.Controls.Add(this.searchList);
            this.Controls.Add(this.preventWordSelection);
            this.Controls.Add(this.youtubeCaptions);
            this.Controls.Add(this.videoEditor);
            this.Controls.Add(this.pageSrtCheck);
            this.Controls.Add(this.discri);
            this.Controls.Add(this.moshaf);
            this.Controls.Add(this.pageZoom);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.dark);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.splitAll);
            this.Controls.Add(this.combiner);
            this.Controls.Add(this.endAyatCheck);
            this.Controls.Add(this.timestampChangeEventCheck);
            this.Controls.Add(this.descSave);
            this.Controls.Add(this.comment);
            this.Controls.Add(this.lComment);
            this.Controls.Add(this.extension);
            this.Controls.Add(this.lExt);
            this.Controls.Add(this.ShaykhDesc);
            this.Controls.Add(this.addShaykhInfo);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.volume);
            this.Controls.Add(this.latest);
            this.Controls.Add(this.rtb);
            this.Controls.Add(this.saveRTF);
            this.Controls.Add(this.tafseerCopy);
            this.Controls.Add(this.tafasir);
            this.Controls.Add(this.searchClose);
            this.Controls.Add(this.searchText);
            this.Controls.Add(this.search);
            this.Controls.Add(this.copy);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.Hizb);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.Juz);
            this.Controls.Add(this.stop);
            this.Controls.Add(this.minimize);
            this.Controls.Add(this.exitForm);
            this.Controls.Add(this.guna2HtmlLabel1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.Rate);
            this.Controls.Add(this.pause);
            this.Controls.Add(this.SurahRepeatCheck);
            this.Controls.Add(this.time5);
            this.Controls.Add(this.SurahRepeat);
            this.Controls.Add(this.AyahRepeatCheck);
            this.Controls.Add(this.AyahRepeat);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Surahs);
            this.Controls.Add(this.Quarter);
            this.Controls.Add(this.Ayah);
            this.Controls.Add(this.Page);
            this.Controls.Add(this.Surah);
            this.Controls.Add(this.quranPic);
            this.Controls.Add(this.addNewMoqrea);
            this.Controls.Add(this.srtFile);
            this.Controls.Add(this.spellingErrors);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Quran Kareem";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.Surah)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Page)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Ayah)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Quarter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AyahRepeat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SurahRepeat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Rate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Juz)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Hizb)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.quranPic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox quranPic;
        private System.Windows.Forms.NumericUpDown Surah;
        private System.Windows.Forms.NumericUpDown Page;
        private System.Windows.Forms.NumericUpDown Ayah;
        private System.Windows.Forms.NumericUpDown Quarter;
        private System.Windows.Forms.ComboBox Surahs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.FolderBrowserDialog folder;
        private System.Windows.Forms.NumericUpDown AyahRepeat;
        private System.Windows.Forms.CheckBox AyahRepeatCheck;
        private System.Windows.Forms.NumericUpDown SurahRepeat;
        private System.Windows.Forms.Label time5;
        private Guna.UI2.WinForms.Guna2Panel panel;
        private System.Windows.Forms.CheckBox SurahRepeatCheck;
        private System.Windows.Forms.Button pause;
        private System.Windows.Forms.NumericUpDown Rate;
        private System.Windows.Forms.Label label7;
        private Guna.UI2.WinForms.Guna2BorderlessForm guna2BorderlessForm1;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private Guna.UI2.WinForms.Guna2Button exitForm;
        private Guna.UI2.WinForms.Guna2Button minimize;
        private System.Windows.Forms.Button stop;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown Hizb;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown Juz;
        private System.Windows.Forms.Button copy;
        private System.Windows.Forms.ListBox searchList;
        private System.Windows.Forms.TextBox searchText;
        private System.Windows.Forms.Button search;
        private System.Windows.Forms.Button searchClose;
        private System.Windows.Forms.Button tafseerCopy;
        private System.Windows.Forms.ComboBox tafasir;
        private System.Windows.Forms.Button saveRTF;
        private System.Windows.Forms.SaveFileDialog saveRichText;
        private System.Windows.Forms.RichTextBox rtb;
        private Guna.UI2.WinForms.Guna2Button latest;
        private System.Windows.Forms.Label label11;
        private Guna.UI2.WinForms.Guna2TrackBar volume;
        private System.Windows.Forms.Button addNewMoqrea;
        private Guna.UI2.WinForms.Guna2Button addShaykhInfo;
        private System.Windows.Forms.Button ShaykhDesc;
        private System.Windows.Forms.Label lExt;
        private System.Windows.Forms.TextBox extension;
        private System.Windows.Forms.TextBox comment;
        private System.Windows.Forms.Label lComment;
        private System.Windows.Forms.Button descSave;
        private System.Windows.Forms.CheckBox timestampChangeEventCheck;
        private System.Windows.Forms.CheckBox endAyatCheck;
        private System.Windows.Forms.Button combiner;
        private System.Windows.Forms.Button splitAll;
        private System.Windows.Forms.Button srtFile;
        private System.Windows.Forms.SaveFileDialog saveSRTFile;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private Guna.UI2.WinForms.Guna2Button dark;
        private System.Windows.Forms.CheckBox spellingErrors;
        private Guna.UI2.WinForms.Guna2Button pageZoom;
        private System.Windows.Forms.ComboBox moshaf;
        private System.Windows.Forms.Button discri;
        private System.Windows.Forms.CheckBox pageSrtCheck;
        private System.Windows.Forms.Button videoEditor;
        private System.Windows.Forms.Button youtubeCaptions;
        private System.Windows.Forms.CheckBox preventWordSelection;
    }
}

