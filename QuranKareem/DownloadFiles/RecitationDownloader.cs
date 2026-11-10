using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QuranKareem
{
    internal sealed class RecitationDownloader
    {
        private readonly HttpClient client;

        public RecitationDownloader(HttpClient client) => this.client = client;

        // False means an existing file was kept without requesting a replacement.
        public async Task<bool> DownloadAsync(Uri source, string destination, bool replaceExisting,
            IProgress<int?> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!replaceExisting && File.Exists(destination)) return false;

            string temporaryPath = null;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, source))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidDataException("لم يرسل الخادم ملفًا كاملاً.");

                    var headers = response.Content.Headers;
                    string mediaType = headers.ContentType?.MediaType ?? "";
                    if (headers.ContentLength == 0 || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                        || mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                        || mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("لم يرسل الخادم ملفًا صوتيًا صالحًا.");

                    cancellationToken.ThrowIfCancellationRequested();
                    string candidate = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination)), $".quran-{Guid.NewGuid():N}.part");
                    using (var output = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        temporaryPath = candidate;
                        using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            await CopyAsync(input, output, headers.ContentLength, progress, cancellationToken).ConfigureAwait(false);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (string.Equals(Path.GetExtension(destination), ".mp3", StringComparison.OrdinalIgnoreCase))
                    ValidateMp3(temporaryPath);
                cancellationToken.ThrowIfCancellationRequested();
                if (replaceExisting && File.Exists(destination))
                    File.Replace(temporaryPath, destination, null);
                else
                {
                    try { File.Move(temporaryPath, destination); }
                    catch (IOException) when (!replaceExisting && File.Exists(destination)) { return false; }
                }
                temporaryPath = null;
                return true;
            }
            finally
            {
                if (temporaryPath != null)
                {
                    try { File.Delete(temporaryPath); }
                    catch (IOException ex) { Trace.TraceWarning(ex.Message); }
                    catch (UnauthorizedAccessException ex) { Trace.TraceWarning(ex.Message); }
                }
            }
        }

        private static void ValidateMp3(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                // Skip optional ID3 metadata without loading it into memory.
                byte[] header = reader.ReadBytes(10);
                if (header.Length == 10 && header[0] == 'I' && header[1] == 'D' && header[2] == '3')
                {
                    int tagSize = 0;
                    for (int i = 6; i < 10; i++)
                    {
                        if (header[i] >= 128) throw new InvalidDataException("بيانات MP3 غير صالحة.");
                        tagSize = (tagSize << 7) | header[i];
                    }
                    reader.BaseStream.Position = 10L + tagSize;
                }
                else reader.BaseStream.Position = 0;

                using (var prefix = new MemoryStream(reader.ReadBytes(8192)))
                    if (Mp3Frame.LoadFromStream(prefix) == null)
                        throw new InvalidDataException("لم يُعثر على بيانات صوتية في ملف MP3.");
            }
        }

        private static async Task CopyAsync(Stream input, Stream output, long? expectedLength,
            IProgress<int?> progress, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            long received = 0;
            int? lastPercent = expectedLength.HasValue ? 0 : (int?)null;
            progress?.Report(lastPercent);

            // On .NET Framework some response streams do not interrupt a pending read on token cancellation alone.
            using (var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (readCancellation.Token.Register(() =>
            {
                try { input.Dispose(); }
                catch (IOException) { }
            }))
            {
                while (true)
                {
                    int count;
                    readCancellation.CancelAfter(TimeSpan.FromSeconds(30));
                    try
                    {
                        count = await input.ReadAsync(buffer, 0, buffer.Length, readCancellation.Token).ConfigureAwait(false);
                    }
                    catch (Exception) when (readCancellation.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new TimeoutException("توقف الخادم عن إرسال البيانات.");
                    }
                    finally { readCancellation.CancelAfter(Timeout.Infinite); }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (readCancellation.IsCancellationRequested)
                        throw new TimeoutException("توقف الخادم عن إرسال البيانات.");
                    if (count == 0) break;
                    received += count;
                    if (received > expectedLength) throw new InvalidDataException("حجم الملف لا يطابق الحجم المتوقع.");
                    await output.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);

                    if (expectedLength.HasValue)
                    {
                        int percent = (int)(received * 100.0 / expectedLength.Value);
                        if (percent != lastPercent) progress?.Report(percent);
                        lastPercent = percent;
                    }
                }
            }

            if (received == 0 || (expectedLength.HasValue && received != expectedLength.Value))
                throw new InvalidDataException("ملف التلاوة فارغ أو لم يكتمل تحميله.");
        }
    }
}
