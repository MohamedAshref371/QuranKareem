using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace QuranKareem
{
    // One transparent video canvas per export. PNG compression favors speed without changing pixels.
    internal sealed class LineFrameWriter : IDisposable
    {
        private readonly Bitmap canvas;
        private readonly Graphics graphics;
        private readonly string folder;
        private readonly byte[] rgbaRow, bgraRow;
        private readonly byte[] emptyRows = new byte[64 * 1024];
        private readonly MemoryStream compressed = new MemoryStream();
        private readonly byte[] chunkNumber = new byte[4];

        private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] HeaderChunk = { 73, 72, 68, 82 };
        private static readonly byte[] ImageChunk = { 73, 68, 65, 84 };
        private static readonly byte[] EndChunk = { 73, 69, 78, 68 };
        private static readonly uint[] CrcTable = CreateCrcTable();
        private readonly byte[] header = new byte[13];

        public LineFrameWriter(string output, int width, int height)
        {
            folder = Path.Combine(output, "img");
            Directory.CreateDirectory(folder);
            canvas = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(canvas);
            rgbaRow = new byte[checked(1 + width * 4)]; // filter byte followed by straight RGBA
            bgraRow = new byte[width * 4];
            PutBigEndian(header, 0, (uint)width);
            PutBigEndian(header, 4, (uint)height);
            header[8] = 8;
            header[9] = 6; // RGBA
        }

        public string Write(int index, Bitmap line, Rectangle target, bool scale)
        {
            graphics.Clear(Color.Transparent);
            if (line != null)
            {
                if (scale) graphics.DrawImage(line, target);
                else graphics.DrawImage(line, target.X, target.Y);
            }

            string name = index + ".png";
            var bounds = line == null ? Rectangle.Empty : Rectangle.Intersect(new Rectangle(Point.Empty, canvas.Size), target);
            EncodeRows(bounds);
            using (var file = File.Create(Path.Combine(folder, name)))
            {
                file.Write(PngSignature, 0, PngSignature.Length);
                WriteChunk(file, HeaderChunk, header, header.Length);
                WriteChunk(file, ImageChunk, compressed.GetBuffer(), checked((int)compressed.Length));
                WriteChunk(file, EndChunk, Array.Empty<byte>(), 0);
            }
            return Path.Combine("img", name);
        }

        // The video frame is blank outside the line. Skip reading/converting those pixels
        // and write the transparent scanlines as reusable zero blocks.
        private void EncodeRows(Rectangle bounds)
        {
            compressed.SetLength(0);
            compressed.WriteByte(0x78); // zlib header: deflate, 32 KiB window
            compressed.WriteByte(0x01);
            const uint adlerMod = 65521;
            uint a = 1, b = 0;
            using (var deflate = new DeflateStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    long blank = (long)canvas.Height * rgbaRow.Length;
                    WriteZeros(deflate, blank);
                    b = (uint)(blank % adlerMod);
                }
                else
                {
                    long prefix = (long)bounds.Y * rgbaRow.Length;
                    WriteZeros(deflate, prefix);
                    b = (uint)(prefix % adlerMod);
                    Array.Clear(rgbaRow, 0, rgbaRow.Length);
                    int left = 1 + bounds.Left * 4, length = bounds.Width * 4;
                    int right = rgbaRow.Length - left - length;
                    var pixels = canvas.LockBits(bounds, ImageLockMode.ReadOnly, canvas.PixelFormat);
                    try
                    {
                        for (int y = 0; y < bounds.Height; y++)
                        {
                            Marshal.Copy(IntPtr.Add(pixels.Scan0, y * pixels.Stride), bgraRow, 0, length);
                            ulong sum = b + (ulong)left * a;
                            for (int i = 0; i < length; i += 4)
                            {
                                byte red = bgraRow[i + 2], green = bgraRow[i + 1], blue = bgraRow[i], alpha = bgraRow[i + 3];
                                rgbaRow[left + i] = red;
                                rgbaRow[left + i + 1] = green;
                                rgbaRow[left + i + 2] = blue;
                                rgbaRow[left + i + 3] = alpha;
                                sum += 4 * (ulong)a + 4 * (uint)red + 3 * (uint)green + 2 * (uint)blue + alpha;
                                a += (uint)red + green + blue + alpha;
                            }
                            b = (uint)((sum + (ulong)right * a) % adlerMod);
                            a %= adlerMod;
                            deflate.Write(rgbaRow, 0, rgbaRow.Length);
                        }
                    }
                    finally { canvas.UnlockBits(pixels); }
                    long suffix = (long)(canvas.Height - bounds.Bottom) * rgbaRow.Length;
                    WriteZeros(deflate, suffix);
                    b = (uint)((b + (ulong)(suffix % adlerMod) * a) % adlerMod);
                }
            }
            WriteNumber(compressed, b << 16 | a);
        }

        private void WriteZeros(Stream stream, long length)
        {
            while (length > 0)
            {
                int count = (int)Math.Min(length, emptyRows.Length);
                stream.Write(emptyRows, 0, count);
                length -= count;
            }
        }

        private void WriteChunk(Stream stream, byte[] type, byte[] data, int length)
        {
            WriteNumber(stream, (uint)length);
            stream.Write(type, 0, 4);
            stream.Write(data, 0, length);
            uint crc = uint.MaxValue;
            foreach (byte value in type) crc = CrcTable[(crc ^ value) & 255] ^ (crc >> 8);
            for (int i = 0; i < length; i++) crc = CrcTable[(crc ^ data[i]) & 255] ^ (crc >> 8);
            WriteNumber(stream, ~crc);
        }

        private void WriteNumber(Stream stream, uint value)
        {
            PutBigEndian(chunkNumber, 0, value);
            stream.Write(chunkNumber, 0, 4);
        }

        private static void PutBigEndian(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }

        private static uint[] CreateCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                uint crc = i;
                for (int bit = 0; bit < 8; bit++)
                    crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320);
                table[i] = crc;
            }
            return table;
        }

        public void Dispose()
        {
            compressed.Dispose();
            graphics.Dispose();
            canvas.Dispose();
        }
    }
}
