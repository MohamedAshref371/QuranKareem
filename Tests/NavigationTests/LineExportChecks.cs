using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuranKareem.NavigationTests
{
    internal static class LineExportChecks
    {
        public static void CheckFrameWriter()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FrameCheck-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var line = new Bitmap(7, 5))
                using (var writer = new LineFrameWriter(folder, 25, 17))
                {
                    for (int y = 0; y < line.Height; y++)
                        for (int x = 0; x < line.Width; x++)
                            line.SetPixel(x, y, Color.FromArgb(25 + x * 30, 50 + x, 100 + y, 150));

                    var positions = new[] { new Rectangle(6, 7, 7, 5), new Rectangle(-4, 10, 15, 12), new Rectangle(30, 20, 7, 5) };
                    for (int i = 0; i < positions.Length; i++)
                    {
                        Rectangle position = positions[i];
                        bool scale = i == 1;
                        string file = writer.Write(i, line, position, scale);
                        using (var expected = new Bitmap(25, 17))
                        using (var graphics = Graphics.FromImage(expected))
                        using (var actual = new Bitmap(Path.Combine(folder, file)))
                        {
                            graphics.Clear(Color.Transparent);
                            if (scale) graphics.DrawImage(line, position);
                            else graphics.DrawImage(line, position.X, position.Y);
                            Require(expected.Size == actual.Size && PicturePageChecks.PixelHash(expected) == PicturePageChecks.PixelHash(actual),
                                "frame PNG preserves full dimensions, alpha and clipping");
                        }
                    }
                }
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
        }

        public static void Measure(string content, string output, string mode, bool dark = false)
        {
            Directory.SetCurrentDirectory(content);
            var words = new List<int>();
            Action<List<string>> export;
            if (mode == "picture")
            {
                string folder = Path.Combine(content, "pictures", "Quran-CD-Roaya-5");
                using (var db = new SQLiteConnection("Data Source=" + Path.Combine(folder, "000.db") + ";Read Only=True"))
                using (var query = new SQLiteCommand("SELECT DISTINCT ayah,word FROM ayat JOIN words ON ayat.id=words.ayah_id WHERE page=30 AND surah=2 AND word BETWEEN 1 AND 599 ORDER BY ayah,word", db))
                {
                    db.Open();
                    using (var rows = query.ExecuteReader())
                        while (rows.Read()) { words.Add(rows.GetInt32(0)); words.Add(rows.GetInt32(1)); }
                }
                var quran = PictureQuran.Instance;
                Require(quran.Start(folder) && quran.Set(page: 30), "open picture page 30");
                quran.DarkMode = dark;
                export = paths => paths.AddRange(Task.Run(quran.PrepareLineExport(words, 1920, 1080, 100, 588, 1720, 200,
                    true, true, output, quran.PagePicture, 2, 30)).GetAwaiter().GetResult());
            }
            else
            {
                string folder = Path.Combine(output, "font");
                CreateFontFixture(folder);
                for (int ayah = 1; ayah <= 12; ayah++)
                    for (int word = 1; word <= 12; word++) { words.Add(ayah); words.Add(word); }
                var quran = TrueTypeFontQuran.Instance;
                Require(quran.Start(folder, 1, 1), "open font fixture");
                quran.DarkMode = dark;
                quran.UseColoringFont = mode == "skia";
                export = paths => paths.AddRange(Task.Run(quran.PrepareLineExport(words, 1920, 1080, 100, 588, 1720, 200,
                    true, true, output, 1, 1)).GetAwaiter().GetResult());
            }

            // Keep pauses and repeated words in the timeline.
            words.InsertRange(2, new[] { words[0], -1, words[0], words[1] });
            words.AddRange(new[] { words[words.Count - 2], -1 });
            var images = new List<string>();
            var clock = Stopwatch.StartNew();
            export(images);
            clock.Stop();
            long peakMb = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            Console.WriteLine($"{mode} dark={dark}: {images.Count} frames in {clock.Elapsed.TotalSeconds:F3}s, peak working set {peakMb} MiB");
            Require(images.Count == words.Count / 2, "one image for each timeline interval");
            var manifest = new List<string>();
            for (int i = 0; i < images.Count; i++)
            {
                using (var image = new Bitmap(Path.Combine(output, images[i])))
                {
                    Require(image.Size == new Size(1920, 1080), "full-size frame");
                    manifest.Add(PicturePageChecks.PixelHash(image));
                }
            }
            Require(manifest[0] == manifest[2], "repeated word matches the first image");
            Require(manifest.Take(10).Distinct().Count() >= 4, "word changes produce different frames");
            if (mode != "skia") Require(manifest[0] != manifest[1], "the pause clears the word color");
            File.WriteAllLines(Path.Combine(output, "pixels.txt"), manifest);
        }

        private static void CreateFontFixture(string folder)
        {
            Directory.CreateDirectory(folder);
            foreach (string name in new[] { "000.ttf", "001.ttf" })
                File.Copy(Path.Combine("fonts", "UthmanicHafs_V22.ttf"), Path.Combine(folder, name));
            File.WriteAllText(Path.Combine(folder, "Colors.txt"), "1|2|2|Red");
            using (var db = new SQLiteConnection("Data Source=" + Path.Combine(folder, "000.db")))
            using (var sql = new SQLiteCommand(@"
CREATE TABLE description (type,version,narration,surahs_count,quarters_count,pages_count,extension,comment);
INSERT INTO description VALUES (5,2,1,1,1,1,'.ttf','Line export fixture');
CREATE TABLE pages (id INTEGER PRIMARY KEY,scale);
INSERT INTO pages VALUES(1,5000);
CREATE TABLE surahs (id INTEGER PRIMARY KEY,name,makya_madanya,ayat_count);
INSERT INTO surahs VALUES(1,'الاختبار',1,12);
CREATE TABLE ayat (id INTEGER PRIMARY KEY,surah,quarter,page,ayah,words_count);
CREATE TABLE words (ayah_id,line,word,discriminator,text);
CREATE TABLE discriminators (id INTEGER PRIMARY KEY,enabled,comment);
INSERT INTO discriminators VALUES(1,1,'Test');", db))
            {
                db.Open();
                sql.ExecuteNonQuery();
                using (var tx = db.BeginTransaction())
                {
                    sql.Transaction = tx;
                    for (int ayah = 1; ayah <= 12; ayah++)
                    {
                        sql.CommandText = $"INSERT INTO ayat VALUES({ayah},1,1,1,{ayah},12);";
                        sql.ExecuteNonQuery();
                        for (int word = 1; word <= 12; word++)
                        {
                            sql.CommandText = $"INSERT INTO words VALUES({ayah},{ayah},{word},1,'{ "ابتثجحخدذرزس"[word - 1]}');";
                            sql.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
