using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuranKareem
{
    internal readonly struct AudioVerseRange
    {
        public readonly int Ayah, From, To;

        public AudioVerseRange(int ayah, int from, int to)
        {
            Ayah = ayah;
            From = from;
            To = to;
        }
    }

    internal sealed class AudioQuranSplitter
    {
        private readonly string source;
        private readonly int surah;
        private readonly IReadOnlyList<AudioVerseRange> verses;

        public AudioQuranSplitter(string source, int surah, IReadOnlyList<AudioVerseRange> verses)
        {
            this.source = source;
            this.surah = surah;
            this.verses = verses;
        }

        // Decode MP3 from the start so a cut never depends on MPEG data in a preceding frame.
        // WAV output preserves the samples at the cut without another lossy encoding pass.
        public int Export(string folder, out int skipped)
        {
            skipped = 0;
            int written = 0;
            using (WaveStream input = OpenSource(source))
            {
                var format = input.WaveFormat;
                var buffer = new byte[65536 / format.BlockAlign * format.BlockAlign];
                long position = 0;
                foreach (AudioVerseRange verse in verses)
                {
                    if (verse.From < 0 || verse.To <= verse.From)
                    {
                        skipped++;
                        continue;
                    }

                    long start = SampleOffset(verse.From, format);
                    long end = Math.Min(input.Length, SampleOffset(verse.To, format));
                    if (start < position || end <= start || verse.To > input.TotalTime.TotalMilliseconds + 1)
                    {
                        skipped++;
                        continue;
                    }

                    Transfer(input, null, buffer, start - position);
                    Directory.CreateDirectory(folder);
                    string output = Path.Combine(folder, $"S{surah:D3}A{verse.Ayah:D3}.wav");
                    using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                    {
                        try
                        {
                            using (var writer = new WaveFileWriter(file, format))
                                Transfer(input, writer, buffer, end - start);
                        }
                        catch
                        {
                            file.Dispose();
                            File.Delete(output);
                            throw;
                        }
                    }

                    position = end;
                    written++;
                }
            }
            return written;
        }

        private static WaveStream OpenSource(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)) return new Mp3FileReader(path);
            if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var reader = new WaveFileReader(path);
                if (IsLinearWave(reader.WaveFormat)) return reader;
                reader.Dispose();
            }
            throw new NotSupportedException("القص يدعم MP3 وملفات WAV غير المضغوطة.");
        }

        internal static bool IsLinearWave(WaveFormat format)
        {
            WaveFormat simple = format is WaveFormatExtensible extensible ? extensible.ToStandardWaveFormat() : format;
            return (simple.Encoding == WaveFormatEncoding.Pcm || simple.Encoding == WaveFormatEncoding.IeeeFloat)
                && format.SampleRate > 0 && format.BlockAlign > 0 && format.AverageBytesPerSecond == (long)format.SampleRate * format.BlockAlign;
        }

        private static long SampleOffset(int milliseconds, WaveFormat format)
            => ((long)milliseconds * format.SampleRate + 500) / 1000 * format.BlockAlign;

        private static void Transfer(WaveStream input, Stream output, byte[] buffer, long bytes)
        {
            while (bytes > 0)
            {
                int read = input.Read(buffer, 0, (int)Math.Min(bytes, buffer.Length));
                if (read == 0) throw new EndOfStreamException("انتهى الملف الصوتي قبل التوقيت المحدد.");
                output?.Write(buffer, 0, read);
                bytes -= read;
            }
        }
    }
}
