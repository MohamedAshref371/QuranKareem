using System;
using System.Data.SQLite;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using static QuranKareem.Coloring;

namespace QuranKareem
{
    internal class PictureQuran : IVisualQuran
    {
        #region Create The Class
        private bool success = false;
        private string path;

        private readonly SQLiteConnection quran;
        private readonly SQLiteCommand command;
        private SQLiteDataReader reader;

        public static readonly PictureQuran Instance = new PictureQuran();

        private PictureQuran()
        {
            quran = new SQLiteConnection();
            command = new SQLiteCommand(quran);
        }
        #endregion

        #region Ayat Table
        private int ayahId;

        #region Surahs Table
        public int SurahNumber { get; private set; }
        public bool Makya_Madanya { get; private set; }
        public int AyatCount { get; private set; }
        #endregion

        public int QuarterNumber { get; private set; }
        public int PageNumber { get; private set; }
        public int AyahNumber { get; private set; }
        #endregion

        #region Description Table
        private int version;
        public int Narration { get; private set; }
        public int SurahsCount { get; private set; }
        //public int QuartersCount { get; private set; }
        public int PagesCount { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private Color textColor = Color.Empty, background;
        public string Extension { get; private set; } // example: .png
        public string Comment { get; private set; }
        #endregion

        #region Bitmaps
        private SharpPixelQK spQuranPicture;
        public Bitmap CurrentPicture { get; private set; }
        public Bitmap PagePicture { get; private set; }
        // The view borrows CurrentPicture and must switch to the replacement before the old image is disposed.
        public event EventHandler PictureReplaced;
        #endregion

        #region Words
        public int CurrentWord { get; private set; } = -1;
        private int wordsCount = 0;
        public bool WordMode { get; set; } = true;
        private bool isWordTableEmpty = true;
        private bool isWordsDiscriminatorEmpty = false;
        #endregion


        private bool darkMode = false;
        public bool DarkMode
        {
            get => darkMode;
            set
            {
                if (success && value != darkMode)
                {
                    darkMode = value;
                    PageNumber = 0;
                    if (background.A != 0) background = Color.FromArgb(background.A, 255 - background.R, 255 - background.G, 255 - background.B);
                    if (!textColor.IsEmpty) textColor = Color.FromArgb(255 - background.R, 255 - background.G, 255 - background.B);
                    isWordsDiscriminatorEmpty = !Discriminators.ActiveDiscriminators(darkMode);
                    Set();
                }
                else
                {
                    darkMode = value;
                }
            }
        }

        // الدالة البداية
        public bool Start(string path, int sura = 1, int aya = 0)
        {

            if (path == null || path.Trim().Length == 0) return false;
            if (path.Substring(path.Length - 1) != "\\") path += "\\";

            if (!File.Exists(path + "000.db") && !File.Exists(path + "0.db")) return false;
            this.path = path; success = false;

            try
            {
                if (File.Exists(path + "000.db"))
                    quran.ConnectionString = $"Data Source={path}000.db;Version=3;";
                else
                    quran.ConnectionString = $"Data Source={path}0.db;Version=3;";

                quran.Open();
                command.CommandText = $"SELECT * FROM description";
                reader = command.ExecuteReader();

                if (!reader.HasRows) return false;
                reader.Read();
                version = reader.GetInt32(1);
                if (reader.GetInt32(0)/*type 1:text, 2:picture, 3: audios*/ != 2 || version != 4) return false;
                Narration = reader.GetInt32(2);
                SurahsCount = reader.GetInt32(3);
                //QuartersCount = reader.GetInt32(4);
                PagesCount = reader.GetInt32(5);
                Width = reader.GetInt32(6);
                Height = reader.GetInt32(7);

                string[] clr;
                clr = reader.GetString(8).Split(',');
                if (clr.Length == 3) textColor = Color.FromArgb(Convert.ToInt32(clr[0]), Convert.ToInt32(clr[1]), Convert.ToInt32(clr[2]));
                else textColor = Color.Empty;
                clr = reader.GetString(9).Split(',');
                if (clr.Length >= 3) background = Color.FromArgb(clr.Length > 3 ? Convert.ToInt32(clr[3]) : 255, Convert.ToInt32(clr[0]), Convert.ToInt32(clr[1]), Convert.ToInt32(clr[2]));

                Extension = reader.GetString(10);
                Comment = reader.GetString(11);
                reader.Close();

                PageNumber = 0;

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
            if (success)
            {
                DiscriminatorsReader();
                Discriminators.GetDiscriminators(path + "Colors.txt");
                isWordsDiscriminatorEmpty = !Discriminators.ActiveDiscriminators(darkMode);
                SurahNumber = 0;
                GetInitialColors();
                if (darkMode)
                {
                    if (background.A != 0) background = Color.FromArgb(background.A, 255 - background.R, 255 - background.G, 255 - background.B);
                    if (!textColor.IsEmpty) textColor = Color.FromArgb(255 - background.R, 255 - background.G, 255 - background.B);
                }
                success = Set(sura, aya);
            }
            return success;
        }

        #region التنقلات في المصحف
        public string[] GetSurahNames()
        {
            if (!success) return new string[] { "" };
            string[] names = new string[SurahsCount];
            quran.Open();
            command.CommandText = $"SELECT name FROM surahs";
            reader = command.ExecuteReader();

            int i = 0;
            while (reader.Read())
            {
                names[i] = reader.GetString(0); // عمود واحد
                i++;
            }
            reader.Close(); quran.Close();
            return names;
        }

        #region Set Ayah Method
        /// <summary>
        /// Implementation Priority:<br/>
        ///   1- Surah (1:114), its Ayah (0:{AyatCount}).<br/>
        ///   1- Ayah (0:{AyatCount}) only.<br/>
        ///   2- Next parameter (false,true).<br/>
        ///   3- Juz (1:30), Hizb (1:2), Quarter (1:4).<br/>
        ///   3- Juz (1:30), Quarter (1:8).<br/>
        ///   3- Juz (1:30) only.<br/>
        ///   4- Hizb (1:60), Quarter (1:4).<br/>
        ///   4- Hizb (1:60) only.<br/>
        ///   5- Quarter (1:240) only.<br/>
        ///   6- Page (1:{PagesCount}).<br/>
        /// </summary>
        /// <param name="surah">Quran Surahs Count is 114.</param>
        /// <param name="ayah">Each Surah contains Ayat.</param>
        /// <param name="next">Go to the next Ayah.</param>
        /// <param name="juz">Quran Juz Count is 30 and one Juz contains 8 Quarters.</param>
        /// <param name="hizb">Quran Hizb Count is 60 and one Hizb contains 4 Quarters.</param>
        /// <param name="quarter">Quran Quarters Count is 240.</param>
        /// <param name="page">Usually the 'Quran Pages Count' is 604.</param>
        /// <returns>true if everything goes well.</returns>
        public bool Set(int surah = 0, int ayah = -2, bool next = false, int juz = 0, int hizb = 0, int quarter = 0, int page = 0)
        {
            if (!success) return false;

            quran.Open();
            try
            {
                VisualAyah? match = VisualQuranNavigation.Find(quran, this, SurahsCount, PagesCount, ayahId,
                    surah, ayah, next, juz, hizb, quarter, page);
                if (!match.HasValue) return false;

                VisualAyah verse = match.Value;
                if (verse.Page != PageNumber || spQuranPicture == null) PictureAt(verse.Page);
                if (verse.Surah != SurahNumber) SurahData(verse.Surah);
                ayahId = verse.Id;
                QuarterNumber = verse.Quarter;
                AyahNumber = verse.Ayah;
                AyahData();
                return true;
            }
            finally { quran.Close(); }
        }

        private void SurahData(int surah)
        {
            command.CommandText = $"SELECT makya_madanya,ayat_count FROM surahs WHERE id={surah}";
            using (SQLiteDataReader rows = command.ExecuteReader())
            {
                rows.Read();
                Makya_Madanya = rows.GetBoolean(0);
                AyatCount = rows.GetInt32(1);
                SurahNumber = surah;
            }
        }

        private void AyahData()
        {
            CurrentWord = -1;

            AyahDecorations(Discriminators.AyahColors.Count > 0 || !isWordTableEmpty);

            if (WordMode && !isWordTableEmpty)
            {
                command.CommandText = $"SELECT word FROM words WHERE ayah_id={ayahId} AND word<=599 ORDER BY word DESC LIMIT 1";
                using (SQLiteDataReader rows = command.ExecuteReader())
                    wordsCount = rows.Read() ? rows.GetInt32(0) : 0;
            }
        }

        private void PictureAt(int page)
        {
            Bitmap next = CatchPicture(page);
            Bitmap clean;
            SharpPixelQK pixels = spQuranPicture;
            try
            {
                if (pixels == null || !pixels.TryUseBitmap(next)) pixels = new SharpPixelQK(next, true);

                if (Discriminators.PageColors.Count > 0 && !isWordTableEmpty)
                    PageDecorations(pixels, page);
                else if (darkMode && isWordTableEmpty)
                {
                    pixels.Lock();
                    try { pixels.ReverseColors(); }
                    finally { pixels.Unlock(true, true); }
                }

                clean = (Bitmap)next.Clone();
            }
            catch
            {
                // Rebinding resets the pixel workspace; reload a page before applying any more highlights.
                if (ReferenceEquals(spQuranPicture, pixels)) spQuranPicture = null;
                next.Dispose();
                throw;
            }

            PageNumber = page;
            spQuranPicture = pixels;
            ReplacePictures(next, clean);
        }

        private void ReplacePictures(Bitmap current, Bitmap clean)
        {
            Bitmap previous = CurrentPicture, previousClean = PagePicture;
            CurrentPicture = current;
            PagePicture = clean;
            if (previous == null && previousClean == null && current == null) return;

            try { PictureReplaced?.Invoke(this, EventArgs.Empty); }
            finally
            {
                previous?.Dispose();
                if (!ReferenceEquals(previousClean, previous)) previousClean?.Dispose();
            }
        }

        public void ReleasePictures()
        {
            success = false;
            PageNumber = 0;
            spQuranPicture = null;
            ReplacePictures(null, null);
        }

        private Bitmap CatchPicture(int page)
        {
            string s = page + "";
            if (s.Length == 1) s = "00" + s;
            else if (s.Length == 2) s = "0" + s;
            s += Extension;

            if (File.Exists(path + s)) return new Bitmap(path + s);
            else if (File.Exists($"{path}{page}{Extension}")) return new Bitmap($"{path}{page}{Extension}");
            else
            {
                string number = page.ToString("D3");
                foreach (string file in Directory.EnumerateFiles(path, "*" + Extension))
                    if (Path.GetFileName(file).Contains(number)) return new Bitmap(file);
            }
            return new Bitmap(Width, Height); // Never show the previous verse as if it were the requested page.
        }
        #endregion

        public bool WordOf(int word)// سيتم تعديله ان شاء الله
        {
            if (!success || spQuranPicture == null || !WordMode || isWordTableEmpty || isWordsDiscriminatorEmpty || word <= 0 || word > wordsCount)
            {
                if (CurrentWord > 0 && spQuranPicture != null)
                {
                    spQuranPicture.Lock();
                    try { spQuranPicture.SetOriginal(true); }
                    finally { spQuranPicture.Unlock(true); }
                }
                CurrentWord = -1;
                return false;
            }
            CurrentWord = word;
            WordDecorations();
            return true;
        }

        public bool SetXY(int xMouse, int yMouse) => SetXY(xMouse, yMouse, Width, Height);
        public bool SetXY(int xMouse, int yMouse, int width, int height)
        { // مؤشر الماوس
            if (!success) return false;
            xMouse = (int)(xMouse * (Width / (decimal)width)); // تصحيح المؤشر إذا كان عارض الصورة ليس بنفس عرض الصورة نفسها
            yMouse = (int)(yMouse * (Height / (decimal)height)) + 1;
            int word = -1; int tempInt = -371, tempInt2 = 0;
            quran.Open();
            bool words = WordMode && !isWordTableEmpty;
            if (words)
            {
                command.CommandText = $"SELECT surah,ayah,word FROM ayat JOIN words ON ayat.id = words.ayah_id WHERE page={PageNumber} AND min_x<={xMouse} AND max_x>={xMouse} AND min_y<={yMouse} AND max_y>={yMouse}";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    tempInt = reader.GetInt32(0);
                    tempInt2 = reader.GetInt32(1);
                    word = !reader.IsDBNull(2) ? reader.GetInt32(2) : -1;
                }
                else
                {
                    words = false;
                    reader.Close();
                }
            }
            if (!words)
            {
                command.CommandText = $"SELECT surah,ayah FROM ayat JOIN lines ON ayat.id = lines.ayah_id WHERE page={PageNumber} AND min_x<={xMouse} AND max_x>={xMouse} AND min_y<={yMouse} AND max_y>={yMouse}";
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    tempInt = reader.GetInt32(0);
                    tempInt2 = reader.GetInt32(1);
                }
            }

            reader.Close(); quran.Close();
            if (tempInt != -371) Set(tempInt, tempInt2); // استدعاء الملك
            if (words && tempInt != -371) WordOf(word);
            return tempInt != -371;
        }
        #endregion

