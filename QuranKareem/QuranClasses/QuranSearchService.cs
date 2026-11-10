using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace QuranKareem
{
    internal sealed class QuranSearchResult
    {
        public int Surah { get; }
        public int Ayah { get; }
        public string Preview { get; }

        public QuranSearchResult(int surah, int ayah, string preview)
        {
            Surah = surah;
            Ayah = ayah;
            Preview = preview;
        }
    }

    internal sealed class QuranSearchService
    {
        private sealed class Verse
        {
            public readonly int Surah;
            public readonly int Ayah;
            public readonly string Text;
            public readonly string NormalizedText;

            public Verse(int surah, int ayah, string text)
            {
                Surah = surah;
                Ayah = ayah;
                Text = text;
                NormalizedText = Normalize(text);
            }
        }

        private readonly string databasePath;
        private readonly object loadLock = new object();
        private Task<Verse[]> versesTask;

        public QuranSearchService(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("A database path is required.", nameof(databasePath));
            this.databasePath = databasePath;
        }

        public async Task<QuranSearchResult[]> SearchAsync(string words, bool spellingErrors, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(words)) return new QuranSearchResult[0];
            words = words.Trim();

            Task<Verse[]> load;
            lock (loadLock)
            {
                if (versesTask == null || versesTask.IsFaulted || versesTask.IsCanceled)
                    versesTask = Task.Run(() => LoadVerses());
                load = versesTask;
            }

            Verse[] verses = await load.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => Search(verses, words, spellingErrors, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        private Verse[] LoadVerses()
        {
            var verses = new List<Verse>();
            using (var connection = new SQLiteConnection("Data Source=" + databasePath + ";Read Only=True;"))
            using (var command = new SQLiteCommand("SELECT surah, ayah, abstract_text FROM ayat ORDER BY id", connection))
            {
                connection.Open();
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        verses.Add(new Verse(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
                }
            }
            return verses.ToArray();
        }

        private static QuranSearchResult[] Search(Verse[] verses, string words, bool spellingErrors, CancellationToken cancellationToken)
        {
            string query = spellingErrors ? Normalize(words) : words;
            var results = new List<QuranSearchResult>();
            for (int i = 0; i < verses.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Verse verse = verses[i];
                string text = spellingErrors ? verse.NormalizedText : verse.Text;
                if (text.IndexOf(query, StringComparison.Ordinal) < 0) continue;
                string preview = verse.Text.Length > 60 ? verse.Text.Substring(0, 60) + "..." : verse.Text;
                results.Add(new QuranSearchResult(verse.Surah, verse.Ayah, preview));
            }
            return results.ToArray();
        }

        internal static string Normalize(string text, bool taaMarbota = true)
        {
            text = text.Replace(" ", "")
                .Replace("ى", "ا").Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ئ", "ا").Replace("ء", "ا").Replace("ؤ", "ا")
                .Replace("ذ", "ز").Replace("ظ", "ز");
            text = taaMarbota ? text.Replace("ة", "ت") : text.Replace("ة", "").Replace("ه", "");
            return text.Replace("ط", "ت").Replace("ث", "س").Replace("ص", "س").Replace("ض", "د").Replace("ق", "ك");
        }
    }
}
