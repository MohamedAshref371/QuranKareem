using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace QuranKareem.NavigationTests
{
    internal static class PicturePageChecks
    {
        public static void Measure(string folder)
        {
            var picture = PictureQuran.Instance;
            Require(picture.Start(folder, 1, 1), "picture Quran starts");
            for (int page = 2; page <= 16; page++) Require(picture.Set(page: page), "warm-up page " + page);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var elapsed = new double[240];
            var timer = new Stopwatch();
            int collectionsBefore = GC.CollectionCount(2);
            using (Process process = Process.GetCurrentProcess())
            {
                long start = PrivateBytes(process), peak = start;
                for (int i = 0; i < elapsed.Length; i++)
                {
                    int page = i < 160 ? 20 + i : 338 - i;
                    timer.Restart();
                    bool loaded = picture.Set(page: page);
                    timer.Stop();
                    Require(loaded && picture.PageNumber == page, "measured page " + page);
                    elapsed[i] = timer.Elapsed.TotalMilliseconds;
                    if ((i + 1) % 5 == 0) peak = Math.Max(peak, PrivateBytes(process));
                }

                long end = PrivateBytes(process);
                Array.Sort(elapsed);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Turns={0} MedianMs={1:F2} P95Ms={2:F2} StartMiB={3:F1} PeakMiB={4:F1} EndMiB={5:F1} Gen2={6}",
                    elapsed.Length, (elapsed[119] + elapsed[120]) / 2, elapsed[227], MiB(start), MiB(peak), MiB(end), GC.CollectionCount(2) - collectionsBefore));
            }

            CheckHighlights(picture);
        }

        public static void CheckHighlights(PictureQuran picture)
        {
            foreach (bool dark in new[] { false, true })
            {
                picture.DarkMode = dark;
                Require(picture.Set(surah: 2, ayah: 255), "open verse 2:255");
                string clean = PixelHash(picture.PagePicture);
                string ayah = PixelHash(picture.CurrentPicture);
                Require(ayah != clean, "verse coloring changes the clean page");
                Require(picture.WordOf(1), "select the first word");
                string firstWord = PixelHash(picture.CurrentPicture);
                Require(firstWord != ayah, "first word coloring changes the verse image");
                Require(picture.WordOf(2), "select the second word");
                string secondWord = PixelHash(picture.CurrentPicture);
                Require(secondWord != ayah && secondWord != firstWord, "second word coloring differs from first");
                Require(PixelHash(picture.PagePicture) == clean, "word coloring leaves the clean page untouched");
                picture.WordOf(-1);
                Require(picture.CurrentWord == -1 && PixelHash(picture.CurrentPicture) == ayah, "a gap removes only the word color");
                Require(picture.Set(surah: 2, ayah: 255) && PixelHash(picture.CurrentPicture) == ayah, "verse selection removes word coloring");
                int page = picture.PageNumber;
                Require(picture.Set(page: page + 1) && picture.Set(surah: 2, ayah: 255), "turn the page and return to the verse");
                Require(PixelHash(picture.CurrentPicture) == ayah, "returning to the page restores the same verse coloring");
                Require(picture.WordOf(1) && PixelHash(picture.CurrentPicture) == firstWord, "returning to the page restores the same word coloring");
                Console.WriteLine($"Highlight fingerprints dark={dark}: clean={clean} ayah={ayah} word1={firstWord} word2={secondWord}");
            }
            picture.DarkMode = false;
        }

        public static void CheckBitmapLifetime(PictureQuran picture)
        {
            using (var view = new PictureBox { Size = new Size(255, 450), BackgroundImageLayout = ImageLayout.Stretch })
            using (var frame = new Bitmap(255, 450))
            {
                Bitmap previous = picture.CurrentPicture;
                Bitmap previousClean = picture.PagePicture;
                view.BackgroundImage = previous;
                EventHandler swap = (sender, e) =>
                {
                    Require(view.BackgroundImage == null || view.BackgroundImage.Width > 0, "old image is usable until the view swaps it");
                    view.BackgroundImage = picture.CurrentPicture;
                };
                picture.PictureReplaced += swap;
                try
                {
                    Require(picture.Set(page: picture.PageNumber + 1), "turn page with a WinForms view attached");
                    Require(view.BackgroundImage == picture.CurrentPicture && view.BackgroundImage != previous, "view gets the new image");
                    view.DrawToBitmap(frame, view.ClientRectangle);
                    Require(IsDisposed(previous) && IsDisposed(previousClean), "both superseded bitmaps are disposed");

                    Bitmap final = picture.CurrentPicture;
                    Bitmap finalClean = picture.PagePicture;
                    picture.ReleasePictures();
                    Require(view.BackgroundImage == null && IsDisposed(final) && IsDisposed(finalClean), "leaving the picture reader releases both bitmaps");
                }
                finally { picture.PictureReplaced -= swap; }
            }
        }

        private static bool IsDisposed(Bitmap bitmap)
        {
            try { bitmap.GetPixel(0, 0); return false; }
            catch (ArgumentException) { return true; }
        }

        internal static string PixelHash(Bitmap image)
        {
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var pixels = new byte[image.Width * image.Height * 4];
                for (int y = 0; y < image.Height; y++)
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * image.Width * 4, image.Width * 4);
                using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(pixels)).Replace("-", "");
            }
            finally { image.UnlockBits(data); }
        }

        private static long PrivateBytes(Process process)
        {
            process.Refresh();
            return process.PrivateMemorySize64;
        }

        private static double MiB(long bytes) => bytes / (1024.0 * 1024.0);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