        #region Discriminators Methods
        private void PageDecorations(SharpPixelQK pixels, int page)
        {
            pixels.Lock();
            try
            {
                command.CommandText = $"SELECT min_x,max_x,min_y,max_y,discriminator FROM words JOIN ayat ON words.ayah_id = ayat.id WHERE discriminator IN ({Discriminators.GetPageKeysAsString()}) AND page={page}";
                using (SQLiteDataReader rows = command.ExecuteReader())
                    while (rows.Read())
                        pixels.Clear(Discriminators.PageColors[rows.GetInt32(4)], rows.GetInt32(0), rows.GetInt32(2), rows.GetInt32(1), rows.GetInt32(3), textColor, 30, false, true);
            }
            finally { pixels.Unlock(true, true); }
        }

        private void AyahDecorations(bool discri = true)
        {
            spQuranPicture.Lock();
            try
            {
                spQuranPicture.SetOriginal();
                command.CommandText = discri
                    ? $"SELECT min_x,max_x,min_y,max_y,discriminator FROM words WHERE discriminator IN ({Discriminators.GetAyahKeysAsString()}) AND ayah_id={ayahId}"
                    : $"SELECT min_x,max_x,min_y,max_y FROM lines WHERE ayah_id={ayahId}";
                using (SQLiteDataReader rows = command.ExecuteReader())
                    while (rows.Read())
                    {
                        Color clr = discri ? Discriminators.AyahColors[rows.GetInt32(4)] : GetColor(1, darkMode);
                        if (clr.Name == "AyahColor") clr = GetColor(1, darkMode);
                        if (!clr.IsEmpty)
                            spQuranPicture.Clear(clr, rows.GetInt32(0), rows.GetInt32(2), rows.GetInt32(1), rows.GetInt32(3), textColor, 30, false, true);
                    }
            }
            finally { spQuranPicture.Unlock(true); }
        }

