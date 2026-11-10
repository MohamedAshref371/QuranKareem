using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static QuranKareem.Constants;

namespace QuranKareem
{
    internal class AudioQuran
    {
        #region Create The Class
        private bool success = false;

        private readonly SQLiteConnection quran;
        private readonly SQLiteCommand command;
        private SQLiteDataReader reader;

        public static readonly AudioQuran Instance = new AudioQuran();

        private AudioQuran()
        {
            quran = new SQLiteConnection();
            command = new SQLiteCommand(quran);
            playbackTimer.Tick += PlaybackTick;
            mp3.PlayStateChange += (sender, e) =>
            {
                if (CapturedAudio && e.newState == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
                    mediaEnded = true;
            };
        }
        #endregion

        private string path; // المسار
        public string Extension { get; private set; } // example: .mp3
        private int ayahId;

        public int SurahsCount { get; private set; }

        public int SurahNumber { get; private set; }
        public int AyahNumber { get; private set; }
        public int AyatCount { get; private set; }

        private int version;

        public int Narration { get; private set; }
        public string Comment { get; private set; }

        public int From { get; private set; }
        public int To { get; private set; }

        double rate = 1;
        public double Rate
        {
            set
            {
                if (value > 1.5) rate = 1.5;
                else if (value < 0.6) rate = 0.6;
                else rate = value;
                mp3.settings.rate = rate;
            }
        }

        public int Volume
        {
            set
            {
                if (value < 1) value = 1;
                else if (value > 100) value = 100;
                mp3.settings.volume = value;
            }
        }

        private readonly Timer playbackTimer = new Timer { Interval = 20 };
        private readonly AxWMPLib.AxWindowsMediaPlayer mp3 = new AxWMPLib.AxWindowsMediaPlayer();
        private int? pendingSeek;
        private bool paused, mediaEnded;
        private event EventHandler AyahChanged, WordChanged;
        public int CurrentWord { get; private set; } = -1;
        public bool WordMode { get; set; } = true;
        private bool isWordTableEmpty = true;

        private bool added = false;
        public void AddInControls(Control.ControlCollection Controls)
        {
            if (!added) try
                {
                    Controls.Add(mp3);
                    mp3.Visible = false;
                    added = true;
                }
                catch { }
        }

        public void Start(string path, int sura = 1, int aya = 0)
        {
            if (!added || !Directory.Exists(path)) return;

            try { path = Path.GetFullPath(path); } catch { return; }
            if (path.Substring(path.Length - 1) != "\\") { path += "\\"; }

            if (!File.Exists(path + "000.db") && !File.Exists(path + "0.db")) return;

            Stop();
            this.path = path;
            SurahNumber = 0;

            try
            {
                if (File.Exists(path + "000.db"))
                    quran.ConnectionString = "Data Source=" + path + "000.db;Version=3;";
                else
                    quran.ConnectionString = "Data Source=" + path + "0.db;Version=3;";
                quran.Open();
                command.CommandText = $"SELECT * FROM description";
                reader = command.ExecuteReader();
                if (!reader.HasRows) return;
                reader.Read();
                version = reader.GetInt32(1);
                if (reader.GetInt32(0) /*type 1:text, 2:picture, 3:audio*/ != 3 || version != 5) return;
                Narration = reader.GetInt32(2);
                SurahsCount = reader.GetInt32(3);
                Extension = reader.GetString(4);
                Comment = reader.GetString(5);
                reader.Close();

                command.CommandText = $"SELECT * FROM words LIMIT 1";
                reader = command.ExecuteReader();
                isWordTableEmpty = !reader.HasRows;

                success = true;
            }
            catch { }
            finally
            {
                reader?.Close();
                quran.Close();
            }
            if (success) Set(sura, aya);
        }

        #region التنقلات في المصحف

        /// <summary>
        /// Implementation Priority:<br/>
        ///   - Surah (1:114), its Ayah (0:{AyatCount}).<br/>
        ///   - Ayah (0:{AyatCount}) only.<br/>
        ///   - Next parameter (false,true).<br/>
        /// </summary>
        /// <param name="surah">Quran Surahs Count is 114.</param>
        /// <param name="ayah">Each Surah contains Ayat.</param>
        /// <param name="next">Go to the next Ayah.</param>
        /// <returns>true if everything goes well.</returns>
        public bool Set(int surah = 0, int ayah = -2, bool next = false)
        {
            if (!success) return false;
            playbackTimer.Stop();
            bool seek = !next;
            if (seek) RepeatReset();

            #region SQL Building
            StrBuilder.Length = 0;
            StrBuilder.Append("SELECT id,surah,ayah,timestamp_from,timestamp_to FROM ayat WHERE ");
            #region Surah and Ayah
            // Ayah
            if ((surah <= 0 || surah == SurahNumber) && ayah >= -1)
            {
                if (ayah == AyatCount + 1)
                {
                    surah = SurahNumber != SurahsCount ? SurahNumber + 1 : 1;
                    ayah = 0;
                }
                else if (ayah > AyatCount + 1)
                {
                    surah = SurahNumber;
                    ayah = AyatCount;
                }
                else
                    surah = SurahNumber;

                if (ayah < 0) ayah = 0;
                StrBuilder.Append($"surah>={surah} AND ayah>={ayah}");
            }

            // Surah and Ayah
            else if (surah >= 1 && surah <= SurahsCount + 1)
            {
                if (surah == SurahsCount + 1) surah = 1;
                if (ayah < 0) ayah = 0;
                StrBuilder.Append($"surah>={surah} AND ayah>={ayah}");
            }

            // Ayah Plus
            else if (next && SurahNumber >= 1)
            {
                if (AyahRepeatCounter < AyahRepeat - 1 && AyahNumber != 0)
                {
                    AyahRepeatCounter += 1;
                    seek = true;
                    StrBuilder.Append($"id={ayahId}");
                }
                else if (AyahNumber == AyatCount && SurahRepeatCounter < SurahRepeat - 1)
                {
                    SurahRepeatCounter += 1;
                    AyahRepeatCounter = 0;
                    seek = true;
                    StrBuilder.Append($"surah={SurahNumber} AND ayah>=0");
                }
                else
                {
                    AyahRepeatCounter = 0;
                    if (SurahNumber == SurahsCount && AyahNumber == AyatCount)
                    {
                        seek = true;
                        StrBuilder.Append("ayah>=0");
                    }
                    else
                        StrBuilder.Append($"id>{ayahId} AND ayah>=0");
                }
            }
            #endregion
            else
            {
                StrBuilder.Append($"surah>={SurahNumber} AND ayah>={AyahNumber}");
            }
            StrBuilder.Append(" LIMIT 1");
            #endregion

            #region SQL Execution
            quran.Open();
            command.CommandText = StrBuilder.ToString();
            reader = command.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close(); quran.Close();
                return false;
            }
            int id = reader.GetInt32(0);
            surah = reader.GetInt32(1);
            ayah = reader.GetInt32(2);
            From = reader.GetInt32(3);
            To = reader.GetInt32(4);
            reader.Close(); quran.Close();
            #endregion

            if (surah != SurahNumber || !CapturedAudio)
            {
                if (!CaptureAudio(surah)) return false;
                SurahData(surah);
                seek = true;
            }
            else if (seek && (paused || mediaEnded))
                mp3.Ctlcontrols.play();
            paused = false;
            if (seek) mediaEnded = false;
            ayahId = id;
            AyahNumber = ayah;

            LoadWordTimings();
            CurrentWord = -1;
            pendingSeek = seek ? (int?)Math.Max(0, From) : null;
            playbackTimer.Start();
            return true;
        }

