using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuranKareem
{
    public partial class DownloadAudioQuranFiles : Form
    {
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private readonly RecitationDownloader downloader;
        // The dialog owns the queue. Only its WinForms thread adds, reorders or removes rows.
        private readonly LinkedList<FileDownloadingControl> pending = new LinkedList<FileDownloadingControl>();
        private readonly FileDownloadingControl queueHeader;
        private FileDownloadingControl active;
        private CancellationTokenSource activeCancellation;
        private Task downloadLoop;
        private bool stopping;
        private int completed, failed, canceled;

        public DownloadAudioQuranFiles() : this(new RecitationDownloader(httpClient)) { }

        internal DownloadAudioQuranFiles(RecitationDownloader downloader)
        {
            this.downloader = downloader;
            InitializeComponent();
            queueHeader = new FileDownloadingControl();
            queueHeader.RemoveRequested += (sender, e) => ClearPending();
            flowLayoutPanel.Controls.Add(queueHeader);
        }

        private string path = "";
        private Panel currentPanel;
        private readonly Dictionary<string, Panel> panels = new Dictionary<string, Panel>(StringComparer.OrdinalIgnoreCase);

        public bool IsBusy => active != null || pending.Count > 0;

        public void OpenDownloadingTab() => tabControl.SelectedTab = fileDownloadPage;

        public void InitializeAudioFile(string path)
        {
            tabControl.SelectedTab = fileViewPage;
            if (string.IsNullOrWhiteSpace(path)) return;
            path = Path.GetFullPath(path);
            if (!string.Equals(this.path, path, StringComparison.OrdinalIgnoreCase))
            {
                if (currentPanel != null)
                {
                    currentPanel.Visible = false;
                    fileViewPage.Controls.Remove(currentPanel);
                    currentPanel = null;
                }
                this.path = path;
                if (panels.TryGetValue(path, out currentPanel))
                {
                    currentPanel.Visible = true;
                    fileViewPage.Controls.Add(currentPanel);
                }
                else
                {
                    currentPanel = new Panel()
                    {
                        Location = new Point(0, 0),
                        ClientSize = fileViewPage.ClientSize ,
                        AutoScroll = true,
                        Visible = true
                    };
                    fileViewPage.Controls.Add(currentPanel);
                    panels.Add(path, currentPanel);

                    FileViewControl baseView = new FileViewControl(BaseCheckBox_CheckChanged, DownloadBaseBtn_Click)
                    {
                        Location = new Point(0, 10)
                    };
                    currentPanel.Controls.Add(baseView);
                    string s = Path.Combine(path, "download links.txt");
                    if (File.Exists(s))
                        InitializeLinks(File.ReadAllLines(s, Encoding.UTF8), path);
                }
            }
        }
        private void InitializeLinks(string[] links, string folder)
        {
            string folderName = new DirectoryInfo(folder).Name;
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            currentPanel.SuspendLayout();
            try
            {
                foreach (string link in links)
                {
                    if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out Uri uri)
                        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) continue;

                    string fileName = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
                    if (string.IsNullOrWhiteSpace(fileName) || fileName.EndsWith(".", StringComparison.Ordinal)
                        || fileName.EndsWith(" ", StringComparison.Ordinal) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                        || !files.Add(fileName)) continue;

                    string fullPath = Path.Combine(folder, fileName);
                    var file = new FileViewControl(uri, fullPath, fileName, folderName, File.Exists(fullPath))
                    {
                        Location = new Point(0, FileViewControl.ControlSize.Height * currentPanel.Controls.Count + 15)
                    };
                    file.DownloadButtonClick += DownloadBtn_Click;
                    currentPanel.Controls.Add(file);
                }
            }
            finally { currentPanel.ResumeLayout(); }
        }

        private Color GetColor()
        {
            Color clr;
            switch (flowLayoutPanel.Controls.Count % 8)
            {
                case 1:
                    clr = Color.FromArgb(250, 200, 200);
                    break;
                case 2:
                    clr = Color.FromArgb(250, 250, 200);
                    break;
                case 3:
                    clr = Color.FromArgb(200, 250, 200);
                    break;
                case 4:
                    clr = Color.FromArgb(200, 200, 200);
                    break;
                case 5:
                    clr = Color.FromArgb(200, 250, 250);
                    break;
                case 6:
                    clr = Color.FromArgb(200, 200, 250);
                    break;
                case 7:
                    clr = Color.FromArgb(250, 200, 250);
                    break;
                default:
                    clr = Color.FromArgb(250, 250, 250);
                    break;
            }
            return clr;
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            if (stopping) return;
            if (!IsBusy)
            {
                completed = failed = canceled = 0;
                UpdateSummary();
            }

            var control = new FileDownloadingControl((FileViewControl)((Button)sender).Tag, GetColor());
            control.Prioritize += PrioritizeDownload;
            control.RemoveRequested += RemoveDownload;
            flowLayoutPanel.Controls.Add(control);
            pending.AddLast(control);
            pending.First.Value.SetWaiting(isNext: true);

            if (downloadLoop == null || downloadLoop.IsCompleted)
            {
                downloadLoop = ProcessQueueAsync();
                await downloadLoop;
            }
        }

        private async Task ProcessQueueAsync()
        {
            while (pending.First != null)
            {
                var row = pending.First.Value;
                pending.RemoveFirst();
                pending.First?.Value.SetWaiting(isNext: true);

                active = row;
                row.StartDownload();
                using (var request = new CancellationTokenSource())
                {
                    activeCancellation = request;
                    var progress = new Progress<int?>(percent =>
                    {
                        if (active == row && !row.IsDisposed && !request.IsCancellationRequested) row.SetProgress(percent);
                    });
                    Status result;
                    try
                    {
                        bool downloaded = await downloader.DownloadAsync(row.FileView.FileLink, row.FileView.FilePath,
                            row.ReplaceExisting, progress, request.Token);
                        result = downloaded ? Status.Downloaded : Status.Exist;
                        if (downloaded) completed++;
                    }
                    catch (Exception) when (request.IsCancellationRequested)
                    {
                        result = Status.Canceled;
                        canceled++;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Recitation download failed: {0}", ex.Message);
                        result = Status.Error;
                        failed++;
                    }
                    finally
                    {
                        active = null;
                        activeCancellation = null;
                    }
                    FinishRow(row, result);
                    UpdateSummary();
                }
            }
        }

        private void PrioritizeDownload(object sender, EventArgs e)
        {
            var row = (FileDownloadingControl)sender;
            if (pending.First == null || pending.First.Value == row || !pending.Remove(row)) return;
            pending.First.Value.SetWaiting(isNext: false);
            pending.AddFirst(row);
            row.SetWaiting(isNext: true);
            flowLayoutPanel.Controls.SetChildIndex(row, active == null ? 1 : 2);
        }

        private void RemoveDownload(object sender, EventArgs e)
        {
            var row = (FileDownloadingControl)sender;
            if (row == active)
            {
                CancelActive();
                return;
            }

            if (!pending.Remove(row)) return;
            RemoveWaitingRow(row);
            pending.First?.Value.SetWaiting(isNext: true);
            UpdateSummary();
        }

        private void CancelActive()
        {
            if (activeCancellation == null || activeCancellation.IsCancellationRequested) return;
            active.SetCanceling();
            activeCancellation.Cancel();
        }

        private void ClearPending()
        {
            flowLayoutPanel.SuspendLayout();
            try
            {
                while (pending.First != null)
                {
                    var row = pending.First.Value;
                    pending.RemoveFirst();
                    RemoveWaitingRow(row);
                }
                UpdateSummary();
            }
            finally { flowLayoutPanel.ResumeLayout(); }
        }

        private void RemoveWaitingRow(FileDownloadingControl row)
        {
            FinishRow(row, File.Exists(row.FileView.FilePath) ? Status.Exist : Status.NotExist);
            canceled++;
        }

        private static void FinishRow(FileDownloadingControl row, Status result)
        {
            row.FileView.Status = result;
            row.Dispose();
        }

        private void UpdateSummary() => queueHeader.SetBatchSummary(completed, failed, canceled);

        internal async Task StopDownloadsAsync()
        {
            stopping = true;
            ClearPending();
            CancelActive();
            if (downloadLoop != null) await downloadLoop;
        }

        private void DownloadBaseBtn_Click(object sender, EventArgs e)
        {
            flowLayoutPanel.SuspendLayout();
            try
            {
                for (int i = 1; i < currentPanel.Controls.Count; i++)
                {
                    var file = (FileViewControl)currentPanel.Controls[i];
                    if (file.Checked) file.Clicked(sender, e);
                }
            }
            finally { flowLayoutPanel.ResumeLayout(); }
        }

        private void BaseCheckBox_CheckChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox chBox)
                for (int i = 1; i < currentPanel.Controls.Count; i++)
                    ((FileViewControl)currentPanel.Controls[i]).Checked = chBox.Checked;
        }

        private void DownloadAudioQuranFiles_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
