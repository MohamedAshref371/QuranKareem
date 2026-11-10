using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuranKareem
{
    internal sealed class FileDownloadingControl : Control
    {
        private readonly Button nextBtn = new Button
        {
            BackColor = Color.FromArgb(224, 224, 224),
            FlatStyle = FlatStyle.Popup,
            Font = new Font("Tahoma", 12F),
            Location = new Point(697, 9),
            Size = new Size(73, 32),
            Text = "التالي"
        };

        private readonly Label status = CreateLabel(117, 117, "الحالة");
        private readonly Button removeBtn = new Button
        {
            BackColor = Color.FromArgb(255, 192, 192),
            FlatStyle = FlatStyle.Popup,
            Font = new Font("Tahoma", 12F),
            Location = new Point(11, 9),
            Size = new Size(100, 32),
            Text = "إزالة"
        };

        public FileViewControl FileView { get; }
        public bool ReplaceExisting { get; }
        public event EventHandler Prioritize;
        public event EventHandler RemoveRequested;

        public FileDownloadingControl(FileViewControl fileView = null, Color backColor = default(Color))
        {
            FileView = fileView;
            ReplaceExisting = fileView?.IsForce ?? false;
            ClientSize = new Size(782, 50);
            if (backColor.A == 255) BackColor = backColor;

            Controls.Add(CreateLabel(506, 185, fileView?.FileName ?? "اسم الملف"));
            Controls.Add(CreateLabel(240, 260, fileView?.FolderName ?? "اسم المجلد"));
            Controls.Add(status);
            Controls.Add(removeBtn);
            removeBtn.Click += (sender, e) => RemoveRequested?.Invoke(this, e);

            if (fileView == null)
            {
                removeBtn.Text = "إزالة المنتظر";
                status.Font = SystemFonts.MessageBoxFont;
                nextBtn.Dispose();
                return;
            }

            Controls.Add(nextBtn);
            nextBtn.Click += (sender, e) => Prioritize?.Invoke(this, e);
            fileView.Status = Status.Waiting;
        }

        private static Label CreateLabel(int x, int width, string text) => new Label
        {
            Font = new Font("Tahoma", 12F),
            Location = new Point(x, 9),
            Size = new Size(width, 32),
            RightToLeft = RightToLeft.Yes,
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter
        };

        public void SetWaiting(bool isNext)
        {
            nextBtn.Enabled = !isNext;
            status.Text = isNext ? "التالي" : "ينتظر";
            status.ForeColor = Color.FromArgb(160, 110, 0);
            FileView.Status = isNext ? Status.Ready : Status.Waiting;
        }

        public void StartDownload()
        {
            nextBtn.Enabled = false;
            SetProgress(null);
            status.Text = "جارٍ التحميل";
            status.ForeColor = Color.FromArgb(0, 120, 160);
            removeBtn.Text = "إلغاء";
            FileView.Status = Status.Downloading;
        }

        public void SetProgress(int? percent) => nextBtn.Text = percent.HasValue ? $"{percent}%" : "…";

        public void SetCanceling()
        {
            removeBtn.Enabled = false;
            status.Text = "جارٍ الإلغاء";
        }

        public void SetBatchSummary(int completed, int failed, int canceled)
        {
            status.Text = $"تم: {completed} | فشل: {failed}\nأُلغي: {canceled}";
            status.ForeColor = failed > 0 ? Color.Firebrick : SystemColors.ControlText;
        }
    }
}