        private void SurahData(int surah)
        {
            SurahNumber = surah;
            RepeatReset();
            command.CommandText = $"SELECT ayat_count FROM surahs WHERE id={surah}";
            quran.Open();
            reader = command.ExecuteReader();
            if (reader.Read())
                AyatCount = reader.GetInt32(0);
            reader.Close(); quran.Close();
        }

        private struct WordTiming
        {
            public int Word, Start, End;
        }

        private readonly List<WordTiming> wordTimings = new List<WordTiming>();
        private void LoadWordTimings()
        {
            wordTimings.Clear();
            if (isWordTableEmpty) return;

            quran.Open();
            try
            {
                command.CommandText = $"SELECT word,timestamp_from,timestamp_to FROM words WHERE ayah_id={ayahId} AND word>0 AND timestamp_from>=0 AND timestamp_to>timestamp_from ORDER BY timestamp_from,word";
                using (SQLiteDataReader timings = command.ExecuteReader())
                    while (timings.Read())
                        wordTimings.Add(new WordTiming { Word = timings.GetInt32(0), Start = timings.GetInt32(1), End = timings.GetInt32(2) });
            }
            finally { quran.Close(); }
        }

        public void WordOf(int word)
        {
            if (!success || !WordMode || !playbackTimer.Enabled || word < 1) return;
            foreach (WordTiming timing in wordTimings)
            {
                if (timing.Word == word && timing.Start < To)
                {
                    pendingSeek = timing.Start;
                    return;
                }
            }
        }

