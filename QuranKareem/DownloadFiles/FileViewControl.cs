using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QuranKareem
{
    internal class FileViewControl : Control
    {
        private readonly CheckBox checkBox = new CheckBox
        {
            Location = new Point(759, 19),
            Size = new Size(15, 14),
            TabIndex = 0,
            UseVisualStyleBackColor = true
        };

        private readonly Label fileName = new Label
        {
            Font = new Font("Tahoma", 12F),
            Location = new Point(568, 9),
            //RightToLeft = RightToLeft.Yes,
            Size = new Size(185, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };

        private readonly Label folderName = new Label
        {
            Font = new Font("Tahoma", 12F),
            Location = new Point(250, 9),
            RightToLeft = RightToLeft.Yes,
            Size = new Size(312, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };

        private readonly Button downloadBtn = new Button
        {
            BackColor = Color.FromArgb(192, 255, 192),
            FlatStyle = FlatStyle.Popup,
            Font = new Font("Tahoma", 12F),
            Location = new Point(139, 9),
            RightToLeft = RightToLeft.Yes,
            Size = new Size(105, 32),
            TabIndex = 1,
            Text = "تحميل",
            UseVisualStyleBackColor = false
        };

        private readonly Label status = new Label
        {
            Font = new Font("Tahoma", 12F),
            Location = new Point(14, 9),
            RightToLeft = RightToLeft.Yes,
            Size = new Size(119, 32),
            TextAlign = ContentAlignment.MiddleCenter
        };

        public static readonly Size ControlSize = new Size(782, 50);

        public event EventHandler DownloadButtonClick;

        public readonly Uri FileLink;
        public readonly string FilePath;
        public readonly string FileName, FolderName;

        public bool Checked
        {
            get => checkBox.Checked;
            set
            {
                if (checkBox.Enabled)
                    checkBox.Checked = value;
            }
        }

        public bool IsForce { get; private set; }

        public void Initialize()
        {
            ClientSize = ControlSize;
            Controls.Add(status);
            Controls.Add(downloadBtn);
            Controls.Add(folderName);
            Controls.Add(fileName);
            Controls.Add(checkBox);
            downloadBtn.Tag = this;
        }

        public Status Status { set => UpdateUI(value); }

        private void UpdateUI(Status _status)
        {
            switch (_status)
            {
                case Status.NotExist:
                    status.Text = "";
                    status.ForeColor = Color.FromArgb(0, 0, 0);
                    checkBox.Enabled = true;
                    downloadBtn.Enabled = true;
                    checkBox.Checked = true;
                    break;
                case Status.Waiting:
                    status.Text = "ينتظر";
                    status.ForeColor = Color.FromArgb(200, 150, 0);
                    checkBox.Checked = false;
                    checkBox.Enabled = false;
                    downloadBtn.Enabled = false;
                    break;
                case Status.Ready:
                    status.Text = "يستعد";
                    status.ForeColor = Color.FromArgb(0, 150, 200);
                    break;
                case Status.Downloading:
                    status.Text = "جارٍ التحميل";
                    status.ForeColor = Color.FromArgb(0, 150, 200);
                    break;
                case Status.Downloaded:
                    status.Text = "تم التحميل";
                    status.ForeColor = Color.FromArgb(0, 0, 190);
                    checkBox.Enabled = false;
                    downloadBtn.Enabled = true;
                    break;
                case Status.Exist:
                    status.Text = "موجود";
                    status.ForeColor = Color.FromArgb(0, 150, 0);
                    checkBox.Enabled = false;
                    downloadBtn.Enabled = true;
                    break;
                case Status.Error:
                case Status.Canceled:
                    status.Text = _status == Status.Error ? "فشل التحميل" : "أُلغي التحميل";
                    status.ForeColor = _status == Status.Error ? Color.Firebrick : SystemColors.ControlText;
                    checkBox.Enabled = !File.Exists(FilePath);
                    downloadBtn.Enabled = true;
                    break;
            }
        }

        public FileViewControl(EventHandler checkedChanged, EventHandler click)
        {
            checkBox.CheckedChanged += checkedChanged;
            downloadBtn.Click += click;
            checkBox.CheckState = CheckState.Checked;
            fileName.Text = "إسم الملف";
            folderName.Text = "إسم المجلد";
            status.Text = "الحالة";
            Initialize();
        }

        public FileViewControl(Uri link, string filePath, string _fileName, string _folderName, bool isExist)
        {
            FileLink = link; FilePath = filePath;
            FileName = _fileName; FolderName = _folderName;
            fileName.Text = _fileName; folderName.Text = _folderName;

            Status = isExist ? Status.Exist : Status.NotExist;

            downloadBtn.Click += Clicked;
            Initialize();
        }

        public void Clicked(object sender, EventArgs e)
        {
            if (!downloadBtn.Enabled) return;
            IsForce = File.Exists(FilePath);
            if (IsForce && !AreYouSure()) return;
            DownloadButtonClick?.Invoke(downloadBtn, e);
        }

        private static bool AreYouSure() => MessageBox.Show("الملف موجود بالفعل، هل تريد إعادة تحميله؟", "؟!?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