        private void WordDecorations()
        {
            spQuranPicture.Lock();
            spQuranPicture.SetOriginal(true);

            quran.Open();
            command.CommandText = $"SELECT min_x,max_x,min_y,max_y,discriminator FROM words WHERE discriminator IN ({Discriminators.GetWordKeysAsString()}) AND ayah_id={ayahId} AND word={CurrentWord}";
            reader = command.ExecuteReader();

            Color clr;
            while (reader.Read())
            {
                clr = Discriminators.WordColors[reader.GetInt32(4)];
                if (clr.Name == "WordColor") clr = GetColor(2, darkMode);
                if (!clr.IsEmpty)
                    spQuranPicture.Clear(clr, reader.GetInt32(0), reader.GetInt32(2), reader.GetInt32(1), reader.GetInt32(3), textColor, 30, false, true, true);
            }

            reader.Close(); quran.Close();
            spQuranPicture.Unlock(true);
        }
        #endregion

        #region lines images
        private sealed class ExportLine
        {
            public readonly List<Rectangle> Segments = new List<Rectangle>();
            public readonly Dictionary<(int Surah, int Ayah, int Word), List<(Rectangle Area, Color Color)>> Words =
                new Dictionary<(int, int, int), List<(Rectangle, Color)>>();
            public Rectangle Bounds;

