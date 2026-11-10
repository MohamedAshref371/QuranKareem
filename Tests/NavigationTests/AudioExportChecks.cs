using NAudio.Wave;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace QuranKareem.NavigationTests
{
    internal static class AudioExportChecks
    {
        public static void Run()
        {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioExportChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try { CheckWave(root); }
            finally { Directory.Delete(root, recursive: true); }
        }

        private static void CheckWave(string root)
        {
            var format = new WaveFormat(8000, 16, 1);
            var bytes = new byte[format.BlockAlign * format.SampleRate];
            new Random(471).NextBytes(bytes);
            string source = Path.Combine(root, "source.wav");
            WriteWave(source, format, bytes);

            var ranges = new[] { new AudioVerseRange(1, 73, 302), new AudioVerseRange(2, 410, 711), new AudioVerseRange(3, 900, 1200) };
            string clips = Path.Combine(root, "clips");
            var splitter = new AudioQuranSplitter(source, 2, ranges);
            Require(splitter.Export(clips, out int skipped) == 2 && skipped == 1, "export valid ranges and skip a range beyond the recording");
            byte[] first = ReadWave(Path.Combine(clips, "S002A001.wav"), format);
            byte[] second = ReadWave(Path.Combine(clips, "S002A002.wav"), format);
            Require(first.SequenceEqual(bytes.Skip(73 * 16).Take(229 * 16)), "first WAV cut has the exact samples");
            Require(second.SequenceEqual(bytes.Skip(410 * 16).Take(301 * 16)), "second WAV cut starts after the skipped gap");

            // A format mismatch must leave neither an output file nor committed timings for that surah.
            WriteWave(Path.Combine(clips, "S003A001.wav"), format, first);
            WriteWave(Path.Combine(clips, "S003A002.wav"), new WaveFormat(16000, 16, 1), second);
            AudioCombineResult combined = AudioQuranCombiner.MushafCombine(clips);
            Require(combined.Folder != null && combined.Errors.SequenceEqual(new[] { "S003A002.wav" }), "reject a mismatched WAV while retaining successful surahs");
            Require(!File.Exists(Path.Combine(combined.Folder, "003.wav")) && !Directory.GetFiles(combined.Folder, "*.part").Any(), "no incomplete merged files remain");
            Require(ReadWave(Path.Combine(combined.Folder, "002.wav"), format).SequenceEqual(first.Concat(second)), "merged WAV reopens and contains every sample in order");
            CheckDatabase(combined.Folder, ".wav", 229, 530);
            Console.WriteLine("WAV export checks passed: exact cuts, playable merged WAV, accurate timings, and format mismatch cleanup.");
        }

        // Optional integration check with a real VBR MP3 supplied by the caller. Keep its outputs for independent decoding.
        public static void CheckMp3(string source, string root)
        {
            string clips = Path.Combine(root, "cuts");
            var ranges = new[] { new AudioVerseRange(1, 73, 357), new AudioVerseRange(2, 481, 1263) };
            var splitter = new AudioQuranSplitter(source, 2, ranges);
            Require(splitter.Export(clips, out int skipped) == 2 && skipped == 0, "export two cuts from a VBR recording");
            using (var decoder = new Mp3FileReader(source))
            using (var pcm = new MemoryStream())
            {
                decoder.CopyTo(pcm);
                byte[] decoded = pcm.ToArray();
                foreach (AudioVerseRange range in ranges)
                {
                    int align = decoder.WaveFormat.BlockAlign, rate = decoder.WaveFormat.SampleRate;
                    int from = (range.From * rate + 500) / 1000 * align;
                    int to = (range.To * rate + 500) / 1000 * align;
                    Require(ReadWave(Path.Combine(clips, $"S002A{range.Ayah:D3}.wav"), decoder.WaveFormat)
                        .SequenceEqual(decoded.Skip(from).Take(to - from)), "MP3 cut has the expected PCM samples");
                }
            }

            AudioCombineResult mergedWave = AudioQuranCombiner.MushafCombine(clips);
            Require(mergedWave.Folder != null && mergedWave.Errors.Length == 0, "merge WAV cuts made from a VBR recording");
            CheckDatabase(mergedWave.Folder, ".wav", 284, 1066);

            string mergeInputs = Directory.CreateDirectory(Path.Combine(root, "merge")).FullName;
            File.Copy(source, Path.Combine(mergeInputs, "S002A001.mp3"));
            File.Copy(source, Path.Combine(mergeInputs, "S002A002.mp3"));
            AudioCombineResult merged = AudioQuranCombiner.MushafCombine(mergeInputs);
            Require(merged.Folder != null && merged.Errors.Length == 0, "combine compatible MP3 clips");
            using (var original = new Mp3FileReader(source))
            using (var combined = new Mp3FileReader(Path.Combine(merged.Folder, "002.mp3")))
            {
                Require(combined.XingHeader != null, "merged MP3 has a new duration and seek header");
                Require(combined.Length == original.Length * 2, "merged MP3 contains both sets of audio frames");
                CheckDatabase(merged.Folder, ".mp3", (int)Math.Round(original.TotalTime.TotalMilliseconds), (int)Math.Round(combined.TotalTime.TotalMilliseconds));
            }
            Console.WriteLine("VBR MP3 cut and merge checks passed. Output folder: " + root);
        }

        private static void CheckDatabase(string folder, string extension, int firstEnd, int finalEnd)
        {
            using (var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder
            {
                DataSource = Path.Combine(folder, "000.db"), ReadOnly = true, FailIfMissing = true
            }.ConnectionString))
            {
                connection.Open();
                using (var description = new SQLiteCommand("SELECT extension FROM description WHERE type=3", connection))
                    Require((string)description.ExecuteScalar() == extension, "the database records the output extension");
                using (var query = new SQLiteCommand("SELECT timestamp_from,timestamp_to FROM ayat WHERE surah=2 AND ayah BETWEEN 1 AND 2 ORDER BY ayah", connection))
                using (var rows = query.ExecuteReader())
                {
                    Require(rows.Read() && rows.GetInt32(0) == 0 && rows.GetInt32(1) == firstEnd, "first clip timing");
                    Require(rows.Read() && rows.GetInt32(0) == firstEnd && rows.GetInt32(1) == finalEnd, "second clip timing");
                }
                using (var failed = new SQLiteCommand("SELECT COUNT(*) FROM ayat WHERE surah=3 AND timestamp_to<>0", connection))
                    Require((long)failed.ExecuteScalar() == 0, "failed surah has no committed timings");
            }
        }

        private static byte[] ReadWave(string path, WaveFormat format)
        {
            using (var reader = new WaveFileReader(path))
            using (var bytes = new MemoryStream())
            {
                Require(reader.WaveFormat.Equals(format), "WAV format round-trips");
                reader.CopyTo(bytes);
                return bytes.ToArray();
            }
        }

        private static void WriteWave(string path, WaveFormat format, byte[] bytes)
        {
            using (var writer = new WaveFileWriter(path, format)) writer.Write(bytes, 0, bytes.Length);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