        public bool Check(int delta = 0)
        {
            if (success && CaptureAudio(SurahNumber))
            {
                quran.Open();
                command.CommandText = $"SELECT duration FROM surahs WHERE id={SurahNumber}";
                reader = command.ExecuteReader();
                reader.Read();
                if (reader.IsDBNull(0)) { quran.Close(); return false; }
                int i = reader.GetInt32(0);
                reader.Close();
                quran.Close();
                return Math.Abs(mp3.currentMedia.duration - i) <= delta;
            }
            return false;
        }

        public bool CapturedAudio = false;
        bool CaptureAudio(int sura)
        {
            string source = FindSurahAudio(sura);
            if (source == null)
            {
                OneTimePause();
                return false;
            }
            CapturedAudio = true;
            mp3.URL = source;
            mp3.settings.rate = rate;
            return true;
        }

        private string FindSurahAudio(int surah)
        {
            if (!Directory.Exists(path)) return null;
            string number = surah.ToString("D3");
            string numbered = Path.Combine(path, number + Extension);
            if (File.Exists(numbered)) return numbered;
            numbered = Path.Combine(path, surah + Extension);
            if (File.Exists(numbered)) return numbered;
            string alternate = Directory.EnumerateFiles(path, "*" + Extension)
                .LastOrDefault(file => Path.GetFileNameWithoutExtension(file).Contains(number));
            if (alternate != null) return alternate;
            // Older exports could contain WAV files while the database still listed MP3.
            return Directory.EnumerateFiles(path).LastOrDefault(file =>
                Path.GetFileNameWithoutExtension(file).Contains(number) &&
                (Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                 Path.GetExtension(file).Equals(".mp3", StringComparison.OrdinalIgnoreCase)));
        }
        #endregion

        public void OneTimePause()
        {
            playbackTimer.Stop();
            pendingSeek = null;
            paused = mediaEnded = false;
            bool hadAudio = CapturedAudio;
            CapturedAudio = false;
            if (hadAudio) mp3.URL = "";
        }

        public bool Pause()
        {
            if (!CapturedAudio || paused) return false;
            playbackTimer.Stop();
            paused = true;
            mp3.Ctlcontrols.pause();
            return true;
        }

        public bool Resume(int surah, int ayah)
        {
            if (!paused || !CapturedAudio || surah != SurahNumber || ayah != AyahNumber) return false;
            paused = false;
            mp3.Ctlcontrols.play();
            playbackTimer.Start();
            return true;
        }

        public void Stop()
        {
            success = false;
            OneTimePause();
            CurrentWord = -1;
        }