            public Bitmap Crop(Bitmap pagePicture)
            {
                var crop = new Bitmap(Bounds.Width + 1, Bounds.Height + 1);
                using (Graphics graphics = Graphics.FromImage(crop))
                {
                    graphics.Clear(Color.Transparent);
                    foreach (Rectangle segment in Segments)
                        graphics.DrawImage(pagePicture, new Rectangle(segment.X - Bounds.X, segment.Y - Bounds.Y, segment.Width, segment.Height),
                            segment, GraphicsUnit.Pixel);
                }
                return crop;
            }

            public Bitmap Highlight(Bitmap crop, List<(Rectangle Area, Color Color)> marks, Color text)
            {
                var highlighted = (Bitmap)crop.Clone();
                var pixels = new SharpPixelQK(highlighted);
                try
                {
                    pixels.Lock();
                    try
                    {
                        foreach (var mark in marks)
                        {
                            Rectangle area = mark.Area;
                            int left = Math.Max(0, area.Left - Bounds.Left), top = Math.Max(0, area.Top - Bounds.Top);
                            int right = Math.Min(crop.Width - 1, area.Right - Bounds.Left), bottom = Math.Min(crop.Height - 1, area.Bottom - Bounds.Top);
                            if (left <= right && top <= bottom)
                                pixels.Clear(mark.Color, left, top, right, bottom, text, 30, false, true);
                        }
                    }
                    finally { pixels.Unlock(true); }
                    return highlighted;
                }
                catch { highlighted.Dispose(); throw; }
            }
        }

