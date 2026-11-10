using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuranKareem
{
    // An immutable source. Each read owns its connection, including concurrent preview and export.
    internal sealed class TafseerQuran
    {
        private readonly string connectionString;
        public string Name { get; }
        public string Key { get; }

        public TafseerQuran(string file, string key)
        {
            Name = Path.GetFileNameWithoutExtension(file);
            Key = key;
            connectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(file), ReadOnly = true, FailIfMissing = true, Version = 3
            }.ConnectionString;
        }

        public override string ToString() => Name;

        private SQLiteConnection Open(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var connection = new SQLiteConnection(connectionString);
            try
            {
                connection.Open();
                using (var validation = new SQLiteCommand("SELECT type,version FROM description LIMIT 1", connection))
                using (var row = validation.ExecuteReader())
                    if (!row.Read() || row.GetInt32(0) != 4 || row.GetInt32(1) != 1)
                        throw new InvalidDataException("ملف التفسير أو الترجمة غير مدعوم.");
                cancellation.ThrowIfCancellationRequested();
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }

        public Task<string> AyahTextAsync(int surah, int ayah, CancellationToken cancellation)
        {
            return Task.Run(() =>
            {
                using (var connection = Open(cancellation))
                using (var query = new SQLiteCommand("SELECT text FROM ayat WHERE surah=@surah AND ayah=@ayah ORDER BY id LIMIT 1", connection))
                {
                    query.Parameters.AddWithValue("@surah", surah);
                    query.Parameters.AddWithValue("@ayah", ayah);
                    string text = query.ExecuteScalar() as string ?? "";
                    cancellation.ThrowIfCancellationRequested();
                    return text;
                }
            }, cancellation);
        }

        public Task<string> SubRipTextAsync(int surah, IReadOnlyList<(int Ayah, int From, int To)> timings, bool relativeToFirstAyah)
            => Task.Run(() => SubRipText(surah, timings, relativeToFirstAyah));

        private string SubRipText(int surah, IReadOnlyList<(int Ayah, int From, int To)> timings, bool relativeToFirstAyah)
        {
            if (timings == null || timings.Count == 0) return "";
            var verses = new Dictionary<int, string>();
            using (var connection = Open(CancellationToken.None))
            using (var query = new SQLiteCommand("SELECT ayah,text FROM ayat WHERE surah=@surah AND ayah BETWEEN @first AND @last ORDER BY ayah,id", connection))
            {
                query.Parameters.AddWithValue("@surah", surah);
                query.Parameters.AddWithValue("@first", timings[0].Ayah);
                query.Parameters.AddWithValue("@last", timings[timings.Count - 1].Ayah);
                using (var rows = query.ExecuteReader())
                    while (rows.Read())
                    {
                        int ayah = rows.GetInt32(0);
                        if (!rows.IsDBNull(1) && !verses.ContainsKey(ayah)) verses.Add(ayah, rows.GetString(1));
                    }
            }

            var srt = new StringBuilder();
            int index = 0, offset = relativeToFirstAyah ? timings[0].From : 0;
            foreach (var timing in timings)
            {
                if (timing.From < offset || timing.To <= timing.From ||
                    !verses.TryGetValue(timing.Ayah, out string verse) || string.IsNullOrWhiteSpace(verse)) continue;
                srt.AppendLine((++index).ToString(CultureInfo.InvariantCulture));
                srt.Append(Timecode(timing.From - offset)).Append(" --> ").AppendLine(Timecode(timing.To - offset));
                // Blank lines delimit cues in SRT, so keep paragraph breaks out of a cue.
                foreach (string line in verse.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(line)) srt.AppendLine(line.Trim());
                srt.AppendLine();
            }
            return srt.ToString();
        }

        private static string Timecode(int milliseconds)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
            return ((int)time.TotalHours).ToString("D2", CultureInfo.InvariantCulture) +
                time.ToString(@"\:mm\:ss\,fff", CultureInfo.InvariantCulture);
        }
    }

    // Used on the UI thread. Even a read that finishes after cancellation cannot publish.
    internal sealed class LatestTafseerRequest
    {
        private CancellationTokenSource active;

        public async Task RunAsync(Func<CancellationToken, Task<string>> read, Action<string> publish, Action<Exception> report)
        {
            Cancel();
            var request = new CancellationTokenSource();
            active = request;
            try
            {
                string text = await read(request.Token);
                if (active == request) publish(text);
            }
            catch (Exception error)
            {
                if (active == request) report(error);
            }
            finally
            {
                if (active == request) active = null;
                request.Dispose();
            }
        }

        public void Cancel()
        {
            var previous = active;
            active = null;
            previous?.Cancel();
        }
    }
}
