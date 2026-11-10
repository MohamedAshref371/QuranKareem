using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuranKareem
{
    internal sealed class AudioCombineResult
    {
        public readonly string Folder;
        public readonly string[] Errors;

        public AudioCombineResult(string folder, IEnumerable<string> errors)
        {
            Folder = folder;
            Errors = errors.ToArray();
        }
    }

    internal static class AudioQuranCombiner
    {
        private static readonly Regex VerseName = new Regex(@"^(s([0-9]{1,3})a([0-9]{1,3})|s?([0-9]{3})a?([0-9]{3}))\.(mp3|wav)$", RegexOptions.IgnoreCase);

        public static AudioCombineResult MushafCombine(string folder)
        {
            folder = Path.GetFullPath(folder);
            string template = Path.GetFullPath(Path.Combine("audios", "database for audios.db"));
            if (!Directory.Exists(folder) || File.Exists(Path.Combine(folder, "000.db")) || File.Exists(Path.Combine(folder, "0.db"))
                || !File.Exists(template)) return null;

            AudioFile[] files = Directory.EnumerateFiles(folder).Select(ParseFile).Where(file => file != null)
                .OrderBy(file => file.Surah).ThenBy(file => file.Ayah).ToArray();
            if (files.Length == 0) return null;

            string extension = Path.GetExtension(files[0].Path).ToLowerInvariant();
            if (files.Any(file => !extension.Equals(Path.GetExtension(file.Path), StringComparison.OrdinalIgnoreCase)))
                return new AudioCombineResult(null, new[] { "استخدم صيغة واحدة لكل الملفات: WAV أو MP3." });
            string[] invalid = files.GroupBy(file => new { file.Surah, file.Ayah })
                .Where(group => group.Count() > 1 || group.Key.Surah < 1 || group.Key.Surah > 114)
                .SelectMany(group => group.Select(file => Path.GetFileName(file.Path))).ToArray();
            if (invalid.Length != 0) return new AudioCombineResult(null, invalid);

            using (var source = new SQLiteConnection(new SQLiteConnectionStringBuilder { DataSource = template, ReadOnly = true, FailIfMissing = true }.ConnectionString))
            using (var check = new SQLiteCommand("SELECT 1 FROM description WHERE type=3 AND version=5", source))
            {
                source.Open();
                if (check.ExecuteScalar() == null) return new AudioCombineResult(null, new[] { Path.GetFileName(template) });
            }

            string outputFolder = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputFolder);
            string outputDatabase = Path.Combine(outputFolder, "000.db");
            File.Copy(template, outputDatabase);

            var errors = new List<string>();
            var buffer = new byte[65536];
            int completed = 0;
            using (var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder { DataSource = outputDatabase, FailIfMissing = true }.ConnectionString))
            {
                connection.Open();
                using (var description = new SQLiteCommand("UPDATE description SET extension=@extension WHERE type=3", connection))
                {
                    description.Parameters.AddWithValue("@extension", extension);
                    description.ExecuteNonQuery();
                }

                foreach (var group in files.GroupBy(file => file.Surah))
                {
                    string target = Path.Combine(outputFolder, $"{group.Key:D3}{extension}");
                    string staged = target + ".part";
                    AudioFile current = group.First();
                    bool stagedHere = false, published = false, committed = false;
                    try
                    {
                        var timings = new List<AudioVerseRange>();
                        using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write))
                        {
                            stagedHere = true;
                            if (extension == ".wav") WriteWave(group, output, buffer, timings, ref current);
                            else WriteMp3(group, output, timings, ref current);
                        }

                        using (var transaction = connection.BeginTransaction())
                        {
                            foreach (AudioVerseRange range in timings)
                            {
                                current = group.First(file => file.Ayah == range.Ayah);
                                SaveTiming(connection, transaction, group.Key, range);
                            }

                            using (var update = new SQLiteCommand("UPDATE surahs SET duration=@duration WHERE id=@surah", connection, transaction))
                            {
                                update.Parameters.AddWithValue("@duration", (int)Math.Round(timings.Last().To / 1000.0));
                                update.Parameters.AddWithValue("@surah", group.Key);
                                update.ExecuteNonQuery();
                            }

                            File.Move(staged, target);
                            published = true;
                            transaction.Commit();
                            committed = true;
                        }
                        completed++;
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        Trace.TraceWarning("Could not combine {0}: {1}", current.Path, ex.Message);
                        errors.Add(Path.GetFileName(current.Path));
                    }
                    finally
                    {
                        if (stagedHere && !published) File.Delete(staged);
                        if (published && !committed) File.Delete(target);
                    }
                }
            }

            if (completed != 0) return new AudioCombineResult(outputFolder, errors);
            File.Delete(outputDatabase);
            Directory.Delete(outputFolder);
            return new AudioCombineResult(null, errors);
        }

        private static void WriteWave(IEnumerable<AudioFile> files, Stream output, byte[] buffer, List<AudioVerseRange> timings, ref AudioFile current)
        {
            WaveFileWriter writer = null;
            byte[] format = null;
            long frames = 0;
            try
            {
                foreach (AudioFile file in files)
                {
                    current = file;
                    using (var reader = new WaveFileReader(file.Path))
                    {
                        WaveFormat wave = reader.WaveFormat;
                        if (!AudioQuranSplitter.IsLinearWave(wave) || reader.Length == 0 || reader.Length % wave.BlockAlign != 0)
                            throw new InvalidDataException("ملف WAV فارغ أو بصيغة غير مدعومة.");

                        byte[] currentFormat = WaveFormatBytes(wave);
                        if (writer == null)
                        {
                            format = currentFormat;
                            writer = new WaveFileWriter(output, wave);
                        }
                        else if (!format.SequenceEqual(currentFormat))
                            throw new InvalidDataException("ملفات السورة لا تستخدم نفس إعدادات WAV.");

                        int read;
                        int chunk = buffer.Length / wave.BlockAlign * wave.BlockAlign;
                        if (chunk == 0) throw new InvalidDataException("تنسيق عيّنات WAV غير مدعوم.");
                        long copied = 0;
                        while ((read = reader.Read(buffer, 0, chunk)) > 0)
                        {
                            writer.Write(buffer, 0, read);
                            copied += read;
                        }
                        if (copied != reader.Length) throw new EndOfStreamException("ملف WAV غير مكتمل.");

                        long end = frames + copied / wave.BlockAlign;
                        timings.Add(new AudioVerseRange(file.Ayah, Milliseconds(frames, wave.SampleRate), Milliseconds(end, wave.SampleRate)));
                        frames = end;
                    }
                }
            }
            finally { writer?.Dispose(); }
        }

        private static void WriteMp3(IEnumerable<AudioFile> files, Stream output, List<AudioVerseRange> timings, ref AudioFile current)
        {
            Mp3Frame first = null;
            byte[] header = null;
            int xing = 0;
            var offsets = new List<long>();
            long samples = 0;
            foreach (AudioFile file in files)
            {
                current = file;
                long start = samples;
                using (var reader = new Mp3FileReader(file.Path))
                {
                    Mp3Frame frame;
                    while ((frame = reader.ReadNextFrame()) != null)
                    {
                        if (first == null)
                        {
                            first = frame;
                            header = CreateXingFrame(frame, out xing);
                            output.Write(header, 0, header.Length);
                        }
                        if (frame.SampleRate != first.SampleRate || frame.MpegVersion != first.MpegVersion || frame.MpegLayer != first.MpegLayer
                            || (frame.ChannelMode == ChannelMode.Mono) != (first.ChannelMode == ChannelMode.Mono))
                            throw new InvalidDataException("ملفات السورة لا تستخدم نفس إعدادات MP3.");

                        offsets.Add(output.Position);
                        output.Write(frame.RawData, 0, frame.RawData.Length);
                        samples += frame.SampleCount;
                    }
                }
                if (samples == start) throw new InvalidDataException("ملف MP3 لا يحتوي على صوت.");
                timings.Add(new AudioVerseRange(file.Ayah, Milliseconds(start, first.SampleRate), Milliseconds(samples, first.SampleRate)));
            }

            long size = output.Position;
            WriteBigEndian(header, xing + 8, (uint)offsets.Count);
            WriteBigEndian(header, xing + 12, checked((uint)size));
            for (int i = 1; i < 100; i++)
                header[xing + 16 + i] = (byte)Math.Min(255, offsets[(int)((long)i * offsets.Count / 100)] * 256 / size);
            output.Position = 0;
            output.Write(header, 0, header.Length);
        }

        // A new Xing frame describes the combined stream; the original clips' headers cannot be reused.
        private static byte[] CreateXingFrame(Mp3Frame frame, out int tagOffset)
        {
            if (frame.MpegLayer != MpegLayer.Layer3) throw new InvalidDataException("ملف MP3 لا يستخدم MPEG Layer III.");
            bool version1 = frame.MpegVersion == MpegVersion.Version1;
            bool mono = frame.ChannelMode == ChannelMode.Mono;
            // Use the highest legal bitrate index so the frame has room for the seek table.
            int bitrate = version1 ? 320000 : 160000;
            var bytes = new byte[frame.SampleCount / 8 * bitrate / frame.SampleRate];
            Buffer.BlockCopy(frame.RawData, 0, bytes, 0, 4);
            bytes[1] |= 1; // No CRC.
            bytes[2] = (byte)((bytes[2] & 0x0D) | 0xE0); // Bitrate index 14, no padding.
            tagOffset = 4 + (version1 ? (mono ? 17 : 32) : (mono ? 9 : 17));
            bytes[tagOffset] = (byte)'X';
            bytes[tagOffset + 1] = (byte)'i';
            bytes[tagOffset + 2] = (byte)'n';
            bytes[tagOffset + 3] = (byte)'g';
            WriteBigEndian(bytes, tagOffset + 4, 7); // Frame count, byte count, and seek table follow.
            return bytes;
        }

        private static void WriteBigEndian(byte[] bytes, int offset, uint value)
        {
            for (int i = 3; i >= 0; i--)
            {
                bytes[offset + i] = (byte)(value & 255);
                value >>= 8;
            }
        }

        private static byte[] WaveFormatBytes(WaveFormat format)
        {
            using (var bytes = new MemoryStream())
            using (var writer = new BinaryWriter(bytes))
            {
                format.Serialize(writer);
                return bytes.ToArray();
            }
        }

        private static int Milliseconds(long frames, int sampleRate) => checked((int)((frames * 1000 + sampleRate / 2) / sampleRate));

        private static void SaveTiming(SQLiteConnection connection, SQLiteTransaction transaction, int surah, AudioVerseRange range)
        {
            using (var update = new SQLiteCommand("UPDATE ayat SET timestamp_from=@from, timestamp_to=@to WHERE surah=@surah AND ayah=@ayah", connection, transaction))
            {
                update.Parameters.AddWithValue("@from", range.From);
                update.Parameters.AddWithValue("@to", range.To);
                update.Parameters.AddWithValue("@surah", surah);
                update.Parameters.AddWithValue("@ayah", range.Ayah);
                if (update.ExecuteNonQuery() != 1) throw new InvalidDataException("رقم الآية غير موجود في قاعدة التوقيتات.");
            }
        }

        private static AudioFile ParseFile(string file)
        {
            Match match = VerseName.Match(Path.GetFileName(file));
            if (!match.Success) return null;
            GroupCollection groups = match.Groups;
            return new AudioFile
            {
                Surah = int.Parse(groups[2].Success ? groups[2].Value : groups[4].Value),
                Ayah = int.Parse(groups[3].Success ? groups[3].Value : groups[5].Value),
                Path = file
            };
        }

        private sealed class AudioFile
        {
            public int Surah, Ayah;
            public string Path;
        }
    }
}
