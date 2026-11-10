using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;

namespace QuranKareem.NavigationTests
{
    internal static class AudioPlaybackChecks
    {
        private static void CreateFixture(string folder, bool transport = false)
        {
            Directory.CreateDirectory(folder);
            string database = Path.Combine(folder, "000.db");
            if (File.Exists(database)) throw new IOException("Use a fresh measurement folder.");
            SQLiteConnection.CreateFile(database);
            using (var db = new SQLiteConnection("Data Source=" + database))
            {
                db.Open();
                using (var command = new SQLiteCommand(@"
CREATE TABLE description(type,version,narration,surahs,extension,comment);
INSERT INTO description VALUES(3,5,1,1,'.wav','clock fixture');
CREATE TABLE surahs(id INTEGER PRIMARY KEY,ayat_count,duration);
INSERT INTO surahs VALUES(1,2,8000);
CREATE TABLE ayat(id INTEGER PRIMARY KEY,surah,ayah,timestamp_from,timestamp_to);
INSERT INTO ayat VALUES(1,1,1,0,4000),(2,1,2,4000,8000);
CREATE TABLE words(ayah_id,word,timestamp_from,timestamp_to);", db))
                {
                    command.ExecuteNonQuery();
                    for (int word = 1; word <= 10; word++)
                    {
                        command.CommandText = $"INSERT INTO words VALUES(1,{word},{(word - 1) * 400},{word * 400})";
                        command.ExecuteNonQuery();
                    }
                    if (transport)
                    {
                        command.CommandText = @"
UPDATE words SET timestamp_to=2250 WHERE ayah_id=1 AND word=6;
UPDATE description SET surahs=2;
INSERT INTO surahs VALUES(2,1,8000);
INSERT INTO ayat VALUES(3,2,1,0,8000);";
                        command.ExecuteNonQuery();
                    }
                }
            }
            using (var wave = new WaveFileWriter(Path.Combine(folder, "001.wav"), new WaveFormat(22050, 16, 1)))
            {
                byte[] silence = new byte[22050 * 2];
                for (int second = 0; second < 8; second++) wave.Write(silence, 0, silence.Length);
            }
            if (transport) File.Copy(Path.Combine(folder, "001.wav"), Path.Combine(folder, "002.wav"));
        }

        // Optional: exercises the actual Windows Media Player clock on a desktop with audio.
        public static void Measure(string folder, double rate, bool verify)
        {
            CreateFixture(folder);
            var audio = AudioQuran.Instance;
            var errors = new List<double>();
            int lastWord = 0;
            double ayahError = double.NaN;
            using (var host = new Form { Text = "Quran Kareem audio timing check", ShowInTaskbar = false })
            using (var timeout = new Timer { Interval = 25 })
            {
                var watch = Stopwatch.StartNew();
                audio.AddEventHandlerOfWord((s, e) =>
                {
                    int word = audio.CurrentWord;
                    if (audio.AyahNumber != 1 || word < 2 || word <= lastWord) return;
                    lastWord = word;
                    errors.Add(audio.Mp3CurrentPosition() * 1000 - (word - 1) * 400);
                });
                audio.AddEventHandlerOfAyah((s, e) =>
                {
                    if (audio.AyahNumber != 2 || !double.IsNaN(ayahError)) return;
                    ayahError = audio.Mp3CurrentPosition() * 1000 - 4000;
                    host.BeginInvoke(new Action(host.Close));
                });
                timeout.Tick += (s, e) => { if (watch.ElapsedMilliseconds > 14000) host.Close(); };
                host.Shown += (s, e) =>
                {
                    audio.AddInControls(host.Controls);
                    audio.Rate = rate;
                    audio.Start(folder, 1, 1);
                    timeout.Start();
                };
                host.FormClosing += (s, e) => { timeout.Stop(); audio.Stop(); };
                Application.Run(host);
            }

            Console.WriteLine($"Playback {rate:F1}x: ayah offset {ayahError:F1} ms; " +
                $"word changes {errors.Count}/9; word offsets [{string.Join(", ", errors.Select(error => error.ToString("F1")))}] ms");
            if (double.IsNaN(ayahError)) throw new InvalidOperationException("The playback clock did not reach the next ayah.");
            if (verify)
            {
                Require(ayahError >= 0 && ayahError < 120, "ayah follows the player clock");
                Require(errors.Count == 9 && errors.All(error => error >= 0 && error < 120), "all words follow the player clock");
            }
        }

        public static void CheckTransport(string folder)
        {
            CreateFixture(folder, transport: true);
            var audio = AudioQuran.Instance;
            var time = Stopwatch.StartNew();
            var pauseTime = new Stopwatch();
            int stage = 0, changes = 0, pausedChanges = 0;
            double pausedPosition = 0;
            Exception failure = null;
            using (var host = new Form { Text = "Quran Kareem audio transport check", ShowInTaskbar = false })
            using (var check = new Timer { Interval = 20 })
            {
                audio.AddEventHandlerOfAyah((s, e) => changes++);
                audio.AddEventHandlerOfWord((s, e) => changes++);
                host.Shown += (s, e) =>
                {
                    audio.AddInControls(host.Controls);
                    audio.Rate = 1;
                    audio.Start(folder, 1, 1);
                    check.Start();
                };
                host.FormClosing += (s, e) => { check.Stop(); audio.Stop(); };
                check.Tick += (s, e) =>
                {
                    try
                    {
                        Require(time.ElapsedMilliseconds < 18000, $"transport timed out at step {stage}, ayah {audio.SurahNumber}:{audio.AyahNumber}, word {audio.CurrentWord}");
                        double position = audio.Mp3CurrentPosition() * 1000;
                        switch (stage)
                        {
                            case 0 when audio.CurrentWord == 2:
                                audio.Rate = 1.5;
                                Require(Math.Abs(audio.Mp3CurrentPosition() * 1000 - position) < 100, "changing rate does not seek");
                                stage++;
                                break;
                            case 1 when audio.CurrentWord == 4:
                                Require(audio.Pause(), "pause");
                                pausedPosition = audio.Mp3CurrentPosition() * 1000;
                                pausedChanges = changes;
                                pauseTime.Restart();
                                stage++;
                                break;
                            case 2 when pauseTime.ElapsedMilliseconds >= 550:
                                Require(Math.Abs(position - pausedPosition) < 80 && changes == pausedChanges, "pause freezes position and highlights");
                                audio.Rate = 0.6;
                                Require(audio.Resume(1, 1), "resume the current verse");
                                stage++;
                                break;
                            case 3 when audio.CurrentWord == 5:
                                Require(position >= 1600 && position < 1750, "resumed playback reaches the next word in sync");
                                stage++;
                                break;
                            case 4 when position >= 2250 && position < 2400 && audio.CurrentWord == -1:
                                audio.WordOf(9);
                                stage++;
                                break;
                            case 5 when audio.CurrentWord == 9:
                                Require(position >= 3200 && position < 3400, "forward seek lands in word 9");
                                audio.WordOf(3);
                                stage++;
                                break;
                            case 6 when audio.CurrentWord == 3:
                                Require(position >= 800 && position < 1000, "backward seek restores word 3");
                                audio.Rate = 1.5;
                                Require(audio.Set(1, 2), "jump to a different ayah");
                                stage++;
                                break;
                            case 7 when audio.AyahNumber == 2 && position >= 4000 && position < 4300:
                                Require(audio.CurrentWord == -1, "an ayah without word timings does not keep a stale word");
                                stage++;
                                break;
                            case 8 when audio.SurahNumber == 2 && position > 50:
                                Require(audio.AyahNumber == 1 && position < 500, "end of media advances to the next surah");
                                audio.Stop();
                                pausedChanges = changes;
                                pauseTime.Restart();
                                stage++;
                                break;
                            case 9 when pauseTime.ElapsedMilliseconds >= 350:
                                Require(changes == pausedChanges, "stop prevents further highlight changes");
                                stage++;
                                host.Close();
                                break;
                        }
                    }
                    catch (Exception ex) { failure = ex; host.Close(); }
                };
                Application.Run(host);
            }
            if (failure != null) throw failure;
            Require(stage == 10, "transport check finished");
            Console.WriteLine("Playback transport checks passed: rate change, pause/resume, gap, seeks, ayah jump, surah boundary, stop.");
        }

        private static void Require(bool condition, string check)
        {
            if (!condition) throw new InvalidOperationException(check);
        }
    }
}
