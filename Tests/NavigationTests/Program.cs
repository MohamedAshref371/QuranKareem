using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuranKareem;

namespace QuranKareem.NavigationTests
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--tafseer-ui-check")
            {
                try { TafseerChecks.CheckPreviewUi(Path.GetFullPath(args[1])); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if ((args.Length == 4 || args.Length == 5) && args[0] == "--line-export-check")
            {
                try { LineExportChecks.Measure(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), args[3], args.Length == 5 && args[4] == "dark"); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length == 3 && (args[0] == "--audio-playback-check" || args[0] == "--audio-playback-verify"))
            {
                try
                {
                    AudioPlaybackChecks.Measure(Path.GetFullPath(args[1]), double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture), args[0] == "--audio-playback-verify");
                    return 0;
                }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length == 2 && args[0] == "--audio-transport-check")
            {
                try { AudioPlaybackChecks.CheckTransport(Path.GetFullPath(args[1])); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length == 3 && args[0] == "--audio-mp3-check")
            {
                try { AudioExportChecks.CheckMp3(Path.GetFullPath(args[1]), Path.GetFullPath(args[2])); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length == 2 && args[0] == "--picture-pages")
            {
                try { PicturePageChecks.Measure(Path.GetFullPath(args[1])); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length == 2 && args[0] == QuranClassChecks.WorkerArgument)
            {
                try { QuranClassChecks.Run(args[1]); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            }

            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.Error.WriteLine("Pass the path to the bundled text Quran database.");
                return 2;
            }

            string database = Path.GetFullPath(args[0]);
            Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetParent(database).FullName).FullName);

            try
            {
                TextQuran visual = TextQuran.Instance;
                Require(visual.Start(database, 1, 1), "text Quran starts");
                ReadingNavigator reading = new ReadingNavigator(visual);

                Check(reading.Position, 1, 1, 1, 1, "initial verse");
                Require(reading.GoToSurah(2), "navigate to surah 2");
                Check(reading.Position, 2, 0, 2, 1, "surah boundary includes basmalah");
                Require(reading.GoToAyah(2, 286), "navigate to last ayah of surah 2");
                Require(reading.Position.Ayah == 286, "last ayah of surah 2");
                Require(reading.GoToAyah(2, 287), "advance past last ayah of surah 2");
                Check(reading.Position, 3, 0, 50, 20, "next surah boundary");
                Require(reading.GoToPage(604), "navigate to last page");
                Check(reading.Position, 112, 0, 604, 240, "last page start");
                Require(reading.GoToQuarter(240), "navigate to last quarter");
                Check(reading.Position, 100, 9, 600, 240, "last quarter start");
                Require(reading.Position.Juz == 30 && reading.Position.Hizb == 60, "quarter maps to juz and hizb");
                Require(reading.GoToJuz(1), "navigate to first juz");
                Check(reading.Position, 1, 1, 1, 1, "first juz start");
                Require(reading.GoToJuz(30), "navigate to last juz");
                Require(reading.Position.Quarter == 233, "juz 30 starts at quarter 233");
                Require(reading.GoToHizb(60), "navigate to last hizb");
                Require(reading.Position.Quarter == 237, "hizb 60 starts at quarter 237");
                Require(reading.GoToAyah(114, 6) && visual.Set(next: true), "text mode wraps after the final verse");
                Check(reading.Position, 1, 1, 1, 1, "text final verse wrap");

                PictureQuran picture = PictureQuran.Instance;
                Require(picture.Start(Path.Combine("pictures", "Quran-CD-Roaya-5"), 2, 286), "picture Quran starts");
                var pictureReading = new ReadingNavigator(picture);
                Require(pictureReading.GoToAyah(2, 287), "picture mode advances past the last ayah of surah 2");
                Check(pictureReading.Position, 3, 0, 50, 20, "picture surah boundary");
                Require(pictureReading.GoToPage(604), "picture mode opens the last page");
                Check(pictureReading.Position, 112, 0, 604, 240, "picture last page");
                Require(pictureReading.GoToPage(605), "picture mode wraps after the last page");
                Check(pictureReading.Position, 1, 1, 1, 1, "picture final page wrap");
                Require(pictureReading.GoToQuarter(240), "picture mode opens the last quarter");
                Check(pictureReading.Position, 100, 9, 599, 240, "picture last quarter");
                PicturePageChecks.CheckHighlights(picture);
                PicturePageChecks.CheckBitmapLifetime(picture);

                var search = new QuranSearchService(database);
                CheckSearch(search, "الله", false, 1857);
                CheckSearch(search, "  الله  ", false, 1857);
                CheckSearch(search, "الله", true, 1894);
                CheckSearch(search, "الل ه", true, 1894);
                Require(search.SearchAsync("  ", true, CancellationToken.None).GetAwaiter().GetResult().Length == 0, "empty query has no results");

                using (var cancellation = new CancellationTokenSource())
                {
                    cancellation.Cancel();
                    try
                    {
                        search.SearchAsync("الله", false, cancellation.Token).GetAwaiter().GetResult();
                        throw new InvalidOperationException("canceled search completed");
                    }
                    catch (OperationCanceledException) { }
                }

                foreach (bool spelling in new[] { false, true })
                {
                    var timings = new long[5];
                    for (int i = 0; i < timings.Length; i++)
                    {
                        var watch = Stopwatch.StartNew();
                        search.SearchAsync("الله", spelling, CancellationToken.None).GetAwaiter().GetResult();
                        watch.Stop();
                        timings[i] = watch.ElapsedMilliseconds;
                    }
                    Array.Sort(timings);
                    Console.WriteLine("Cached search (spelling=" + spelling + "): median " + timings[2] + " ms");
                }

                Console.WriteLine("Navigation tests passed against bundled Quran data.");
                QuranClassChecks.RunIsolated();
                AudioExportChecks.Run();
                LineExportChecks.CheckFrameWriter();
                Task.Run(TafseerChecks.RunAsync).GetAwaiter().GetResult();
                Task.Run(RecitationDownloadChecks.RunAsync).GetAwaiter().GetResult();
                RecitationDownloadChecks.RunQueue();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void Check(ReadingPosition position, int surah, int ayah, int page, int quarter, string label)
        {
            Require(position.Surah == surah && position.Ayah == ayah && position.Page == page && position.Quarter == quarter,
                label + ": expected " + string.Join(",", new[] { surah, ayah, page, quarter }.Select(x => x.ToString())) +
                "; got " + string.Join(",", new[] { position.Surah, position.Ayah, position.Page, position.Quarter }.Select(x => x.ToString())));
        }

        private static void CheckSearch(QuranSearchService search, string query, bool spelling, int expectedCount)
        {
            QuranSearchResult[] results = search.SearchAsync(query, spelling, CancellationToken.None).GetAwaiter().GetResult();
            Require(results.Length == expectedCount, "search count for " + query + ", spelling=" + spelling);
            Require(results.All(result => result.Surah >= 1 && result.Surah <= 114 && result.Ayah >= 0 && result.Preview.Length <= 63), "search result metadata");
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }
    }
}