        private void PlaybackTick(object sender, EventArgs e)
        {
            if (!CapturedAudio || paused) return;
            bool playing = mp3.playState == WMPLib.WMPPlayState.wmppsPlaying;
            if (pendingSeek.HasValue)
            {
                // WMP does not reliably accept a position until the media has started.
                if (playing)
                {
                    int target = pendingSeek.Value;
                    pendingSeek = null;
                    mediaEnded = false;
                    mp3.Ctlcontrols.currentPosition = target / 1000.0;
                }
                return;
            }

            if (!playing && !mediaEnded) return;
            double position = mediaEnded ? Mp3Duration() * 1000 : Mp3CurrentPosition() * 1000;
            bool advanced = false;
            // Catch up after a busy UI without displaying each expired ayah.
            for (int step = 0; step < 32; step++)
            {
                if ((To <= 0 && AyahNumber > 0) || (position < To && !(mediaEnded && AyahNumber == AyatCount))) break;
                if (!Set(next: true)) break;
                advanced = true;
                if (pendingSeek.HasValue) break; // repeat, wrap, or a new recording
            }
            if (advanced) AyahChanged?.Invoke(this, EventArgs.Empty);
            if (mediaEnded) playbackTimer.Stop();
            if (!CapturedAudio || pendingSeek.HasValue || (To > 0 && position >= To)) return;

            int word = -1;
            if (WordMode)
                foreach (WordTiming timing in wordTimings)
                {
                    if (timing.Start > position) break;
                    if (position < timing.End) word = timing.Word;
                }
            if (CurrentWord == word) return;
            CurrentWord = word;
            WordChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddEventHandlerOfAyah(EventHandler eH) => AyahChanged += eH;
        public void AddEventHandlerOfWord(EventHandler eH) => WordChanged += eH;

        int SurahRepeat = 1, AyahRepeat = 1, SurahRepeatCounter = 0, AyahRepeatCounter = 0;
        public void Repeat(int SurahRepeat = 1, int AyahRepeat = 1)
        {
            this.SurahRepeat = (SurahRepeat >= 1) ? SurahRepeat : 1;
            this.AyahRepeat = (AyahRepeat >= 1) ? AyahRepeat : 1;
        }
        public void RepeatReset()
        {
            SurahRepeatCounter = 0;
            AyahRepeatCounter = 0;
        }

        public AudioQuranSplitter PrepareSurahSplit()
        {
            if (!success || SurahNumber < 1) return null;
            int surah = SurahNumber;
            string source = FindSurahAudio(surah);
            if (source == null) return null;

            var verses = new List<AudioVerseRange>();
            using (var query = new SQLiteCommand("SELECT ayah, timestamp_from, timestamp_to FROM ayat WHERE surah=@surah AND ayah>=1 ORDER BY ayah", quran))
            {
                query.Parameters.AddWithValue("@surah", surah);
                quran.Open();
                try
                {
                    using (SQLiteDataReader rows = query.ExecuteReader())
                        while (rows.Read())
                            verses.Add(new AudioVerseRange(rows.GetInt32(0), rows.IsDBNull(1) ? 0 : rows.GetInt32(1), rows.IsDBNull(2) ? 0 : rows.GetInt32(2)));
                }
                finally { quran.Close(); }
            }
            return new AudioQuranSplitter(source, surah, verses);
        }

        public float[] WordsList(int ayahStart, int ayahEnd, out string mp3Url, List<int> ayahword, List<float> timestamps)
        {
            mp3Url = mp3.URL;
            ayahword.Clear();
            timestamps.Clear();

            if (!success || isWordTableEmpty || mp3.URL == "" || ayahword == null || timestamps == null) return null;

            int tStart = 0, tEnd = 0, to;
            quran.Open();

            command.CommandText = $"SELECT MIN(id),MAX(id) FROM ayat WHERE surah={SurahNumber} AND ayah>={ayahStart} AND ayah<={ayahEnd}";
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                tStart = reader.GetInt32(0);
                tEnd = reader.GetInt32(1);
            }
            reader.Close(); command.Cancel();

            command.CommandText = $"SELECT MIN(timestamp_to),MAX(timestamp_to) FROM ayat WHERE id>={tStart - 1} AND id<={tEnd}";
            reader = command.ExecuteReader();
            if (reader.Read())
            {
                tStart = reader.GetInt32(0);
                tEnd = reader.GetInt32(1);
            }
            reader.Close(); command.Cancel();

            command.CommandText = $"SELECT ayah,word,words.timestamp_from,words.timestamp_to FROM ayat JOIN words ON ayat.id = words.ayah_id WHERE surah={SurahNumber} AND ayah>={ayahStart} AND ayah<={ayahEnd} ORDER BY words.timestamp_from";
            reader = command.ExecuteReader();

            to = tStart;

            while (reader.Read())
            {
                if (reader.GetInt32(2) > to)
                {
                    if (reader.GetInt32(2) - to <= 12)
                    {
                        timestamps[timestamps.Count - 1] += (reader.GetInt32(2) - to) / 1000f;
                    }
                    else
                    {
                        ayahword.Add(reader.GetInt32(0));
                        ayahword.Add(-1);
                        timestamps.Add((reader.GetInt32(2) - to) / 1000f);
                    }
                }
                ayahword.Add(reader.GetInt32(0));
                ayahword.Add(reader.GetInt32(1));
                to = reader.GetInt32(3);
                timestamps.Add((to - reader.GetInt32(2)) / 1000f);
            }
            reader.Close(); quran.Close();

            if (tEnd - to > 1)
            {
                ayahword.Add(-1);
                ayahword.Add(-1);
                timestamps.Add((tEnd - to) / 1000f);
            }
            return new float[2] { tStart / 1000f, tEnd / 1000f };
        }