        private Dictionary<int, ExportLine> ReadExportLines(int page, bool includeWords,
            Dictionary<(int Surah, int Ayah, int Word), int> wordLines = null)
        {
            var lines = new Dictionary<int, ExportLine>();
            Color wordColor = GetColor(2, darkMode);
            bool useMarks = includeWords && !wordColor.IsEmpty && !isWordsDiscriminatorEmpty;
            using (var connection = new SQLiteConnection(quran.ConnectionString))
            using (var query = connection.CreateCommand())
            {
                connection.Open();
                query.Parameters.AddWithValue("@page", page);
                query.CommandText = @"SELECT line,min_x,max_x,min_y,max_y FROM lines
                    JOIN ayat ON lines.ayah_id=ayat.id WHERE page=@page ORDER BY ayat.id,lines.rowid";
                using (var rows = query.ExecuteReader())
                    while (rows.Read())
                    {
                        int number = rows.GetInt32(0);
                        var segment = Rectangle.FromLTRB(rows.GetInt32(1), rows.GetInt32(3), rows.GetInt32(2), rows.GetInt32(4));
                        if (!lines.TryGetValue(number, out ExportLine line))
                            lines.Add(number, line = new ExportLine { Bounds = segment });
                        else line.Bounds = Rectangle.Union(line.Bounds, segment);
                        line.Segments.Add(segment);
                    }

                if (!isWordTableEmpty && (useMarks || wordLines != null))
                {
                    query.CommandText = @"SELECT line,surah,ayah,word,discriminator,min_x,max_x,min_y,max_y
                        FROM words JOIN ayat ON words.ayah_id=ayat.id
                        WHERE page=@page AND word BETWEEN 1 AND 599 ORDER BY ayat.id,word,words.rowid";
                    using (var rows = query.ExecuteReader())
                        while (rows.Read())
                        {
                            int number = rows.GetInt32(0);
                            var key = (rows.GetInt32(1), rows.GetInt32(2), rows.GetInt32(3));
                            if (!lines.TryGetValue(number, out ExportLine line)) continue;
                            if (wordLines != null && !wordLines.ContainsKey(key)) wordLines.Add(key, number);
                            if (!useMarks) continue;
                            if (!line.Words.TryGetValue(key, out var marks))
                                line.Words.Add(key, marks = new List<(Rectangle, Color)>());
                            if (!Discriminators.WordColors.TryGetValue(rows.GetInt32(4), out Color color) || color.IsEmpty || color.Name == "WordColor")
                                color = wordColor;
                            var area = Rectangle.FromLTRB(rows.GetInt32(5), rows.GetInt32(7), rows.GetInt32(6), rows.GetInt32(8));
                            marks.Add((area, color));
                        }
                }
            }
            return lines;
        }

        public List<Bitmap> GetLines()
        {
            var result = new List<Bitmap>(15);
            var lines = success ? ReadExportLines(PageNumber, includeWords: false) : new Dictionary<int, ExportLine>();
            for (int i = 1; i <= 15; i++)
                result.Add(lines.TryGetValue(i, out ExportLine line) ? line.Crop(PagePicture) : null);
            return result;
        }

        public List<List<Bitmap>> GetLinesWithWordsMarks()
        {
            var result = new List<List<Bitmap>>(15);
            var lines = success ? ReadExportLines(PageNumber, includeWords: true) : new Dictionary<int, ExportLine>();
            for (int i = 1; i <= 15; i++)
            {
                if (!lines.TryGetValue(i, out ExportLine line)) { result.Add(null); continue; }
                Bitmap crop = line.Crop(PagePicture);
                var variants = new List<Bitmap> { crop };
                foreach (var word in line.Words.OrderBy(entry => entry.Key.Surah).ThenBy(entry => entry.Key.Ayah).ThenBy(entry => entry.Key.Word))
                {
                    Bitmap mark = line.Highlight(crop, word.Value, textColor);
                    mark.Tag = $"{word.Key.Ayah},{word.Key.Word}";
                    variants.Add(mark);
                }
                result.Add(variants);
            }
            return result;
        }

