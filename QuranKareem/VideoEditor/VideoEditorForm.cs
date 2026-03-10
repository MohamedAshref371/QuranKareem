using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QuranKareem
{
    public partial class VideoEditorForm : Form
    {
        private int quranClass;
        private bool captions;
        private static int width = 1920, height = 1080, x = 100, y = 588, lWidth = 1720, lHeight = 200, audioRate = 128000;
        private static float frames = 30f;
        private static bool autoCheck, yCheck;

        public VideoEditorForm(int quranClass, bool captions = false)
        {
            InitializeComponent();
            this.quranClass = quranClass;
            this.captions = captions;
            videoWidth.Value = width; videoHeight.Value = height;
            frameRate.Value = (decimal)frames;
            locX.Value = x; locY.Value = y;
            lineWidth.Value = lWidth; lineHeight.Value = lHeight;
            audioBitRate.Value = audioRate;
            lineHeightAuto.Checked = autoCheck;
            yEditCheck.Checked = yCheck;
            if (yCheck)
                lineHeight.Enabled = true;
        }

        private void VideoPath_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                VideoXMLProperties.VideoPath = openFileDialog.FileName;
                videoNameLabel.Text = openFileDialog.FileName;
            }
        }

        private void VideoOutputPath_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                VideoXMLProperties.OutputPath = saveFileDialog.FileName;
                videoOutputNameLabel.Text = saveFileDialog.FileName;
            }
        }

        private readonly List<int> ayahword = new List<int>();
        private Bitmap bitmap;
        private int surah, page;
        private void VideoEditorForm_Load(object sender, EventArgs e)
        {
            int[] minmax;
            if (quranClass == 1)
            {
                surah = PictureQuran.Instance.SurahNumber;
                page = PictureQuran.Instance.PageNumber;
                bitmap = (Bitmap)PictureQuran.Instance.PagePicture.Clone();
                minmax = PictureQuran.Instance.GetStartAndEndOfPage();
            }
            else if(quranClass == 2)
            {
                surah = TrueTypeFontQuran.Instance.SurahNumber;
                page = TrueTypeFontQuran.Instance.PageNumber;
                minmax = TrueTypeFontQuran.Instance.GetStartAndEndOfPage();
            }
            else
            {
                MessageBox.Show("لم أجد مصاحف مصورة.");
                Close();
                return;
            }

            float[] minmax2;
            string mp3Url;
            
            if (captions)
            {
                YoutubeCaptions.Surah = surah;
                try
                {
                    minmax2 = YoutubeCaptions.WordsList(minmax[0], minmax[1], out mp3Url, ayahword, VideoXMLProperties.AudioTimestamps);
                }
                catch
                {
                    MessageBox.Show("حاول ان توفق بين الصفحة الحالية وملف الـ كابشن الناتج");
                    Close();
                    return;
                }
            }
            else
            {
                minmax2 = AudioQuran.Instance.WordsList(minmax[0], minmax[1], out mp3Url, ayahword, VideoXMLProperties.AudioTimestamps);
            }

            if (minmax2 == null)
            {
                MessageBox.Show("لم أستطع إمساك سورة صوتية أو غير متوفر تحديد الكلمات لهذا الشيخ.");
                Close();
                return;
            }

            VideoXMLProperties.AudioPath = mp3Url;
            VideoXMLProperties.AudioOffsetInSecond = minmax2[0];
            VideoXMLProperties.LengthInSecond = minmax2[1] - minmax2[0];
            
            audioLength.Text = AudioQuran.Instance.GetPositionOf((int)((minmax2[1] - minmax2[0]) * 1000));
        }

        private void VideoWidth_ValueChanged(object sender, EventArgs e)
        {
            width = (int)videoWidth.Value;
            VideoXMLProperties.VideoWidth = width;
        }

        private void VideoHeight_ValueChanged(object sender, EventArgs e)
        {
            height = (int)videoHeight.Value;
            VideoXMLProperties.VideoHeight = height;
        }

        private void FrameRate_ValueChanged(object sender, EventArgs e)
        {
            frames = (float)frameRate.Value;
            VideoXMLProperties.FrameRate = frames;
        }

        private void AudioBitRate_ValueChanged(object sender, EventArgs e)
        {
            audioRate = (int)audioBitRate.Value;
            VideoXMLProperties.AudioBitRate = audioRate;
        }

        private void OliveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/olive-editor/olive");
        }

        private void OliveLink32_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.olivevideoeditor.org/dl/Olive-1e3cf53-Windows-i686.exe");
        }

        private void OliveLink64_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.olivevideoeditor.org/dl/Olive-1e3cf53-Windows-x86_64.exe");
        }

        private void TtfColrv1_CheckedChanged(object sender, EventArgs e)
        {
            TrueTypeFontQuran.Instance.UseColoringFont = ttfColrv1.Checked;
            strokeLine.Enabled = ttfColrv1.Checked;
            topLine.Enabled = ttfColrv1.Checked;
        }

        private void TopLine_ValueChanged(object sender, EventArgs e)
        {
            TrueTypeFontQuran.Instance.UnderlineY = (float)topLine.Value;
        }

        private void StrokeLine_ValueChanged(object sender, EventArgs e)
        {
            TrueTypeFontQuran.Instance.StrokeWidth = (float)strokeLine.Value;
        }

        private void VidiotLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://sourceforge.net/projects/vidiot");
        }

        private void YEditCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (yEditCheck.Visible)
                lineHeight.Enabled = yEditCheck.Checked;
            yCheck = yEditCheck.Checked;
        }

        private void LineHeightAuto_CheckedChanged(object sender, EventArgs e)
        {
            lineHeight.Enabled = !lineHeightAuto.Checked;
            yEditCheck.Visible = lineHeightAuto.Checked;
            if (!yEditCheck.Visible)
                yEditCheck.Checked = false;
            autoCheck = lineHeightAuto.Checked;
        }

        private void Generate_Click(object sender, EventArgs e)
        {
            string path = "videos\\" + DateTime.Now.Ticks.ToString();
            if (quranClass == 2)
            {
                TrueTypeFontQuran.Instance.GetAyatInLinesWithWordsMarks(
                ayahword,
                VideoXMLProperties.VideoWidth, VideoXMLProperties.VideoHeight,
                (int)locX.Value, (int)locY.Value, (int)lineWidth.Value, (int)lineHeight.Value,
                lineHeightAuto.Checked, yEditCheck.Checked,
                path,
                VideoXMLProperties.ImagesPaths,
                surah, page
                );
            }
            else
            {
                PictureQuran.Instance.GetAyatInLinesWithWordsMarks(
                ayahword,
                VideoXMLProperties.VideoWidth, VideoXMLProperties.VideoHeight,
                (int)locX.Value, (int)locY.Value, (int)lineWidth.Value, (int)lineHeight.Value,
                lineHeightAuto.Checked, yEditCheck.Checked,
                path,
                VideoXMLProperties.ImagesPaths,
                bitmap, surah, page
                );
            }
                
            if (VideoXMLProperties.ImagesPaths.Count != 0)
            {
                if (vidiotRadio.Checked)
                    File.WriteAllText(path + $"\\QuranKareem.vid", VidiotXmlBuilder.Build());
                else if (oliveRadio.Checked)
                    File.WriteAllText(path + $"\\QuranKareem.ove", OliveXmlBuilder.Build());
                Process.Start(path);
            }
            else
                MessageBox.Show("هناك خطأ، يحتمل أن المصحف المصور لا يدعم تحديد الكلمات");
        }

        private void LocX_ValueChanged(object sender, EventArgs e)
        {
            x = (int)locX.Value;
        }

        private void LocY_ValueChanged(object sender, EventArgs e)
        {
            y = (int)locY.Value;
        }

        private void LineWidth_ValueChanged(object sender, EventArgs e)
        {
            lWidth = (int)lineWidth.Value;
        }

        private void LineHeight_ValueChanged(object sender, EventArgs e)
        {
            lHeight = (int)lineHeight.Value;
        }
    }
}