        // Mp3 Current Position String
        public string GetCurrentPosition() => GetPositionOf(From);
        public string GetPositionOf(int num)
        {
            StrBuilder.Clear();

            int temp = num / 3600000;
            StrBuilder.Append(temp.ToString().PadLeft(2, '0')).Append(":");

            num -= temp * 3600000;
            temp = num / 60000;
            StrBuilder.Append(temp.ToString().PadLeft(2, '0')).Append(":");

            num -= temp * 60000;
            temp = num / 1000;
            StrBuilder.Append( temp.ToString().PadLeft(2, '0') ).Append(".");

            num -= temp * 1000;
            StrBuilder.Append(num.ToString().PadLeft(3, '0'));

            return StrBuilder.ToString();
        }

        public string[] GetPositionsOf(int surah, int ayahStart = -1, int ayahEnd = 287)
        {
            if (!success) return null;
            int[] timestampsT = GetTimestamps(surah, ayahStart, ayahEnd);
            string[] timestamps = new string[timestampsT.Length];

            for (int i = 0; i < timestamps.Length; i++)
                timestamps[i] = GetPositionOf(timestampsT[i]);

            return timestamps;
        }

        internal IReadOnlyList<(int Ayah, int From, int To)> GetSubtitleTimings(int surah, int firstAyah, int lastAyah)
        {
            var timings = new List<(int, int, int)>();
            if (!success) return timings;
            var settings = new SQLiteConnectionStringBuilder(quran.ConnectionString) { ReadOnly = true, FailIfMissing = true };
            using (var connection = new SQLiteConnection(settings.ConnectionString))
            using (var query = new SQLiteCommand(@"SELECT ayah,timestamp_from,timestamp_to FROM ayat
                WHERE surah=@surah AND ayah BETWEEN @first AND @last AND timestamp_from>=0
                AND timestamp_to>timestamp_from ORDER BY ayah,id", connection))
            {
                query.Parameters.AddWithValue("@surah", surah);
                query.Parameters.AddWithValue("@first", Math.Max(0, firstAyah));
                query.Parameters.AddWithValue("@last", lastAyah);
                connection.Open();
                using (var rows = query.ExecuteReader())
                    while (rows.Read())
                        timings.Add((rows.GetInt32(0), rows.GetInt32(1), rows.GetInt32(2)));
            }
            return timings;
        }

        #region إضافة شيخ جديد
        public bool NewQuranAudio(string path)
        {
            if (!added || !Directory.Exists(path)) return false;

            try { path = Path.GetFullPath(path); } catch { return false; }
            if (path.Substring(path.Length - 1) != "\\") { path += "\\"; }

            Stop();
            this.path = path;
            try
            {
                if (!File.Exists(path + "000.db") && !File.Exists(path + "0.db"))
                {
                    if (!File.Exists(@"audios\database for audios.db")) return false;
                    File.Copy(@"audios\database for audios.db", path + "000.db");
                }

                if (File.Exists(path + "000.db"))
                    quran.ConnectionString = "Data Source=" + path + "000.db;Version=3;";
                else
                    quran.ConnectionString = "Data Source=" + path + "0.db;Version=3;";

                quran.Open();
                command.CommandText = $"SELECT * FROM description";
                reader = command.ExecuteReader();
                if (!reader.HasRows) return false;
                reader.Read();
                version = reader.GetInt32(1);
                if (reader.GetInt32(0) != 3 || version != 5) return false;
                Narration = reader.GetInt32(2);
                SurahsCount = reader.GetInt32(3);
                Extension = reader.GetString(4);
                Comment = reader.GetString(5);

                reader.Close(); command.Cancel();
                command.CommandText = $"SELECT * FROM words LIMIT 1";
                reader = command.ExecuteReader();
                isWordTableEmpty = !reader.HasRows;

                success = true;
                return true;
            }
            catch { return false; }
            finally
            {
                reader?.Close();
                quran.Close();
            }
        }

        public int[] GetTimestamps(int sura, int ayahStart = -1, int ayahEnd = 287)
        {
            if (!success) return null;
            int shift = 0;
            List<int> list = new List<int>();
            quran.Open();
            command.CommandText = $"SELECT timestamp_to FROM ayat WHERE surah={sura} AND ayah>={ayahStart - 1} AND ayah<={ayahEnd}";
            reader = command.ExecuteReader();
            if (ayahStart >= 0 && reader.Read())
                    shift = reader.GetInt32(0);
            
            while (reader.Read()) list.Add(reader.GetInt32(0) - shift);
            reader.Close();
            quran.Close();
            return list.ToArray();
        }

        public void SetSurah(int sura, int duration)
        {
            if (!success) return;
            quran.Open();
            command.CommandText = $"UPDATE surahs SET duration={duration} WHERE id={sura}";
            command.ExecuteNonQuery();
            quran.Close();
        }

        public void SetAyah(int sura, int aya, int timestampTo)
        {
            if (!success) return;
            quran.Open();
            command.CommandText = $"UPDATE ayat SET timestamp_to={timestampTo} WHERE surah={sura} AND ayah={aya};";
            command.CommandText += $"UPDATE ayat SET timestamp_from={timestampTo} WHERE surah={sura} AND ayah={aya + 1}";
            command.ExecuteNonQuery();
            quran.Close();
        }

        public bool SetDescription(string extension, string comment)
        {
            if (!success) return false;

            var settings = new SQLiteConnectionStringBuilder(quran.ConnectionString) { FailIfMissing = true };
            using (var connection = new SQLiteConnection(settings.ConnectionString))
            using (var update = new SQLiteCommand("UPDATE description SET extension = @extension, comment = @comment WHERE type = 3", connection))
            {
                update.Parameters.AddWithValue("@extension", extension ?? "");
                update.Parameters.AddWithValue("@comment", comment ?? "");
                connection.Open();
                if (update.ExecuteNonQuery() != 1) return false;
            }

            Extension = extension ?? "";
            Comment = comment ?? "";
            return true;
        }

        public double Mp3CurrentPosition() => mp3.Ctlcontrols.currentPosition;
        public double Mp3Duration() => mp3.currentMedia != null ? mp3.currentMedia.duration : 0;
        #endregion
    }
}
