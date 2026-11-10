using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QuranKareem.NavigationTests
{
    internal static class QuranClassChecks
    {
        public const string WorkerArgument = "--quran-class-checks";

        public static void RunIsolated()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuranClassChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                // The renderer and player singletons can hold SQLite resources until process exit.
                var start = new ProcessStartInfo(typeof(QuranClassChecks).Assembly.Location, $"{WorkerArgument} \"{root}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };
                using (Process worker = Process.Start(start))
                {
                    if (!worker.WaitForExit(30000))
                    {
                        worker.Kill();
                        worker.WaitForExit();
                        throw new TimeoutException("Quran class checks did not finish.");
                    }
                    Require(worker.ExitCode == 0, "Quran class checks passed");
                }
            }
            finally { Directory.Delete(root, recursive: true); }
            Console.WriteLine("Audio description and font navigation checks passed.");
        }

        public static void Run(string root)
        {
            CheckAudioDescription(Directory.CreateDirectory(Path.Combine(root, "audio")).FullName);
            CheckFontNavigation(Directory.CreateDirectory(Path.Combine(root, "font")).FullName);
        }

        private static void CheckAudioDescription(string folder)
        {
            string database = Path.Combine(folder, "000.db");
            File.Copy(Path.Combine("audios", "database for audios.db"), database);
            using (var host = new Panel())
            {
                AudioQuran audio = AudioQuran.Instance;
                audio.AddInControls(host.Controls);
                Require(audio.NewQuranAudio(folder), "open a copy of the bundled audio timing database");
                int[] timings = audio.GetTimestamps(2);
                string comment = "تلاوة القارئ 'عبد الله' — \"رواية حفص\"" + Environment.NewLine + "ملاحظة ثانية؛ وُضوح الصوت";
                Require(audio.SetDescription(".wav", comment), "save a description containing quotes and line breaks");
                Require(audio.NewQuranAudio(folder), "reopen the audio database after saving");
                Require(audio.Extension == ".wav" && audio.Comment == comment, "read the exact saved description");
                Require(audio.GetTimestamps(2).SequenceEqual(timings), "saving a description leaves timings unchanged");
                Require(audio.SetDescription(".mp3", "") && audio.NewQuranAudio(folder) && audio.Comment == "", "clear a saved comment");
                audio.SetAyah(1, 1, 1234);
                audio.SetAyah(1, 2, 2450);
                var subtitles = audio.GetSubtitleTimings(1, 0, 7);
                Require(subtitles.SequenceEqual(new[] { (1, 0, 1234), (2, 1234, 2450) }), "subtitle timing snapshot uses ayah numbers and skips untimed rows");
                string translation = Path.Combine("translations", "English - Saheeh International.db");
                var source = new TafseerQuran(translation, translation);
                string srt = source.SubRipTextAsync(1, subtitles, false).GetAwaiter().GetResult();
                Require(srt.Contains("00:00:00,000 --> 00:00:01,234") && srt.Contains("00:00:01,234 --> 00:00:02,450"),
                    "SRT uses the audio database's exact timings");
                File.WriteAllText(Path.Combine(folder, "checked.srt"), srt);
            }
            Console.WriteLine("Audio description round-trip passed on a copy of the bundled database.");
        }

        private static void CheckFontNavigation(string folder)
        {
            string font = Path.Combine("fonts", "UthmanicHafs_V22.ttf");
            foreach (string name in new[] { "000.ttf", "001.ttf", "002.ttf" })
                File.Copy(font, Path.Combine(folder, name));

            File.WriteAllText(Path.Combine(folder, "Colors.txt"), "1|2|2|Red");
            string database = Path.Combine(folder, "000.db");
            using (var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder { DataSource = database }.ConnectionString))
            using (var setup = new SQLiteCommand(@"
                CREATE TABLE description (type INTEGER, version INTEGER, narration INTEGER, surahs_count INTEGER, quarters_count INTEGER, pages_count INTEGER, extension TEXT, comment TEXT);
                INSERT INTO description VALUES (5, 2, 1, 2, 2, 2, '.ttf', 'Test');
                CREATE TABLE pages (id INTEGER PRIMARY KEY, scale INTEGER);
                INSERT INTO pages VALUES (1, 5000), (2, 5000);
                CREATE TABLE surahs (id INTEGER PRIMARY KEY, name TEXT, makya_madanya INTEGER, ayat_count INTEGER);
                INSERT INTO surahs VALUES (1, 'الفاتحة', 1, 1), (2, 'البقرة', 0, 1);
                CREATE TABLE ayat (id INTEGER PRIMARY KEY, surah INTEGER, quarter INTEGER, page INTEGER, ayah INTEGER, words_count INTEGER);
                INSERT INTO ayat VALUES (1, 1, 1, 1, 1, 1), (2, 2, 2, 2, 0, 0), (3, 2, 2, 2, 1, 2);
                CREATE TABLE words (ayah_id INTEGER, line INTEGER, word INTEGER, discriminator INTEGER, text TEXT);
                INSERT INTO words VALUES (1, 1, 1, 1, 'أ'), (2, 1, 1, 1, 'ب'), (3, 1, 1, 1, 'ج'), (3, 1, 2, 1, 'د');
                CREATE TABLE discriminators (id INTEGER PRIMARY KEY, enabled INTEGER, comment TEXT);
                INSERT INTO discriminators VALUES (1, 1, 'Test');", connection))
            {
                connection.Open();
                setup.ExecuteNonQuery();
            }

            var fontQuran = TrueTypeFontQuran.Instance;
            Require(fontQuran.Start(folder, 1, 1), "font Quran starts with a two-page fixture");
            Require(fontQuran.Set(surah: 1, ayah: 2) && fontQuran.SurahNumber == 2 && fontQuran.AyahNumber == 0 && fontQuran.PageNumber == 2,
                "font mode advances to the next surah and loads its page");
            Require(fontQuran.Set(next: true) && fontQuran.AyahNumber == 1 && fontQuran.WordOf(2),
                "font mode uses the destination verse's word count");
            Require(fontQuran.Set(next: true) && fontQuran.SurahNumber == 1 && fontQuran.AyahNumber == 1 && fontQuran.PageNumber == 1,
                "font mode wraps at the final verse");
            Require(fontQuran.Set(page: 2) && fontQuran.AyahNumber == 0 && fontQuran.Set(page: 3) && fontQuran.PageNumber == 1,
                "font mode opens and wraps pages");
            Console.WriteLine("Font navigation and word selection passed on a two-page fixture.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
