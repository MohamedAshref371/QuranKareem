using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuranKareem.NavigationTests
{
    internal static class TafseerChecks
    {
        public static async Task RunAsync()
        {
            string tafseerFile = Path.Combine("tafasir", "الميسر.db");
            string translationFile = Path.Combine("translations", "English - Saheeh International.db");
            var tafseer = new TafseerQuran(tafseerFile, tafseerFile);
            var translation = new TafseerQuran(translationFile, translationFile);
            var pendingReads = new List<Task<string>>();
            for (int i = 0; i < 8; i++)
            {
                pendingReads.Add(tafseer.AyahTextAsync(1, 1, CancellationToken.None));
                pendingReads.Add(translation.AyahTextAsync(1, 1, CancellationToken.None));
            }
            string[] results = await Task.WhenAll(pendingReads);
            string arabic = results[0], english = results[1];
            Require(arabic.Any(c => c >= '\u0600' && c <= '\u06FF') && english.Contains("Allah"), "read Arabic tafseer and English translation");
            Require(results.Where((text, i) => text != (i % 2 == 0 ? arabic : english)).Count() == 0, "concurrent reads keep their source");

            foreach (string directory in new[] { "tafasir", "translations" })
                foreach (string file in Directory.GetFiles(directory, "*.db"))
                    Require((await new TafseerQuran(file, file).AyahTextAsync(2, 255, CancellationToken.None)).Length > 0,
                        "bundled source opens: " + file);

            var preview = new LatestTafseerRequest();
            var copy = new LatestTafseerRequest();
            var shown = new List<string>();
            var copied = new List<string>();
            var errors = new List<Exception>();
            var oldAyah = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken oldToken = default(CancellationToken);
            Task slowAyah = preview.RunAsync(token => { oldToken = token; return oldAyah.Task; }, shown.Add, errors.Add);
            // The reading view navigates away (and can return to the original verse) before the read completes.
            preview.Cancel();
            Require(oldToken.IsCancellationRequested, "navigation cancels the pending preview");
            await preview.RunAsync(token => tafseer.AyahTextAsync(1, 2, token), shown.Add, errors.Add);
            oldAyah.SetResult(arabic);
            await slowAyah;
            Require(shown.Count == 1 && shown[0] != arabic && shown[0].Length > 0, "a late ayah cannot replace the current preview");

            shown.Clear();
            var oldSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var oldCopy = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task slowSource = preview.RunAsync(_ => oldSource.Task, shown.Add, errors.Add);
            Task slowCopy = copy.RunAsync(_ => oldCopy.Task, copied.Add, errors.Add);
            preview.Cancel();
            copy.Cancel();
            Task currentPreview = preview.RunAsync(token => translation.AyahTextAsync(1, 1, token), shown.Add, errors.Add);
            Task currentCopy = copy.RunAsync(token => translation.AyahTextAsync(1, 1, token), copied.Add, errors.Add);
            await Task.WhenAll(currentPreview, currentCopy);
            oldSource.SetResult(arabic);
            oldCopy.SetException(new IOException("stale read failure"));
            await Task.WhenAll(slowSource, slowCopy);
            Require(shown.SequenceEqual(new[] { english }) && copied.SequenceEqual(new[] { english }) && errors.Count == 0,
                "changing source discards stale preview, copy, and errors");

            var replaced = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task previousPreview = preview.RunAsync(_ => replaced.Task, shown.Add, errors.Add);
            await preview.RunAsync(token => translation.AyahTextAsync(1, 1, token), shown.Add, errors.Add);
            replaced.SetResult(arabic);
            await previousPreview;
            Require(shown.SequenceEqual(new[] { english, english }), "a newer preview wins even without a navigation event");

            Require(await translation.AyahTextAsync(1, 0, CancellationToken.None) == "", "missing text yields no clipboard update");
            string missing = Path.Combine(Path.GetTempPath(), "MissingTafseer-" + Guid.NewGuid().ToString("N") + ".db");
            await preview.RunAsync(token => new TafseerQuran(missing, missing).AyahTextAsync(1, 1, token), shown.Add, errors.Add);
            Require(errors.Count == 1 && !File.Exists(missing), "a missing source is reported without creating a database");

            // Ayah zero is absent in this source; its timing must not shift verses one and two.
            var timings = new (int Ayah, int From, int To)[] { (0, 1000, 1400), (1, 1500, 2440), (2, 2560, 3691) };
            string full = await translation.SubRipTextAsync(1, timings, relativeToFirstAyah: false);
            string page = await translation.SubRipTextAsync(1, timings.Skip(1).ToArray(), relativeToFirstAyah: true);
            string english2 = await translation.AyahTextAsync(1, 2, CancellationToken.None);
            string n = Environment.NewLine;
            Require(full == $"1{n}00:00:01,500 --> 00:00:02,440{n}{english}{n}{n}2{n}00:00:02,560 --> 00:00:03,691{n}{english2}{n}{n}",
                "surah SRT aligns the actual ayah numbers and keeps timing gaps");
            Require(page == $"1{n}00:00:00,000 --> 00:00:00,940{n}{english}{n}{n}2{n}00:00:01,060 --> 00:00:02,191{n}{english2}{n}{n}",
                "page SRT starts at zero while keeping the verse durations");
            Require(await translation.SubRipTextAsync(1, Array.Empty<(int, int, int)>(), false) == "", "no timings produces no SRT");
            Console.WriteLine("Tafseer checks passed: bundled sources, late requests, copy text, missing data and SRT.");
        }

        public static void CheckPreviewUi(string database)
        {
            Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetParent(database).FullName).FullName);
            const BindingFlags handlers = BindingFlags.Instance | BindingFlags.NonPublic;
            var visual = TextQuran.Instance;
            Require(visual.Start(database, 1, 1), "preview test opens bundled Quran");
            string clipboard = null;
            using (var form = new Form1(AppDomain.CurrentDomain.BaseDirectory, text => clipboard = text) { ShowInTaskbar = false })
            {
                // Use real controls and handlers without reading or saving the user's app settings.
                form.Load -= (EventHandler)typeof(Form1).GetMethod("Form1_Load", handlers).CreateDelegate(typeof(EventHandler), form);
                form.FormClosing -= (FormClosingEventHandler)typeof(Form1).GetMethod("Form1_FormClosing", handlers).CreateDelegate(typeof(FormClosingEventHandler), form);
                typeof(Form1).GetMethod("UseVisual", handlers).Invoke(form, new object[] { visual });
                var surahs = (ComboBox)form.Controls.Find("Surahs", true).Single();
                var sources = (ComboBox)form.Controls.Find("tafasir", true).Single();
                var ayah = (NumericUpDown)form.Controls.Find("Ayah", true).Single();
                var quickRead = (Button)form.Controls.Find("saveRTF", true).Single();
                var copy = (Button)form.Controls.Find("tafseerCopy", true).Single();
                surahs.Items.AddRange(visual.GetSurahNames());
                var tafseer = new TafseerQuran(Path.Combine("tafasir", "الميسر.db"), "Arabic");
                var translation = new TafseerQuran(Path.Combine("translations", "English - Saheeh International.db"), "English");
                sources.Items.AddRange(new object[] { tafseer, translation });
                Exception failure = null;
                form.Shown += async (sender, args) =>
                {
                    try
                    {
                        await Task.Yield();
                        ayah.Value = 1;
                        sources.SelectedIndex = 0;
                        quickRead.PerformClick();
                        await ExpectPreview(form, await tafseer.AyahTextAsync(1, 1, CancellationToken.None), "(1)");
                        ayah.Value = 2;
                        Require(form.OwnedForms.Length == 0, "navigation closes the displayed verse");
                        quickRead.PerformClick();
                        sources.SelectedIndex = 1;
                        ayah.Value = 3;
                        ayah.Value = 1;
                        quickRead.PerformClick();
                        string expected = await translation.AyahTextAsync(1, 1, CancellationToken.None);
                        await ExpectPreview(form, expected, "(1)");
                        await Task.Delay(150);
                        await ExpectPreview(form, expected, "(1)");
                        copy.PerformClick();
                        await WaitFor(() => clipboard != null);
                        Require(clipboard == expected, "the copy button sends the exact selected translation to the clipboard");
                        sources.SelectedIndex = 0;
                        Require(form.OwnedForms.Length == 0, "changing source closes the displayed translation");
                        Console.WriteLine("WinForms Tafseer checks passed: rapid navigation, source changes and copy button.");
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { form.Close(); }
                };
                Application.Run(form);
                if (failure != null) throw failure;
            }
        }

        private static async Task ExpectPreview(Form form, string expected, string verse)
        {
            await WaitFor(() => form.OwnedForms.Length > 0);
            Form[] previews = form.OwnedForms;
            Require(previews.Length == 1, $"expected one preview, found {previews.Length}");
            Require(previews[0].Text.EndsWith(verse, StringComparison.Ordinal), $"unexpected verse in preview title: {previews[0].Text}");
            string actual = NormalizeLines(previews[0].Controls.OfType<RichTextBox>().Single().Text);
            Require(actual == NormalizeLines(expected),
                $"preview text differs from the requested source: {previews[0].Text} ({actual.Length} vs {NormalizeLines(expected).Length} chars)");
        }

        private static string NormalizeLines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

        private static async Task WaitFor(Func<bool> ready)
        {
            var wait = Stopwatch.StartNew();
            while (!ready() && wait.ElapsedMilliseconds < 4000) await Task.Delay(20);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
