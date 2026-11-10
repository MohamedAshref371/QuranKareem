using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuranKareem.NavigationTests
{
    internal static class RecitationDownloadChecks
    {
        private static readonly Uri Source = new Uri("https://example.invalid/001.mp3");

        public static async Task RunAsync()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DownloadChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path = Path.Combine(folder, "001.mp3");
                byte[] audio = MakeMp3Frame();
                byte[] replacement = Encoding.ASCII.GetBytes("ID3\x04\0\0\0\0\0\x03tag").Concat(audio).ToArray();

                Require(await DownloadAsync(() => AudioResponse(audio), path, false), "download creates a new recording");
                CheckFile(path, audio);
                Require(!await DownloadAsync(() => throw new InvalidOperationException("existing file should skip HTTP"), path, false), "skip existing recording");

                await ExpectAsync<HttpRequestException>(() => DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), path, true));
                CheckFile(path, audio);
                await ExpectAsync<InvalidDataException>(() => DownloadAsync(() => AudioResponse(replacement, replacement.Length + 100), path, true));
                CheckFile(path, audio);
                await ExpectAsync<InvalidDataException>(() => DownloadAsync(() => AudioResponse(Encoding.UTF8.GetBytes("<html>upstream error</html>")), path, true));
                CheckFile(path, audio);

                using (var stalled = new StalledStream(replacement))
                using (var cancel = new CancellationTokenSource())
                {
                    Task<bool> transfer = DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stalled) }, path, true, cancel.Token);
                    Require(await Task.WhenAny(stalled.ReadPending, Task.Delay(5000)) == stalled.ReadPending, "transfer reached a pending read after writing bytes");
                    Require(Directory.EnumerateFiles(folder, "*.part").Count() == 1, "incomplete transfer uses a temporary file");
                    CheckContents(path, audio);
                    cancel.Cancel();
                    Require(await Task.WhenAny(transfer, Task.Delay(5000)) == transfer, "cancel interrupts a stalled stream");
                    await ExpectAsync<OperationCanceledException>(() => transfer);
                    CheckFile(path, audio);
                }

                Require(await DownloadAsync(() => AudioResponse(replacement), path, true), "complete transfer replaces an existing recording");
                CheckFile(path, replacement);
                Console.WriteLine("Recitation download checks passed (no live network).");
            }
            finally { Directory.Delete(folder, recursive: true); }
        }

        public static void RunQueue()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QueueChecks-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string[] names = { "001.mp3", "002.mp3", "003.mp3", "004.mp3", "005.mp3" };
                // One duplicate and one unsupported link should not produce extra rows.
                File.WriteAllLines(Path.Combine(folder, "download links.txt"), names.Select(name => "https://example.invalid/" + name)
                    .Concat(new[] { "https://example.invalid/001.mp3?duplicate=1", "file:///006.mp3" }));
                var requested = new List<string>();
                var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                byte[] audio = MakeMp3Frame();
                using (var stalled = new StalledStream(audio))
                using (var client = new HttpClient(new TestHandler(request =>
                {
                    string name = Path.GetFileName(request.RequestUri.AbsolutePath);
                    requested.Add(name);
                    switch (name)
                    {
                        case "001.mp3": return first.Task;
                        case "002.mp3": return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                        case "004.mp3": return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stalled) });
                        default: return Task.FromResult(AudioResponse(audio));
                    }
                })))
                using (var form = new DownloadAudioQuranFiles(new RecitationDownloader(client)))
                {
                    Exception error = null;
                    EventHandler check = null;
                    check = async (sender, args) =>
                    {
                        Application.Idle -= check;
                        try
                        {
                            form.InitializeAudioFile(folder);
                            var rows = Descendants(form).OfType<FileViewControl>().Where(row => row.FilePath != null)
                                .ToDictionary(row => row.FileName);
                            Require(rows.Count == names.Length, "one row per valid recording destination");
                            foreach (string name in names.Take(3)) rows[name].Clicked(null, EventArgs.Empty);
                            Require(form.IsBusy && requested.SequenceEqual(names.Take(1)), "only one request starts while others wait");
                            first.SetResult(AudioResponse(audio));
                            for (int i = 0; form.IsBusy && i < 250; i++) await Task.Delay(20);
                            Require(!form.IsBusy && requested.SequenceEqual(names.Take(3)), "queue drains in order despite a failed request");
                            CheckFile(Path.Combine(folder, names[0]), audio);
                            Require(!File.Exists(Path.Combine(folder, names[1])), "failed recording has no final file");
                            CheckFile(Path.Combine(folder, names[2]), audio);

                            foreach (string name in names.Skip(3)) rows[name].Clicked(null, EventArgs.Empty);
                            Require(await Task.WhenAny(stalled.ReadPending, Task.Delay(5000)) == stalled.ReadPending, "active queue row is receiving data");
                            await form.StopDownloadsAsync();
                            Require(!form.IsBusy && requested.SequenceEqual(names.Take(4)), "stopping the queue cancels active work and drops waiting work");
                            Require(!names.Skip(3).Any(name => File.Exists(Path.Combine(folder, name)))
                                && !Directory.EnumerateFiles(folder, "*.part").Any(), "stopping leaves no incomplete files");
                        }
                        catch (Exception ex) { error = ex; }
                        finally
                        {
                            first.TrySetCanceled();
                            Application.ExitThread();
                        }
                    };
                    Application.Idle += check;
                    Application.Run();
                    if (error != null) throw new InvalidOperationException("Recitation queue check failed.", error);
                }
                Console.WriteLine("WinForms recitation queue checks passed (no live network).");
            }
            finally { Directory.Delete(folder, recursive: true); }
        }

        private static IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;
                foreach (Control descendant in Descendants(child)) yield return descendant;
            }
        }

        private static async Task<bool> DownloadAsync(Func<HttpResponseMessage> response, string path, bool replace, CancellationToken token = default(CancellationToken))
        {
            using (var client = new HttpClient(new TestHandler(_ => Task.FromResult(response()))))
                return await new RecitationDownloader(client).DownloadAsync(Source, path, replace, null, token);
        }

        private static HttpResponseMessage AudioResponse(byte[] bytes, int? expectedLength = null)
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentLength = expectedLength ?? bytes.Length;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }

        private static byte[] MakeMp3Frame()
        {
            // MPEG-1 Layer III, 128 kbps at 44.1 kHz, with a full frame payload.
            var frame = new byte[417];
            frame[0] = 0xFF;
            frame[1] = 0xFB;
            frame[2] = 0x90;
            frame[3] = 0x64;
            return frame;
        }

        private static void CheckContents(string path, byte[] expected) =>
            Require(File.ReadAllBytes(path).SequenceEqual(expected), "recording bytes match the expected version");

        private static void CheckFile(string path, byte[] expected)
        {
            CheckContents(path, expected);
            Require(!Directory.EnumerateFiles(Path.GetDirectoryName(path), "*.part").Any(), "no temporary files remain after transfer");
        }

        private static async Task ExpectAsync<T>(Func<Task> operation) where T : Exception
        {
            try { await operation(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> response;
            public TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) => this.response = response;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response(request);
        }

        private sealed class StalledStream : MemoryStream
        {
            private readonly TaskCompletionSource<bool> started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<int> read = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            public StalledStream(byte[] bytes) : base(bytes) { }
            public Task ReadPending => started.Task;
            public override bool CanSeek => false;

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                if (Position == 0) return base.ReadAsync(buffer, offset, Math.Min(count, 16), CancellationToken.None);
                started.TrySetResult(true);
                return read.Task; // Emulate a response stream that only unblocks when disposed.
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) read.TrySetException(new ObjectDisposedException(nameof(StalledStream)));
                base.Dispose(disposing);
            }
        }
    }
}
