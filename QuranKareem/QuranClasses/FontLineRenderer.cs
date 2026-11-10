using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace QuranKareem
{
    internal sealed class FontLineRenderer : IDisposable
    {
        internal sealed class Layout
        {
            public string[] Glyphs;
            public float[] Positions, Advances;
            public int Width, Height;
        }

        private readonly Font font;
        private readonly Bitmap measureBitmap;
        private readonly Graphics measure;
        private readonly StringFormat format;
        private readonly SKTypeface typeface;
        private readonly SKPaint paint;
        private readonly bool dark;
        private readonly float underlineY, strokeWidth;
        private readonly Dictionary<string, float> widths = new Dictionary<string, float>();
        private readonly Dictionary<int, SolidBrush> brushes = new Dictionary<int, SolidBrush>();
        private byte[] darkPixels;
        private readonly float baseline, skiaHeight;

        public FontLineRenderer(Font font, string colorFontPath, bool dark, float underlineY, float strokeWidth)
        {
            this.font = font;
            this.dark = dark;
            this.underlineY = underlineY;
            this.strokeWidth = strokeWidth;
            if (colorFontPath != null) typeface = SKTypeface.FromFile(colorFontPath);
            if (typeface != null)
            {
                paint = new SKPaint
                {
                    IsAntialias = true, Typeface = typeface, TextSize = font.Size,
                    SubpixelText = true, LcdRenderText = true
                };
                var metrics = paint.FontMetrics;
                baseline = -metrics.Ascent;
                skiaHeight = metrics.Descent - metrics.Ascent;
            }
            else
            {
                measureBitmap = new Bitmap(1, 1);
                measure = Graphics.FromImage(measureBitmap);
                format = new StringFormat { Trimming = StringTrimming.Character };
            }
        }

        private float Measure(string text)
        {
            if (!widths.TryGetValue(text, out float width))
                widths.Add(text, width = paint != null ? paint.MeasureText(text) : (int)measure.MeasureString(text, font).Width);
            return width;
        }

        public Layout Prepare(string text)
        {
            var line = new Layout { Glyphs = new string[text.Length], Advances = new float[text.Length], Positions = new float[text.Length] };
            float total = 0;
            for (int i = 0; i < text.Length; i++)
            {
                line.Glyphs[i] = text[i].ToString();
                total += line.Advances[i] = Measure(line.Glyphs[i]);
            }
            if (paint != null)
            {
                line.Width = Math.Max(1, (int)Math.Ceiling(total));
                line.Height = Math.Max(1, (int)Math.Ceiling(skiaHeight));
            }
            else
            {
                SizeF bounds = measure.MeasureString(text, font);
                line.Width = Math.Max(1, (int)bounds.Width);
                line.Height = Math.Max(1, (int)bounds.Height);
            }
            float x = line.Width;
            for (int i = 0; i < text.Length; i++)
            {
                float advance = line.Advances[i];
                if (paint == null && i > 0)
                    advance += Measure(line.Glyphs[i - 1] + line.Glyphs[i]) - line.Advances[i - 1] - advance;
                line.Positions[i] = x -= advance;
            }
            return line;
        }

        public Bitmap Draw(Layout line, Color[] colors)
        {
            var bitmap = new Bitmap(line.Width, line.Height, PixelFormat.Format32bppArgb);
            try
            {
                if (paint != null) DrawSkia(bitmap, line, colors);
                else DrawGdi(bitmap, line, colors);
                return bitmap;
            }
            catch { bitmap.Dispose(); throw; }
        }

        private Color GlyphColor(Color[] colors, Layout line, int i) =>
            colors?.Length == line.Glyphs.Length ? colors[i] : (dark ? Color.White : Color.Black);

        private void DrawGdi(Bitmap bitmap, Layout line, Color[] colors)
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.Transparent);
                for (int i = 0; i < line.Glyphs.Length; i++)
                {
                    Color color = GlyphColor(colors, line, i);
                    if (!brushes.TryGetValue(color.ToArgb(), out SolidBrush brush))
                        brushes.Add(color.ToArgb(), brush = new SolidBrush(color));
                    graphics.DrawString(line.Glyphs[i], font, brush, new PointF(line.Positions[i], 0), format);
                }
            }
        }

        private void DrawSkia(Bitmap bitmap, Layout line, Color[] colors)
        {
            var info = new SKImageInfo(line.Width, line.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKSurface surface = SKSurface.Create(info))
            {
                surface.Canvas.Clear(SKColors.Transparent);
                for (int i = 0; i < line.Glyphs.Length; i++)
                {
                    Color color = GlyphColor(colors, line, i);
                    float x = line.Positions[i];
                    paint.Color = new SKColor(color.R, color.G, color.B, color.A);
                    surface.Canvas.DrawText(line.Glyphs[i], x, baseline, paint);
                    if (!(color.R == color.G && color.G == color.B && (color.R == 0 || color.R == 255)))
                    {
                        paint.StrokeWidth = strokeWidth;
                        surface.Canvas.DrawLine(x, baseline + underlineY, x + line.Advances[i], baseline + underlineY, paint);
                    }
                }

                // Read straight BGRA pixels into GDI+ instead of encoding and decoding an intermediate PNG.
                var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                try
                {
                    var target = new SKImageInfo(line.Width, line.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    if (!surface.ReadPixels(target, data.Scan0, data.Stride, 0, 0))
                        throw new InvalidOperationException("Could not read the rendered line.");
                    if (dark) InvertDarkPixels(data, line.Height);
                }
                finally { bitmap.UnlockBits(data); }
            }
        }

        private void InvertDarkPixels(BitmapData data, int height)
        {
            int size = data.Stride * height;
            if (darkPixels == null || darkPixels.Length < size) darkPixels = new byte[size];
            Marshal.Copy(data.Scan0, darkPixels, 0, size);
            for (int i = 0; i < size; i += 4)
                if (darkPixels[i + 3] > 0 && darkPixels[i] < 40 && darkPixels[i + 1] < 40 && darkPixels[i + 2] < 40)
                {
                    darkPixels[i] = (byte)(255 - darkPixels[i]);
                    darkPixels[i + 1] = (byte)(255 - darkPixels[i + 1]);
                    darkPixels[i + 2] = (byte)(255 - darkPixels[i + 2]);
                }
            Marshal.Copy(darkPixels, 0, data.Scan0, size);
        }

        public void Dispose()
        {
            foreach (SolidBrush brush in brushes.Values) brush.Dispose();
            format?.Dispose();
            measure?.Dispose();
            measureBitmap?.Dispose();
            paint?.Dispose();
            typeface?.Dispose();
        }
    }
}
