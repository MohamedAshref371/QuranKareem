using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace QuranKareem
{
    public partial class Form1 : Form
    {

        readonly string save;

        readonly TextQuran quranText = TextQuran.Instance;
        readonly PictureQuran quranPicture = PictureQuran.Instance;
        readonly AudioQuran quranAudio = AudioQuran.Instance;
        private TafseerQuran tafseerSource;
        private readonly LatestTafseerRequest tafseerPreviewRequest = new LatestTafseerRequest();
        private readonly LatestTafseerRequest tafseerCopyRequest = new LatestTafseerRequest();
        private readonly Action<string> copyTafseerText;
        private Form tafseerPreview;
        readonly TrueTypeFontQuran quranTtf = TrueTypeFontQuran.Instance;
        private IVisualQuran quranVisual = null;
        private ReadingNavigator reading;
        private QuranSearchService searchService;
        private QuranSearchResult[] searchResults = new QuranSearchResult[0];
        private CancellationTokenSource searchCancellation;

        DownloadAudioQuranFiles downloadForm = null;

        string moshafText = "", moshafAudio = "", tafseer = "";

        #region Form
        private readonly int SizeX = 1100, SizeY = 910;

        public Form1() : this(
            Microsoft.VisualBasic.FileIO.SpecialDirectories.AllUsersApplicationData.Replace(Application.ProductVersion, ""),
            text => Clipboard.SetText(text, TextDataFormat.UnicodeText))
        {
        }

        internal Form1(string settingsPath, Action<string> copyTafseerText)
        {
            save = settingsPath;
            this.copyTafseerText = copyTafseerText;
            InitializeComponent();
            quranPicture.PictureReplaced += QuranPicture_PictureReplaced;
        }

        private void QuranPicture_PictureReplaced(object sender, EventArgs e)
            => quranPic.BackgroundImage = quranPicture.CurrentPicture;

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            quranPic.BackgroundImage = null;
            quranPicture.PictureReplaced -= QuranPicture_PictureReplaced;
            quranPicture.ReleasePictures();
            base.OnFormClosed(e);
        }
        bool success = false;
        #endregion

        #region Form EventArgs
        FormSize fs;
        private void Form1_Load(object sender, EventArgs e)
        {

            string[] textsFiles = null, picturesFolders = null;

            if (Directory.Exists("texts")) textsFiles = Directory.GetFiles("texts"); /*البحث في مجلد المصاحف المكتوبة */

            if (Directory.Exists("pictures")) picturesFolders = Directory.GetDirectories("pictures"); /* البحث في مجلد المصاحف المصورة */

            if (picturesFolders != null && picturesFolders.Length > 0)
            {
                for (int i = 0; i < picturesFolders.Length; i++)
                    moshaf.Items.Add(picturesFolders[i].Split('\\').Last());
                moshaf.Items.Add("تحميل ...");
                string s = @"pictures\TheChosenMoshaf.txt";
                if (File.Exists(s) && (s = File.ReadAllText(s, Encoding.UTF8)).Trim() != "" && Directory.Exists($@"pictures\{s}"))
                    moshaf.SelectedItem = s;
                else
                    moshaf.SelectedItem = picturesFolders[0].Split('\\').Last();
            }

            // مجلد الصور غير موجود ؟
            else if (textsFiles != null && textsFiles.Length > 0)
            {
                moshaf.SelectedIndex = 0;
                moshaf.Items.Add("تحميل ...");
                qPicture = 0; // التبديل إلى RichTextBox
                UseVisual(quranText);
                pageZoom.Enabled = false; // تعطيل خاصية تكبير الصفحة
                quranText.AddRichTextBoxInControls(Controls, quranPic.Location.X, quranPic.Location.Y, quranPic.Width, quranPic.Height); // اظهاره في النافذة
                quranText.AddEventHandler(PageQuranText_Click); // اضافة دالة تُنفذ عند الضغط بالماوس
                quranPic.Visible = false;
            }

            else
            {
                MessageBox.Show($"مجلدات المصاحف غير موجودة،\nسيتم إغلاق البرنامج.");
                Close();
            }

            if (textsFiles != null && textsFiles.Length > 0)
            {
                quranText.Start(textsFiles[0], (int)Surah.Value, (int)Ayah.Value);
                searchService = new QuranSearchService(textsFiles[0]);
            }

            Surahs.Items.Clear(); // إفراغ قائمة ال ComboBox
            Surahs.Items.AddRange(quranVisual.GetSurahNames()); // ملأها بأسماء السور
            Surahs.SelectedIndex = (int)Surah.Value - 1; // الإشارة على أول سورة

            quranAudio.AddInControls(Controls); // اضافة المشغل الصوتي إلى خلفية النافذة
            quranAudio.AddEventHandlerOfAyah(AyahAudio); // اضافة تنبيه لإنتهاء الآية حين يقرأ الشيخ
            quranAudio.AddEventHandlerOfWord(WordAudio);

            tafasir.Items.Clear();
            AddTafseerSources("tafasir", "* التفاسير ...");
            AddTafseerSources("translations", "* التراجم ...");
            if (tafasir.Items.Count > 0) tafasir.SelectedIndex = 0;

            // الوقوف عند الآية التي كنت فيها قبل اغلاق البرنامج آخر مرة
            try
            {
                string[] stringArray;
                if (File.Exists(save + "AyahNumberCurrent"))
                {
                    stringArray = File.ReadAllText(save + "AyahNumberCurrent").Split(',');
                    Surah.Value = Convert.ToInt32(stringArray[0]);
                    Ayah.Value = Convert.ToInt32(stringArray[1]);
                }
            }
            catch { }

            // تشغيل الشيخ الذي كنت تسمعه آخر مرة
            try
            {
                if (File.Exists(save + "MoshafAudioCurrent"))
                {
                    moshafAudio = File.ReadAllText(save + "MoshafAudioCurrent");
                    quranAudio.Start(moshafAudio, (int)Surah.Value, (int)Ayah.Value);
                    time5.Text = quranAudio.GetCurrentPosition();
                    quranAudio.OneTimePause(); // ثم إيقاف صوته :)
                }
            }
            catch { }

            // ذاكرتك ضعيفة ولا تتذكر كتاب التفسير الذي كنت عليه قبلاً؟
            try
            {
                if (File.Exists(save + "TafseerCurrent"))
                {
                    string savedSource = File.ReadAllText(save + "TafseerCurrent");
                    var sources = tafasir.Items.OfType<TafseerQuran>();
                    TafseerQuran selected = sources.FirstOrDefault(item => item.Key == savedSource) ??
                        sources.FirstOrDefault(item => item.Name == savedSource);
                    if (selected != null) tafasir.SelectedItem = selected;
                }
            }
            catch { }

            try
            {
                if (File.Exists(save + "Volume"))
                    volume.Value = Convert.ToInt32(File.ReadAllText(save + "Volume"));
            }
            catch { }

            // FormWindowState -> Maximized
            fs = new FormSize(SizeX, SizeY, Size.Width, Size.Height);
            fs.SetControls(Controls);
            AddMashaykhButtons();

            // The Controls which backcolor is not subject to the form's backcolor.
            ControlsList.AddRange(new List<Control> { Surahs, Surah, Juz, Hizb, Quarter, Page, Ayah, pause, stop, Rate, SurahRepeat, AyahRepeat, copy, search, searchClose, searchText, searchList, srtFile, extension, comment, tafasir, tafseerCopy, saveRTF, descSave, latest, moshaf, addNewMoqrea, splitAll, combiner, videoEditor, youtubeCaptions, discri });
            success = true;
        }

        private int qPicture;
        private void Moshaf_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (moshaf.Items.Count != 1 && moshaf.SelectedIndex == moshaf.Items.Count - 1)
            {
                Process.Start("https://github.com/mohamedashref371/QuranKareem/tree/master/Quran%20Kareem/pictures");
            }
            else if (moshaf.SelectedIndex > 0)
            {
                if (qPicture == 2)
                    quranTtf.PageRichText.Visible = false;

                qPicture = 0;
                if (quranPicture.Start($@"pictures\{moshaf.SelectedItem}", (int)Surah.Value, (int)Ayah.Value))
                    qPicture = 1;
                else
                {
                    quranPicture.ReleasePictures();
                    if (quranTtf.Start($@"pictures\{moshaf.SelectedItem}", (int)Surah.Value, (int)Ayah.Value))
                        qPicture = 2;
                }

                if (qPicture == 1)
                {
                    quranPic.Visible = true;
                    quranPic.BackColor = dark.Text == "Dark" ? Coloring.Light.BackColor : Coloring.Night.BackColor;
                    UseVisual(quranPicture);
                }
                else if (qPicture == 2)
                {
                    quranPic.Visible = false;
                    quranTtf.PageRichText.Location = new Point(quranPic.Location.X, quranPic.Location.Y);
                    quranTtf.PageRichText.BackColor = dark.Text == "Dark" ? Coloring.Light.BackColor : Coloring.Night.BackColor;
                    if (!Controls.Contains(quranTtf.PageRichText))
                    {
                        quranTtf.PageRichText.Click += PageFontQuranText_Click;
                        quranTtf.PageRichText.MouseWheel += QuranPic_MouseWheel;
                        Controls.Add(quranTtf.PageRichText);
                    }
                    quranTtf.PageRichText.Size = new Size(quranPic.Width, quranPic.Height);
                    quranTtf.SetWidth();
                    quranTtf.PageRichText.Visible = true;
                    UseVisual(quranTtf);
                }
            }
        }

        private bool waitingForDownloadsToStop;

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.Cancel) return;
            if (e.CloseReason == CloseReason.UserClosing && audioExportTask != null && !audioExportTask.IsCompleted)
            {
                e.Cancel = true;
                MessageBox.Show(this, "انتظر اكتمال تصدير الملفات الصوتية قبل إغلاق البرنامج.", "تصدير الصوت");
                return;
            }
            if (e.CloseReason == CloseReason.UserClosing && downloadForm != null && downloadForm.IsBusy)
            {
                e.Cancel = true;
                if (waitingForDownloadsToStop) return;
                if (MessageBox.Show(this, "هناك تلاوات قيد التحميل. هل تريد إلغاء التحميل والخروج؟", "إغلاق البرنامج",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    downloadForm.OpenDownloadingTab();
                    downloadForm.Show();
                    downloadForm.WindowState = FormWindowState.Normal;
                    return;
                }

                waitingForDownloadsToStop = true;
                try { await downloadForm.StopDownloadsAsync(); }
                finally { waitingForDownloadsToStop = false; }
                // Re-enter closing after this event has returned, even if the transfer stopped immediately.
                BeginInvoke(new Action(Close));
                return;
            }

            searchCancellation?.Cancel();
            InvalidateTafseer();
            quranAudio.Stop();
            if (!success) return;
            // حفظ الإعدادات الحالية عند إغلاق البرنامج لاستعادتها لاحقاً
            File.WriteAllText(save + "AyahNumberCurrent", Surah.Value + "," + Ayah.Value);
            File.WriteAllText(save + "MoshafTextCurrent", moshafText);

            if (qPicture != 0 && moshaf.SelectedIndex > 0 && moshaf.SelectedIndex < moshaf.Items.Count - 1)
                File.WriteAllText(@"pictures\TheChosenMoshaf.txt", (string)moshaf.SelectedItem, Encoding.UTF8);
            
            File.WriteAllText(save + "MoshafAudioCurrent", moshafAudio);
            File.WriteAllText(save + "TafseerCurrent", tafseer);
            File.WriteAllText(save + "Volume", volume.Value + "");
        }

        private readonly string visibleEqualTrue = "Visible == true";
        private void ExitForm_Click(object sender, EventArgs e) // زر الإغلاق
        {
            if (zoom) // البرنامج في وضعية الزوم
            {
                zoom = !zoom; // الخروج من وضعية الزوم
                for (int i = 0; i < Controls.Count; i++)
                {
                    if ((string)Controls[i].Tag == visibleEqualTrue)
                    {
                        Controls[i].Visible = true;
                        Controls[i].Tag = null;
                    }
                }
                exitForm.TabStop = true;
                minimize.TabStop = true;
                if (qPicture == 2)
                {
                    quranTtf.PageRichText.Location = new Point(quranPictureLocation.X, quranPictureLocation.Y);
                    quranTtf.PageRichText.Size = new Size(quranPicSize.Width, quranPicSize.Height);
                    quranTtf.SetWidth();
                }
                else
                {
                    quranPic.Size = quranPicSize;
                    quranPic.Location = quranPictureLocation;
                }
            }
            else
            {
                Close();
            }
        }

        private void Minimize_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;

        Point quranPictureLocation;
        Size quranPicSize;
        bool zoom = false;
        // تكبير صفحة القرآن المصور
        private void PageZoom_Click(object sender, EventArgs e)
        {
            if (!zoom)
            {
                zoom = !zoom;
                for (int i = 0; i < Controls.Count; i++)
                {
                    if (Controls[i].Visible == true)
                    {
                        Controls[i].Tag = visibleEqualTrue;
                        Controls[i].Visible = false;
                    }
                }
                exitForm.Visible = true; exitForm.TabStop = false;
                minimize.Visible = true; minimize.TabStop = false;
                quranPicSize = quranPic.Size;
                quranPictureLocation = quranPic.Location;
                if (qPicture == 2)
                {
                    quranTtf.PageRichText.Visible = true;
                    quranTtf.PageRichText.Location = new Point(30, exitForm.Location.Y + exitForm.Size.Height + 5);
                    quranTtf.PageRichText.Size = new Size(Width - 60, (int)(quranTtf.PageRichText.Height * (Width - 60.0) / quranTtf.PageRichText.Width));
                    quranTtf.SetWidth();
                }
                else
                {
                    quranPic.Visible = true;
                    quranPic.Location = new Point(30, exitForm.Location.Y + exitForm.Size.Height + 5);
                    quranPic.Size = new Size(Width - 60, (int)(quranPicture.Height * (Width - 60.0) / quranPicture.Width));
                    quranPic.Select();
                }
            }
            NumberOfTimesPictureRise = 0;
        }

        // تشغيل بكرة الماوس في وضعية تكبير الصفحة
        int NumberOfTimesPictureRise = 0;
        private void QuranPic_MouseWheel(object sender, MouseEventArgs e)
        {
            if (zoom)
            {
                if (e.Delta < 0 && NumberOfTimesPictureRise < 3)
                {
                    NumberOfTimesPictureRise++;
                    if (qPicture == 2)
                        quranTtf.PageRichText.Location = new Point(quranTtf.PageRichText.Location.X, (int)(quranTtf.PageRichText.Location.Y - quranTtf.PageRichText.Size.Height * 0.25));
                    else
                        quranPic.Location = new Point(quranPic.Location.X, (int)(quranPic.Location.Y - quranPic.Size.Height * 0.25));
                }
                else if (e.Delta > 0 && NumberOfTimesPictureRise >= 0)
                {
                    NumberOfTimesPictureRise--;
                    if (qPicture == 2)
                        quranTtf.PageRichText.Location = new Point(quranTtf.PageRichText.Location.X, (int)(quranTtf.PageRichText.Location.Y + quranTtf.PageRichText.Size.Height * 0.25));
                    else
                        quranPic.Location = new Point(quranPic.Location.X, (int)(quranPic.Location.Y + quranPic.Size.Height * 0.25));

                }
            }
        }

        // List of special controls which backcolor is not subject to the form's backcolor.
        readonly List<Control> ControlsList = new List<Control>();

        private void Dark_Click(object sender, EventArgs e)
        {
            if (dark.Text == "Dark")
            {
                dark.Text = "Light";

                BackColor = Color.Black; ForeColor = Color.White;
                dark.FillColor = Color.FromArgb(191, 191, 191);
                dark.ForeColor = Color.Black;
                quranPic.BackColor = Coloring.Night.BackColor;
                quranPicture.DarkMode = true;
                quranTtf.DarkMode = true;
                quranText.DarkMode = true;
                panel.BackColor = Color.FromArgb(0, 31, 63);
                for (int i = 0; i < ControlsList.Count; i++)
                {
                    if (ControlsList[i].BackColor != Color.Transparent)
                        ControlsList[i].BackColor = BackColor;
                    ControlsList[i].ForeColor = ForeColor;
                }
                ShaykhDesc.BackColor = Color.Gray;
            }
            else
            {
                dark.Text = "Dark";

                BackColor = SystemColors.Control; ForeColor = Color.Black;
                dark.FillColor = Color.FromArgb(64, 64, 64);
                dark.ForeColor = Color.White;
                quranPic.BackColor = Coloring.Light.BackColor;
                quranPicture.DarkMode = false;
                quranTtf.DarkMode = false;
                quranText.DarkMode = false;
                panel.BackColor = Color.FromArgb(255, 224, 192);
                for (int i = 0; i < ControlsList.Count; i++)
                {
                    if (ControlsList[i].BackColor != Color.Transparent)
                        ControlsList[i].BackColor = SystemColors.Window;
                    ControlsList[i].ForeColor = ForeColor;
                    if (ControlsList[i] is Button button) button.UseVisualStyleBackColor = true;
                }
                ShaykhDesc.BackColor = BackColor;
                ShaykhDesc.UseVisualStyleBackColor = true;
            }
            if (qPicture == 1) quranPic.Invalidate();
        }
        #endregion

        #region أزرار المشايخ
        // اضافة ازرار المشايخ
        void AddMashaykhButtons()
        {
            string audioFolder = Path.Combine(Application.StartupPath, "audios\\");
            bool distinct = false;
            string[] stringArray;

            #region read favourite.txt file
            string temp = audioFolder + "favourite.txt";
            if (!File.Exists(temp)) return;
            List<string> audioFolders = File.ReadAllLines(temp).SelectMany(line => line.Split('*')).Select(item => item.Trim()).Where(item => item != "").ToList();
            if (audioFolders.Count == 0) return;
            for (int i = 0; i < audioFolders.Count; i++)
            {
                stringArray = audioFolders[i].Split('|');
                temp = stringArray[0].Trim();
                if (temp.Length == 0)
                {
                    audioFolders.RemoveAt(i);
                    i--;
                }
                else if (Directory.Exists(audioFolder + temp))
                {
                    audioFolders[i] = Path.GetFullPath(audioFolder + audioFolders[i]);
                }
                else if (Directory.Exists(temp))
                {
                    audioFolders[i] = Path.GetFullPath(audioFolders[i]);
                }
                else if (temp == ":distinct:")
                {
                    distinct = true;
                    audioFolders.RemoveAt(i);
                    i--;
                }
                else if (temp != ":space:" && temp != ":line:")
                {
                    audioFolders.RemoveAt(i);
                    i--;
                }
            }
            #endregion

            #region get all folders from audios folder
            List<string> list = Directory.GetDirectories(audioFolder).ToList();
            if (distinct)
            {
                for (int i = 0; i < audioFolders.Count; i++)
                {
                    if (audioFolders[i][0] == ':') continue;
                    temp = audioFolders[i].Split('|').First().Trim('\\', '/');
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (temp == list[j])
                        {
                            list.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            audioFolders.AddRange(list);
            #endregion

            #region add mashaykh buttons in the panel
            Color clr;
            int y = -45;
            Guna2Button b;
            panel.Controls.Clear();
            Random rand = new Random();
            for (int i = 0; i < audioFolders.Count; i++)
            {
                stringArray = audioFolders[i].Split('|');
                temp = stringArray[0].Trim();
                if (temp == ":space:") { y += 5; continue; }
                y += 50;
                if (stringArray.Length > 1 && stringArray[1].Trim() != "") clr = Coloring.GetColor(stringArray[1].Trim());
                else clr = Color.FromArgb(rand.Next(0, 256), rand.Next(0, 200), rand.Next(0, 256));
                b = new Guna2Button
                {
                    FillColor = clr,
                    Location = new Point(fs.GetNewX(3), fs.GetNewY(y)),
                    BorderRadius = 15
                };
                if (stringArray.Length > 2 && stringArray[2].Trim() != "") b.ForeColor = Coloring.GetColor(stringArray[2].Trim());
                if (stringArray.Length > 3 && stringArray[3].Trim() != "") b.Text = stringArray[3];
                if (temp == ":line:")
                {
                    b.Size = new Size(fs.GetNewX(243), fs.GetNewY(10));
                    y -= 35;
                }
                else
                {
                    if (b.Text == "") b.Text = temp.Trim('\\', '/').Split('\\').Last();
                    b.Font = new Font("Segoe UI", fs.GetNewX(12));
                    b.Size = new Size(fs.GetNewX(243), fs.GetNewY(45));
                    b.Cursor = Cursors.Hand;
                    b.Tag = temp;
                    b.KeyUp += Button_KeyUp;
                    b.MouseClick += Button1_MouseClick;
                }
                panel.Controls.Add(b);
            }
            #endregion
        }

        private void Button_KeyUp(object sender, KeyEventArgs e)
        {
            string s = (string)((Guna2Button)sender).Tag;
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                QuranAudioStart(s);
            else if (e.KeyCode == Keys.D)
                OpenDownloadForm(s);
        }

        private void Button1_MouseClick(object sender, MouseEventArgs e)
        {
            string s = (string)((Guna2Button)sender).Tag;
            if (e.Button == MouseButtons.Right)
                OpenDownloadForm(s);
            else if (e.Button == MouseButtons.Left)
                QuranAudioStart(s);
        }

        private void OpenDownloadForm(string s)
        {
            if (downloadForm is null)
                downloadForm = new DownloadAudioQuranFiles();

            downloadForm.Owner = this;
            downloadForm.InitializeAudioFile(s);
            downloadForm.Show();
            downloadForm.WindowState = FormWindowState.Normal;
        }

        private void QuranAudioStart(string s)
        {
            if (string.Equals(moshafAudio, s, StringComparison.OrdinalIgnoreCase) &&
                quranAudio.Resume((int)Surah.Value, (int)Ayah.Value))
            {
                pause.Text = "||";
                return;
            }
            moshafAudio = s;
            int curr = quranAudio.CurrentWord;
            quranAudio.Start(s, (int)Surah.Value, (int)Ayah.Value);
            if (curr > 1) quranAudio.WordOf(curr);
            pause.Text = "||";
            time5.Text = quranAudio.GetCurrentPosition();
            folder.SelectedPath = s;
        }
        #endregion

        #region التعامل مع التنقلات في المصحف
        private bool updatingNavigationControls;
        private int displayedSurah, displayedAyah;

        private void UseVisual(IVisualQuran visual)
        {
            InvalidateTafseer();
            quranVisual = visual;
            reading = new ReadingNavigator(visual);
            displayedSurah = (int)Surah.Value;
            displayedAyah = (int)Ayah.Value;
        }

        private void Surahs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!updatingNavigationControls && Surahs.SelectedIndex >= 0)
                Surah.Value = Surahs.SelectedIndex + 1;
        }

        private void Surah_ValueChanged(object sender, EventArgs e)
        {
            if (updatingNavigationControls || reading == null || !reading.GoToSurah((int)Surah.Value)) return;
            ApplyReadingPosition();
        }

        private void Juz_ValueChanged(object sender, EventArgs e) /* فهرس بالأجزاء */
        {
            if (updatingNavigationControls || reading == null || !reading.GoToJuz((int)Juz.Value)) return;
            ApplyReadingPosition();
        }

        private void Hizb_ValueChanged(object sender, EventArgs e) /* فهرس بالحزب */
        {
            if (updatingNavigationControls || reading == null || !reading.GoToHizb((int)Hizb.Value)) return;
            ApplyReadingPosition();
        }

        private void Quarter_ValueChanged(object sender, EventArgs e) /* فهرس بالربع */
        {
            if (updatingNavigationControls || reading == null || !reading.GoToQuarter((int)Quarter.Value)) return;
            ApplyReadingPosition();
        }

        private void Page_ValueChanged(object sender, EventArgs e) /* فهرس بالصفحات */
        {
            if (updatingNavigationControls || reading == null || !reading.GoToPage((int)Page.Value)) return;
            ApplyReadingPosition();
        }

        private void ApplyReadingPosition(bool syncAudio = true)
        {
            if (reading == null) return;
            ReadingPosition position = reading.Position;
            bool surahChanged = displayedSurah != position.Surah;
            updatingNavigationControls = true;
            try
            {
                Surah.Value = position.Surah;
                Surahs.SelectedIndex = position.Surah - 1;
                Ayah.Maximum = position.AyatCount;
                Ayah.Value = position.Ayah;
                Quarter.Value = position.Quarter;
                Juz.Value = position.Juz;
                Hizb.Value = position.Hizb;
                Page.Value = position.Page;
            }
            finally
            {
                updatingNavigationControls = false;
            }
            displayedSurah = position.Surah;
            if (surahChanged || displayedAyah != position.Ayah) InvalidateTafseer();
            displayedAyah = position.Ayah;

            if (surahChanged && addNewMoqrea.Text == "إلغاء")
                EditMoqreaSurah(position.Surah);

            if (qPicture == 1) quranPic.Invalidate();

            if (syncAudio)
            {
                if (quranAudio.Set(position.Surah, position.Ayah)) pause.Text = "||";
                if (position.Word > 0)
                    quranAudio.WordOf(position.Word);
            }

            time5.Text = quranAudio.GetCurrentPosition();
        }

        private void Ayah_ValueChanged(object sender, EventArgs e)
        {
            if (updatingNavigationControls || reading == null || !reading.GoToAyah((int)Surah.Value, (int)Ayah.Value)) return;
            ApplyReadingPosition();
        }

        private void AyahAudio(object sender, EventArgs e)
        {
            if (!updatingNavigationControls && reading != null && reading.GoToAyah(quranAudio.SurahNumber, quranAudio.AyahNumber))
                ApplyReadingPosition(syncAudio: false);
        }

        // quranAudios -> AddEventHandlerOfWord
        private void WordAudio(object sender, EventArgs e)
        {
            if (reading == null) return;
            ReadingPosition position = reading.Position;
            if (position.Surah != quranAudio.SurahNumber || position.Ayah != quranAudio.AyahNumber) return;
            reading.GoToWord(quranAudio.CurrentWord);
            if (qPicture == 1) quranPic.Invalidate();
        }

        private async void QuranPic_MouseClick(object sender, MouseEventArgs e)
        {
            if (!updatingNavigationControls
                && quranPicture.SetXY(MousePosition.X - quranPic.Location.X - Location.X,
                                    MousePosition.Y - quranPic.Location.Y - Location.Y, quranPic.Width, quranPic.Height) )
            {
                ApplyReadingPosition();
                if (e.Button == MouseButtons.Right) // التفسير
                    await ReadTafseerAsync(copy: false);
            }
        }

        // بديل عن المصحف المصور، ربما لن تراه في حياتك
        // quranTexts -> AddEventHandler
        private void PageQuranText_Click(object sender, EventArgs e)
        {
            if (!updatingNavigationControls && quranText.SetCursor()) ApplyReadingPosition();
        }

        private void PageFontQuranText_Click(object sender, EventArgs e)
        {
            if (!updatingNavigationControls && quranTtf.SetCursor()) ApplyReadingPosition();
        }
        #endregion

        #region نسخ القرآن وتفسيره والبحث فيه
        // نسخ الآية
        private void Copy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(quranText.AyahAbstractText((int)Surah.Value, (int)Ayah.Value) + "\n\n\n" + quranText.AyahText((int)Surah.Value, (int)Ayah.Value));
            }
            catch { }
        }

        private void AddTafseerSources(string folder, string heading)
        {
            if (!Directory.Exists(folder)) return;
            string[] files = Directory.GetFiles(folder, "*.db");
            if (files.Length == 0) return;
            tafasir.Items.Add(heading);
            foreach (string file in files.OrderBy(Path.GetFileName, StringComparer.CurrentCulture))
                tafasir.Items.Add(new TafseerQuran(file, Path.Combine(folder, Path.GetFileName(file))));
        }

        private void Tafasir_SelectedIndexChanged(object sender, EventArgs e)
        {
            InvalidateTafseer();
            tafseerSource = tafasir.SelectedItem as TafseerQuran;
            tafseer = tafseerSource?.Key ?? "";
        }

        private void CloseTafseerPreview()
        {
            Form preview = tafseerPreview;
            tafseerPreview = null;
            preview?.Close();
        }

        private void InvalidateTafseer()
        {
            tafseerPreviewRequest.Cancel();
            tafseerCopyRequest.Cancel();
            CloseTafseerPreview();
        }

        private async void TafseerCopy_Click(object sender, EventArgs e) => await ReadTafseerAsync(copy: true);

        private async void SaveRTF_Click(object sender, EventArgs e) => await ReadTafseerAsync(copy: false);

        private async Task ReadTafseerAsync(bool copy)
        {
            TafseerQuran source = tafseerSource;
            if (source == null)
            {
                MessageBox.Show(this, "اختار تفسير أو ترجمة من القائمة أولًا.", "التفسير والترجمة");
                return;
            }
            if (reading == null) return;
            ReadingPosition position = reading.Position;
            LatestTafseerRequest request = copy ? tafseerCopyRequest : tafseerPreviewRequest;
            if (!copy) CloseTafseerPreview();
            await request.RunAsync(
                cancellation => source.AyahTextAsync(position.Surah, position.Ayah, cancellation),
                text =>
                {
                    if (IsDisposed || Disposing) return;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        MessageBox.Show(this, "المصدر المختار مفيهوش نص للآية دي.", "التفسير والترجمة");
                        return;
                    }
                    if (copy) copyTafseerText(text);
                    else ShowTafseerPreview(source, position, text);
                },
                error =>
                {
                    if (!IsDisposed && !Disposing)
                        MessageBox.Show(this, (copy ? "تعذّر نسخ النص." : "تعذّر فتح النص.") + Environment.NewLine + error.Message, "التفسير والترجمة");
                });
        }

        private void ShowTafseerPreview(TafseerQuran source, ReadingPosition position, string text)
        {
            string surahName = position.Surah > 0 && position.Surah <= Surahs.Items.Count
                ? Surahs.Items[position.Surah - 1].ToString()
                : $"سورة {position.Surah}";
            var preview = new Form
            {
                Text = $"{source.Name} — {surahName} ({position.Ayah})",
                StartPosition = FormStartPosition.CenterParent, Size = new Size(680, 380),
                ShowInTaskbar = false, MinimizeBox = false, KeyPreview = true
            };
            char firstLetter = text.FirstOrDefault(char.IsLetter);
            var body = new RichTextBox
            {
                Dock = DockStyle.Fill, Font = saveRTF.Font,
                RightToLeft = firstLetter >= '\u0590' && firstLetter <= '\u08FF' ? RightToLeft.Yes : RightToLeft.No,
                BackColor = SystemColors.Window, DetectUrls = false
            };
            preview.Controls.Add(body);
            preview.Load += (s, e) =>
            {
                body.Text = text;
                body.ReadOnly = true;
            };
            preview.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) preview.Close(); };
            preview.FormClosed += (s, e) => { if (tafseerPreview == preview) tafseerPreview = null; };
            tafseerPreview = preview;
            preview.Show(this);
        }

        // البحث بالكلمات في القرآن
        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && searchCancellation == null)
            {
                e.SuppressKeyPress = true;
                search.PerformClick();
            }
            else if (e.KeyCode == Keys.Escape && searchList.Visible)
            {
                e.SuppressKeyPress = true;
                searchClose.PerformClick();
            }
        }

        private void CancelSearch()
        {
            CancellationTokenSource request = searchCancellation;
            if (request == null) return;

            searchCancellation = null;
            request.Cancel();
            search.Text = "بحث";
        }

        private async void Search_Click(object sender, EventArgs e)
        {
            if (searchCancellation != null)
            {
                CancelSearch();
                searchList.Items.Clear();
                searchList.Items.Add("تم إلغاء البحث");
                return;
            }

            if (searchService == null)
            {
                MessageBox.Show("لا يوجد مصحف مكتوب للبحث.");
                return;
            }

            var request = new CancellationTokenSource();
            searchCancellation = request;
            searchResults = new QuranSearchResult[0];
            searchList.Items.Clear();
            searchList.Items.Add("جار البحث...");
            searchList.Visible = true;
            searchClose.Visible = true;
            search.Text = "إلغاء البحث";

            try
            {
                QuranSearchResult[] results = await searchService.SearchAsync(searchText.Text, spellingErrors.Checked, request.Token);
                if (searchCancellation != request || request.IsCancellationRequested || IsDisposed) return;

                searchResults = results;
                searchList.BeginUpdate();
                try
                {
                    searchList.Items.Clear();
                    searchList.Items.AddRange(results.Length == 0
                        ? new object[] { "لا توجد نتائج" }
                        : results.Select(result => (object)SearchResultLabel(result)).ToArray());
                }
                finally { searchList.EndUpdate(); }
            }
            catch (OperationCanceledException) when (request.IsCancellationRequested) { }
            catch (Exception ex)
            {
                if (!IsDisposed && searchCancellation == request)
                {
                    searchList.Items.Clear();
                    searchList.Items.Add("تعذر إتمام البحث");
                    MessageBox.Show("تعذر إتمام البحث: " + ex.Message);
                }
            }
            finally
            {
                if (!IsDisposed && searchCancellation == request)
                {
                    searchCancellation = null;
                    search.Text = "بحث";
                }
                request.Dispose();
            }
        }

        private string SearchResultLabel(QuranSearchResult result)
        {
            int index = result.Surah - 1;
            string surah = index >= 0 && index < Surahs.Items.Count
                ? Surahs.Items[index].ToString()
                : $"سورة {result.Surah}";
            return $"{surah} ({result.Ayah}): {result.Preview}";
        }

        // إختيار آية من قائمة البحث
        private void SearchList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = searchList.SelectedIndex;
            if (index < 0 || index >= searchResults.Length) return;
            QuranSearchResult result = searchResults[index];
            if (reading != null && reading.GoToAyah(result.Surah, Math.Max(result.Ayah, (int)Ayah.Minimum)))
                ApplyReadingPosition();
        }

        // إغلاق قائمة البحث
        private void SearchClose_Click(object sender, EventArgs e)
        {
            CancelSearch();
            searchList.Visible = false;
            searchClose.Visible = false;
            searchText.Focus();
        }

        // إنشاء ملف ترجمة
        private async void SrtFile_Click(object sender, EventArgs e)
        {
            TafseerQuran source = tafseerSource;
            if (source == null)
            {
                MessageBox.Show(this, "اختار تفسير أو ترجمة من القائمة أولًا.", "تصدير SRT");
                return;
            }
            if (reading == null) return;
            int surah = reading.Position.Surah;
            bool currentPage = pageSrtCheck.Checked;
            srtFile.Enabled = false;
            try
            {
                int first = 0, last = 287;
                if (currentPage)
                {
                    int[] range = quranVisual == quranPicture ? quranPicture.GetStartAndEndOfPage() :
                        quranVisual == quranTtf ? quranTtf.GetStartAndEndOfPage() : quranText.GetStartAndEndOfPage();
                    if (range == null || range[0] < 1)
                    {
                        MessageBox.Show(this, "مفيش آيات من السورة دي في الصفحة الحالية.", "تصدير SRT");
                        return;
                    }
                    first = range[0];
                    last = range[1];
                }

                var timings = quranAudio.GetSubtitleTimings(surah, first, last);
                string subtitles = await source.SubRipTextAsync(surah, timings, relativeToFirstAyah: currentPage);
                if (IsDisposed || Disposing) return;
                if (subtitles.Length == 0)
                {
                    MessageBox.Show(this, "مفيش نص وتوقيتات تلاوة متطابقة للتصدير. تأكّد من اختيار القارئ والمصدر.", "تصدير SRT");
                    return;
                }
                saveSRTFile.FileName = $"Surah {surah:D3}.srt";
                if (saveSRTFile.ShowDialog(this) == DialogResult.OK)
                    File.WriteAllText(saveSRTFile.FileName, subtitles, Encoding.UTF8);
            }
            catch (Exception error)
            {
                if (!IsDisposed && !Disposing)
                    MessageBox.Show(this, "تعذّر تصدير الملف." + Environment.NewLine + error.Message, "تصدير SRT");
            }
            finally
            {
                if (!IsDisposed && !Disposing) srtFile.Enabled = true;
            }
        }
        #endregion

        #region التعامل مع الصوت
        // تفعيل التكرار
        private void Repeat_CheckedChanged(object sender, EventArgs e)
            => quranAudio.Repeat(SurahRepeatCheck.Checked ? (int)SurahRepeat.Value : 1, AyahRepeatCheck.Checked ? (int)AyahRepeat.Value : 1);


        private void Pause_Click(object sender, EventArgs e)
        {
            if (quranAudio.Resume((int)Surah.Value, (int)Ayah.Value)) pause.Text = "||";
            else if (quranAudio.Pause()) pause.Text = "▶";
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            quranAudio.Stop();
            pause.Text = "||";
        }

        // سرعة الصوت
        private void Rate_ValueChanged(object sender, EventArgs e) => quranAudio.Rate = (double)Rate.Value;

        // مستوى إرتفاع الصوت
        private void Volume_ValueChanged(object sender, EventArgs e) => quranAudio.Volume = volume.Value;

        // تجميع الآيات المقطعة
        private Task audioExportTask;

        private async void Combiner_Click(object sender, EventArgs e)
        {
            using (var folder = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = "اختر مجلد الآيات المقسّمة. استخدم صيغة واحدة (WAV أو MP3) وأسماء مثل S002A003.wav أو 002003.mp3."
            })
            {
                if (folder.ShowDialog(this) != DialogResult.OK) return;
                string input = folder.SelectedPath;
                await RunAudioExportAsync(() =>
                {
                    AudioCombineResult result = AudioQuranCombiner.MushafCombine(input);
                    if (result == null) return "لم يُعثر على ملفات آيات مدعومة في المجلد المختار.";
                    string summary = result.Folder == null ? "لم تُنشأ ملفات صوتية." : "حُفظت السور المدمجة في:\n" + result.Folder;
                    if (result.Errors.Length > 0)
                    {
                        summary += "\n\nتعذّر دمج بعض الملفات:\n" + string.Join("، ", result.Errors.Take(20));
                        if (result.Errors.Length > 20) summary += $" (+{result.Errors.Length - 20})";
                    }
                    return summary;
                });
            }
        }

        // تقطيع السورة لآيات
        private async void SplitAll_Click(object sender, EventArgs e)
        {
            try
            {
                AudioQuranSplitter split = quranAudio.PrepareSurahSplit();
                if (split == null)
                {
                    MessageBox.Show(this, "اختر تسجيلًا للسورة الحالية قبل تقسيمها.", "تقسيم السورة");
                    return;
                }

                string output = Path.GetFullPath(Path.Combine("splits", $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
                await RunAudioExportAsync(() =>
                {
                    int count = split.Export(output, out int skipped);
                    return count == 0 ? "لا توجد آيات بتوقيتات صالحة للتصدير."
                        : $"حُفظ {count} مقطع WAV في:\n{output}"
                        + (skipped == 0 ? "" : $"\nتم تخطي {skipped} آية بسبب توقيت ناقص أو غير صالح.");
                });
            }
            catch (Exception ex) { ShowAudioExportError(ex); }
        }

        private async Task RunAudioExportAsync(Func<string> work)
        {
            combiner.Enabled = splitAll.Enabled = false;
            try
            {
                Task<string> operation = Task.Run(work);
                audioExportTask = operation;
                string summary = await operation;
                if (!IsDisposed) MessageBox.Show(this, summary, "تصدير الصوت");
            }
            catch (Exception ex) { ShowAudioExportError(ex); }
            finally
            {
                audioExportTask = null;
                if (!IsDisposed) combiner.Enabled = splitAll.Enabled = true;
            }
        }

        private void ShowAudioExportError(Exception ex)
        {
            Trace.TraceWarning("Audio export failed: {0}", ex.Message);
            if (!IsDisposed)
                MessageBox.Show(this, ex is NotSupportedException ? ex.Message : "تعذّر تصدير الصوت. تأكّد من سلامة الملفات ومن إمكانية الكتابة في مجلد الإخراج.",
                    "تصدير الصوت", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        #endregion

        #region إضافة شيخ جديد
        // إضافة أدوات إضافة الشيخ الجديد
        void EditMoqreaSurah(int sura)
        {
            panel.Controls.Clear();
            int[] ayat = quranAudio.GetTimestamps(sura);
            string[] ayatS = quranText.SurahAbstractTexts(sura, 5, endAyatCheck.Checked);
            if (ayat == null) return;

            Label label; NumericUpDown num; Button btn;
            Font font = new Font("Tahoma", fs.GetNewX(13));
            Size labelSize = new Size(fs.GetNewX(257), fs.GetNewY(33));
            Size numSize = new Size(fs.GetNewX(120), fs.GetNewY(24));
            Size btnSize = new Size(fs.GetNewX(31), fs.GetNewY(23));
            Color clr = dark.Text == "Dark" ? Color.FromArgb(255, 204, 172) : Color.FromArgb(0, 51, 83);
            Color Fclr = dark.Text == "Dark" ? Color.Black : Color.White;
            int y = 28;
            int k;
            for (int i = 0; i < ayat.Length; i++)
            {

                if (Ayah.Minimum == 0 || i == 0) k = i - 1;
                else k = i;

                label = new Label
                {
                    Font = font,
                    Location = new Point(fs.GetNewX(3), fs.GetNewY(y)),
                    RightToLeft = RightToLeft.Yes,
                    Size = labelSize,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag = endAyatCheck.Checked ? k : Ayah.Minimum == 0 ? i : i + 1
                };
                if (endAyatCheck.Checked)
                {
                    if (i != 0) label.Text = ayatS[i];
                    else label.Text = "الإستعاذة";
                }
                else
                {
                    if (i != ayat.Length - 1) label.Text = ayatS[i + 1];
                    else label.Text = "نهاية آخر آية";
                }
                if (endAyatCheck.Checked && i != 0 || !endAyatCheck.Checked && i != ayat.Length - 1) label.Click += AyahLabel_Click;
                panel.Controls.Add(label);

                num = new NumericUpDown
                {
                    BackColor = clr,
                    ForeColor = Fclr,
                    BorderStyle = BorderStyle.None,
                    DecimalPlaces = 3,
                    Font = font,
                    Location = new Point(fs.GetNewX(35), fs.GetNewY(y + 37)),
                    Size = numSize,
                    Tag = k,
                    TextAlign = HorizontalAlignment.Center,
                    Minimum = 0,
                    Maximum = 99999,
                    Increment = 0.1M,
                    Value = ayat[i] / 1000M
                };
                num.MouseWheel += Timestamp_MouseWheel;
                num.ValueChanged += Timestamp_ValueChanged;
                panel.Controls.Add(num);

                btn = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Size = btnSize,
                    Tag = i * 3 + 1,
                    Location = new Point(fs.GetNewX(165), fs.GetNewY(y + 37)),
                    ForeColor = Color.Red,
                    Text = "O"
                };
                btn.Click += Timestamp_record;
                panel.Controls.Add(btn);

                y += 86;
            }

            btn = new Button
            {
                FlatStyle = FlatStyle.Flat,
                Size = btnSize,
                Tag = ayat.Length * 3 - 2,
                Location = new Point(fs.GetNewX(200), fs.GetNewY(y - 49)),
                ForeColor = Color.Blue,
                Text = "O"
            };
            btn.Click += Mp3Duration_record;
            panel.Controls.Add(btn);
        }

        // استدعاء الآية عند الضغط عليها
        void AyahLabel_Click(object sender, EventArgs e) => Ayah.Value = (int)((Label)sender).Tag;

        int tempInt;
        void Timestamp_ValueChanged(object sender, EventArgs e)
        {
            tempInt = (int)((NumericUpDown)sender).Tag;
            quranAudio.SetAyah((int)Surah.Value, tempInt, (int)(((NumericUpDown)sender).Value * 1000));
            if (tempInt >= Ayah.Minimum && tempInt != Ayah.Maximum && timestampChangeEventCheck.Checked)
                if (Ayah.Value != tempInt + 1) Ayah.Value = tempInt + 1;
                else Ayah_ValueChanged(sender, e);
        }

        private void Timestamp_MouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
            var num = (NumericUpDown)sender;
            tempInt = (int)num.Tag;
            if (tempInt >= Ayah.Minimum && tempInt != Ayah.Maximum)
            {
                decimal decml = ((NumericUpDown)panel.Controls[3 * (tempInt - (int)Ayah.Minimum) + 1]).Value + 0.5M;
                if (decml > 0.5M || tempInt <= 1)
                {
                    if (num.Value < decml) num.Value = decml;
                    if (e.Delta > 0) num.Value += 3;
                    else if (num.Value >= decml + 3) num.Value -= 3;
                }
            }
        }

        void Timestamp_record(object sender, EventArgs e)
        {
            double temp = quranAudio.Mp3CurrentPosition();
            if (temp == 0) return;
            ((NumericUpDown)panel.Controls[(int)((Button)sender).Tag]).Value = (decimal)temp;
        }

        void Mp3Duration_record(object sender, EventArgs e)
        {
            double temp = quranAudio.Mp3Duration();
            if (temp == 0) return;
            ((NumericUpDown)panel.Controls[(int)((Button)sender).Tag]).Value = (decimal)temp;
        }

        // زر تشغيل وضعية إضافة مقرئ جديد
        private void AddNewMoqrea_Click(object sender, EventArgs e)
        {
            if (addNewMoqrea.Text != "إلغاء" && folder.ShowDialog() == DialogResult.OK && quranAudio.NewQuranAudio(folder.SelectedPath))
            {
                addNewMoqrea.Text = "إلغاء"; stop.Enabled = false;
                moshafAudio = folder.SelectedPath;
                combiner.Visible = false; splitAll.Visible = false;
                videoEditor.Visible = false;
                youtubeCaptions.Visible = false;
                timestampChangeEventCheck.Visible = true;
                endAyatCheck.Visible = true;
                ShaykhDesc.Enabled = true; addShaykhInfo.Enabled = true;
                EditMoqreaSurah((int)Surah.Value);
            }
            else
            {
                AddMashaykhButtons();
                addNewMoqrea.Text = "إضافة شيخ جديد"; stop.Enabled = true;
                timestampChangeEventCheck.Visible = false;
                endAyatCheck.Visible = false;
                ShaykhDesc.Enabled = false; addShaykhInfo.Enabled = false;
                combiner.Visible = true; splitAll.Visible = true;
                videoEditor.Visible = true;
                youtubeCaptions.Visible = true;
            }
        }

        // عند التفعيل سوف ترى نهاية الآيات بدل بدايتها
        private void EndAyatCheck_CheckedChanged(object sender, EventArgs e) => EditMoqreaSurah((int)Surah.Value);

        private void AddShaykhInfo_Click(object sender, EventArgs e)
        {
            if (endAyatCheck.Checked) MessageBox.Show("هذه التوقيتات هي لنهاية الآيات وليس بدايتها");
            else MessageBox.Show($"هذه التوقيتات هي لبداية الآيات إلا آخر توقيت");
        }

        private void DescSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (quranAudio.SetDescription(extension.Text, comment.Text)) return;
            }
            catch (SQLiteException ex)
            {
                Trace.TraceWarning("Could not save recitation description: {0}", ex.Message);
            }

            MessageBox.Show(this, "تعذّر حفظ وصف التلاوة. تأكّد من إمكانية الكتابة في مجلد القارئ ثم حاول مرة أخرى.",
                "حفظ الوصف", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // إضافة وصف للشيخ
        private void ShaykhDesc_(object sender, EventArgs e)
        {
            if (ShaykhDesc.Text == "الوصف" && ShaykhDesc.Enabled == true)
            {
                ShaykhDesc.Text = "إلغاء";
                tafasir.Visible = false;
                tafseerCopy.Visible = false;
                saveRTF.Visible = false;
                srtFile.Visible = false;
                pageSrtCheck.Visible = false;
                lExt.Visible = true;
                extension.Text = quranAudio.Extension;
                extension.Visible = true;
                lComment.Visible = true;
                comment.Text = quranAudio.Comment;
                comment.Visible = true;
                descSave.Visible = true;
            }
            else if (ShaykhDesc.Text == "إلغاء")
            {
                ShaykhDesc.Text = "الوصف";
                lExt.Visible = false;
                extension.Visible = false;
                lComment.Visible = false;
                comment.Visible = false;
                descSave.Visible = false;
                tafasir.Visible = true;
                tafseerCopy.Visible = true;
                saveRTF.Visible = true;
                srtFile.Visible = true;
                pageSrtCheck.Visible = true;
            }

        }
        #endregion

        private DiscriminatorsForm discriminatorsForm = null;
        private void Discri_Click(object sender, EventArgs e)
        {
            if (discriminatorsForm == null)
            {
                discriminatorsForm = new DiscriminatorsForm();
                discriminatorsForm.FormClosedWithYesResult += DiscriminatorsForm_Closed;
            }
            discriminatorsForm.InitializeDiscriminator();
            discriminatorsForm.Owner = this;
            discriminatorsForm.Show();
            discriminatorsForm.WindowState = FormWindowState.Normal;
        }

        private void DiscriminatorsForm_Closed(object sender, EventArgs e)
        {
            if (qPicture == 1)
            {
                quranPicture.SetInitialColors();
                quranPicture.SetDiscriminators();
                if (dark.Text == "Dark")
                    quranPic.BackColor = Coloring.Light.BackColor;
                else
                    quranPic.BackColor = Coloring.Night.BackColor;
            }
            else if (qPicture == 2)
            {
                quranTtf.SetInitialColors();
                quranTtf.SetDiscriminators();
                if (dark.Text == "Dark")
                    quranTtf.PageRichText.BackColor = Coloring.Light.BackColor;
                else
                    quranTtf.PageRichText.BackColor = Coloring.Night.BackColor;
            }
            else
            {
                quranText.DarkMode = !quranText.DarkMode;
                quranText.DarkMode = !quranText.DarkMode;
            }
        }

        private void VideoEditor_Click(object sender, EventArgs e) => new VideoEditorForm(qPicture).ShowDialog();
        
        private void YoutubeCaptions_Click(object sender, EventArgs e) => new YoutubeCaptionsForm(qPicture).ShowDialog();

        private void PreventWordSelection_CheckedChanged(object sender, EventArgs e)
        {
            bool isAllow = !preventWordSelection.Checked;
            quranText.WordMode = isAllow;
            quranPicture.WordMode = isAllow;
            quranTtf.WordMode = isAllow;
            quranAudio.WordMode = isAllow;
            if (!isAllow && reading != null)
            {
                reading.GoToWord(-1);
                if (qPicture == 1) quranPic.Invalidate();
            }
        }

        private void Latest_Click(object sender, EventArgs e) => Process.Start("https://github.com/mohamedashref371/QuranKareem");

        private void About_Click(object sender, EventArgs e) => Process.Start("https://youtube.com/channel/UCS2-gBRB3I7K6xA-Vuf-lGQ");

        private void Guna2HtmlLabel1_Click(object sender, EventArgs e) => About_Click(null, null);

    }
}