        public int[] GetStartAndEndOfPage()
        {
            command.CommandText = $"SELECT MIN(ayah), MAX(ayah) FROM ayat WHERE surah={SurahNumber} AND page = {PageNumber} AND ayah >= 1";
            int[] ints = new int[2];
            quran.Open();
            reader = command.ExecuteReader();
            if (reader.Read() && !reader.IsDBNull(0))
            {
                ints[0] = reader.GetInt32(0);
                ints[1] = reader.GetInt32(1);
            }
            reader.Close();
            quran.Close();
            return ints;
        }

        public void GetAyatInLinesWithWordsMarks(List<int> ayahword, int width, int height, int locx, int locy, int linWdth, int linHght, bool autoHeight, bool yEdit, string path, List<string> paths, Bitmap pageClone, int surah, int page)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            paths.Clear();
            var export = PrepareLineExport(ayahword, width, height, locx, locy, linWdth, linHght, autoHeight, yEdit, path, pageClone, surah, page);
            if (export != null) paths.AddRange(export());
        }

        internal Func<List<string>> PrepareLineExport(List<int> ayahword, int width, int height, int locx, int locy, int linWdth, int linHght,
            bool autoHeight, bool yEdit, string path, Bitmap pageClone, int surah, int page)
        {
            if (!success || isWordTableEmpty || ayahword == null || ayahword.Count < 2) return null;
            var wordLines = new Dictionary<(int, int, int), int>();
            var lines = ReadExportLines(page, includeWords: true, wordLines: wordLines);
            if (lines.Count == 0) return null;
            int[] intervals = ayahword.ToArray();
            Color text = textColor;
            return () => RenderLineExport(intervals, lines, wordLines, text, width, height, locx, locy, linWdth, linHght, autoHeight, yEdit, path, pageClone, surah);
        }

        private static List<string> RenderLineExport(int[] intervals, Dictionary<int, ExportLine> lines, Dictionary<(int, int, int), int> wordLines,
            Color text, int width, int height, int locx, int locy, int linWdth, int linHght, bool autoHeight, bool yEdit, string path, Bitmap pageClone, int surah)
        {
            var paths = new List<string>(intervals.Length / 2);
            // A pause has no word coordinates; keep the previously displayed line.
            int currentLine = lines.Keys.Min();
            var crops = new Dictionary<int, Bitmap>();
            using (var output = new LineFrameWriter(path, width, height))
            {
                try
                {
                    for (int i = 0; i < intervals.Length / 2; i++)
                    {
                        var key = (surah, intervals[i * 2], intervals[i * 2 + 1]);
                        if (wordLines.TryGetValue(key, out int number)) currentLine = number;
                        ExportLine line = lines[currentLine];
                        if (!crops.TryGetValue(currentLine, out Bitmap crop))
                            crops.Add(currentLine, crop = line.Crop(pageClone));
                        if (autoHeight)
                        {
                            int heightForLine = (int)(1f * crop.Height / crop.Width * linWdth);
                            if (yEdit) locy -= (heightForLine - linHght) / 2;
                            linHght = heightForLine;
                        }
                        var target = new Rectangle(locx, locy, linWdth, linHght);
                        if (line.Words.TryGetValue(key, out var marks))
                        {
                            using (Bitmap highlighted = line.Highlight(crop, marks, text))
                                paths.Add(output.Write(i, highlighted, target, scale: true));
                        }
                        else paths.Add(output.Write(i, crop, target, scale: true));
                    }
                }
                finally
                {
                    foreach (Bitmap crop in crops.Values) crop.Dispose();
                }
            }
            return paths;
        }
        #endregion

        #region Colors
        private void GetInitialColors()
        {
            Coloring.GetInitialColors(path + "Colors0.txt");
        }

        public void SetInitialColors()
        {
            if (!success) return;
            Coloring.SetInitialColors(path + "Colors0.txt");
        }

        public void SetDiscriminators()
        {
            if (!success) return;
            Discriminators.SetDiscriminators(path + "Colors.txt");
            isWordsDiscriminatorEmpty = !Discriminators.ActiveDiscriminators(darkMode);
        }

        private void DiscriminatorsReader()
        {
            Discriminators.Descriptions.Clear();
            quran.Open();
            command.CommandText = $"SELECT id,comment FROM discriminators WHERE enabled=1";
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                Discriminators.Descriptions.Add(reader.GetInt32(0), reader.GetString(1));
            }
            reader.Close(); quran.Close();
        }
        #endregion

    }
}
