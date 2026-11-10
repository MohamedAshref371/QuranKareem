using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static QuranKareem.Coloring;

namespace QuranKareem
{
    internal class TrueTypeFontQuran : IVisualQuran
    {
        private bool success = false;
        private string path;

        private readonly SQLiteConnection quran;
        private readonly SQLiteCommand command;
        private SQLiteDataReader reader;

        #region Description
        public int Narration { get; private set; }
        public int SurahsCount { get; private set; }
        public int PagesCount { get; private set; }
        public string Extension { get; private set; }
        public string Comment { get; private set; }
        #endregion

        public int SurahNumber { get; private set; }
        public bool Makya_Madanya { get; private set; }
        public int AyatCount { get; private set; }
        public int QuarterNumber { get; private set; }
        public int PageNumber { get; private set; }
        public int AyahNumber { get; private set; }
        public int CurrentWord { get; private set; } = -1;
        public bool WordMode { get; set; } = true;

        private int wordsCount;
        private int ayahId;

        private bool isWordsDiscriminatorEmpty = false;

        private PrivateFontCollection bsml, fPage;

        private int prevAyah, prevWord;

        private float[] pagesSizeUnits;

        public bool UseColoringFont { get; set; } = false;
        public float UnderlineY { get; set; } = 70f;
        public float StrokeWidth { get; set; } = 4f;

        public static readonly TrueTypeFontQuran Instance = new TrueTypeFontQuran();

        public readonly RichTextBox PageRichText = new RichTextBox()
        {
            RightToLeft = RightToLeft.Yes,
            WordWrap = false,
            //Font = new Font("Tahoma", 20F),
            BackColor = SystemColors.Control,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Cursor = Cursors.Hand,
            TabStop = false,
            ForeColor = Color.Black,
            ScrollBars = RichTextBoxScrollBars.None
        };

        private bool darkMode = false;
        public bool DarkMode
        {
            get => darkMode;
            set
            {
                if (success && value != darkMode)
                {
                    darkMode = value;
                    Color clr;
                    if (value)
                    {
                        clr = Night.BackColor;
                        PageRichText.ForeColor = Color.White;
                        if (clr.IsEmpty || clr == Color.Transparent)
                            PageRichText.BackColor = Color.Black;
                        else
                            PageRichText.BackColor = clr;
                    }
                    else
                    {
                        clr = Light.BackColor;
                        PageRichText.ForeColor = Color.Black;
                        if (clr.IsEmpty || clr == Color.Transparent)
                            PageRichText.BackColor = Color.White;
                        else
                            PageRichText.BackColor = clr;
                    }
                    PageNumber = 0;
                    isWordsDiscriminatorEmpty = !Discriminators.ActiveDiscriminators(darkMode);
                    Set(SurahNumber, AyahNumber);
                }
            }
        }

        public void SetWidth()
        {
            PageRichText.SelectAll();
            PageRichText.SelectionAlignment = HorizontalAlignment.Center;
            PageRichText.DeselectAll();
            PageNumber = 0; //
            Set(SurahNumber, AyahNumber); //
        }


        private TrueTypeFontQuran()
        {
            quran = new SQLiteConnection();
            command = new SQLiteCommand(quran);
        }

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
                if (reader.GetInt32(0) != 5 || reader.GetInt32(1) != 2) return false;
                Narration = reader.GetInt32(2);
                SurahsCount = reader.GetInt32(3);
                PagesCount = reader.GetInt32(5);
                Extension = reader.GetString(6);
                Comment = reader.GetString(7);
                reader.Close();

                PageNumber = 0;

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
                pagesSizeUnits = new float[PagesCount];
                SetPagesSizeUnits();
                CatchFontFile(0, ref bsml);
                DiscriminatorsReader();
                Discriminators.GetDiscriminators(path + "Colors.txt");
                isWordsDiscriminatorEmpty = !Discriminators.ActiveDiscriminators(darkMode);
                GetInitialColors();
                Set(sura, aya);
            }
            return success;
        }

        private void SetPagesSizeUnits()
        {
            command.CommandText = $"SELECT * FROM pages";
            quran.Open();
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                pagesSizeUnits[reader.GetInt32(0) - 1] = reader.GetInt32(1) / 1000f;
            }
            reader.Close();
            quran.Close();
        }

        #region التنقلات في المصحف
        public string[] GetSurahNames()
        {
            if (!success) return new string[] { "" };
            string[] names = new string[SurahsCount];
            quran.Open();
            command.CommandText = $"SELECT id,name FROM surahs";
            reader = command.ExecuteReader();

            while (reader.Read())
                names[reader.GetInt32(0) - 1] = reader.GetString(1);

            reader.Close(); quran.Close();
            return names;
        }

        private bool AyahAt(int id)
        {
            quran.Open();
            command.CommandText = $"SELECT id,surah,quarter,page,ayah,words_count FROM ayat WHERE id >= {id} AND ayah >= 0 LIMIT 1";
            reader = command.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close(); quran.Close();
                return false;
            }
            ayahId = reader.GetInt32(0);
            int surah = reader.GetInt32(1);
            QuarterNumber = reader.GetInt32(2);
            AyahNumber = reader.GetInt32(4);
            wordsCount = reader.GetInt32(5);
            reader.Close(); quran.Close();

            if (surah != SurahNumber) SurahData(surah);
            AyahData();

            return true;
        }

        public bool Set(int surah = 0, int ayah = -2, bool next = false, int juz = 0, int hizb = 0, int quarter = 0, int page = 0)
        {
            if (!success) return false;

            VisualAyah? match = VisualQuranNavigation.Find(quran, this, SurahsCount, PagesCount, ayahId,
                surah, ayah, next, juz, hizb, quarter, page, includeWordCount: true);
            if (!match.HasValue) return false;

            VisualAyah verse = match.Value;
            ayahId = verse.Id;
            QuarterNumber = verse.Quarter;
            AyahNumber = verse.Ayah;
            wordsCount = verse.WordCount;
            if (verse.Surah != SurahNumber) SurahData(verse.Surah);
            if (verse.Page != PageNumber) PageData(verse.Page);
            AyahData();

            return true;
        }

        private void SurahData(int surah)
        {
            SurahNumber = surah;
            command.CommandText = $"SELECT makya_madanya,ayat_count FROM surahs WHERE id = {surah}";
            quran.Open();
            reader = command.ExecuteReader();
            reader.Read();
            Makya_Madanya = reader.GetBoolean(0);
            AyatCount = reader.GetInt32(1);
            reader.Close(); quran.Close();
        }

        private readonly List<int[]> pageWords = new List<int[]>();
        private void PageData(int page)
        {
            PageNumber = page;
            PageRichText.Text = "";
            pageWords.Clear();

            CatchFontFile(page, ref fPage);

            PageRichText.Font = new Font(fPage.Families[0], PageRichText.Width / pagesSizeUnits[PageNumber - 1], GraphicsUnit.Pixel);
            Font fBsml = new Font(bsml.Families[0], PageRichText.Width / pagesSizeUnits[PageNumber - 1], GraphicsUnit.Pixel);

            command.CommandText = $"SELECT ayah_id,ayah,line,word,discriminator,text FROM ayat JOIN words ON words.ayah_id = ayat.id WHERE page = {page}";
            int line = 1, index;
            string s; Color clr;
            int discri;
            quran.Open();
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                s = "";
                index = PageRichText.Text.Length;
                if (line != reader.GetInt32(2))
                {
                    line = reader.GetInt32(2);
                    s = "\n";
                    pageWords.Add(null);
                }
                discri = reader.GetInt32(4);
                pageWords.Add(new int[3] { reader.GetInt32(0), reader.GetInt32(3), discri });
                s += reader.GetString(5);
                PageRichText.AppendText(s);
                if (reader.GetInt32(1) <= 0)
                {
                    PageRichText.Select(index, s.Length);
                    PageRichText.SelectionFont = fBsml;
                    PageRichText.DeselectAll();
                }
                if (Discriminators.KeyExists(0, discri))
                {
                    clr = Discriminators.PageColors[discri];
                    if (!clr.IsEmpty)
                    {
                        PageRichText.Select(index + s.Length - 1, 1);
                        PageRichText.SelectionColor = clr;
                        PageRichText.DeselectAll();
                    }
                }
                PageRichText.Select(PageRichText.TextLength, 0);
                PageRichText.SelectionFont = PageRichText.Font;
                PageRichText.SelectionColor = PageRichText.ForeColor;
            }
            pageWords.Add(null);
            reader.Close(); quran.Close();
            PageRichText.SelectAll();
            PageRichText.SelectionAlignment = HorizontalAlignment.Center;
            PageRichText.DeselectAll();

            prevAyah = -1; prevWord = -1;
        }

        private bool CatchFontFile(int page, ref PrivateFontCollection coll)
        {
            coll?.Dispose();
            coll = new PrivateFontCollection();
            string filePath = FindFontFile(page);
            if (filePath != null) coll.AddFontFile(filePath);
            return coll.Families.Length != 0;
        }

        private string FindFontFile(int page)
        {
            string s = page.ToString().PadLeft(3, '0');
            string filePath = $"{path}{s}{Extension}";
            if (File.Exists(filePath)) return filePath;
            filePath = $"{path}{page}{Extension}";
            if (File.Exists(filePath)) return filePath;
            foreach (string file in Directory.EnumerateFiles(path, $"*{Extension}"))
            {
                string name = Path.GetFileName(file);
                if (name.Contains(s) || page == 0 && name.IndexOf("bsml", StringComparison.OrdinalIgnoreCase) >= 0)
                    return file;
            }
            return null;
        }

        private int ayahIdIndex, wordIndex = 0;
        private void AyahData()
        {
            if (prevAyah >= 0) PrevAyah(prevAyah);

            prevWord = -1;
            CurrentWord = -1;
            int index = pageWords.FindIndex(arr => arr?[0] == ayahId);
            ayahIdIndex = index;
            Color clr;
            for (; index < pageWords.Count; index++)
            {
                if (pageWords[index] == null)
                    continue;
                else if (pageWords[index][0] != ayahId)
                    break;
                else if (!Discriminators.KeyExists(1, pageWords[index][2]))
                    continue;
                clr = Discriminators.AyahColors[pageWords[index][2]];
                if (clr.Name == "AyahColor") clr = GetColor(1, darkMode);
                if (!clr.IsEmpty)
                {
                    PageRichText.Select(index, 1);
                    PageRichText.SelectionColor = clr;
                    PageRichText.DeselectAll();
                }
            }

            prevAyah = ayahId;
        }

        private void PrevAyah(int ayahId)
        {
            int index = ayahIdIndex;
            Color clr = Color.Empty;
            for (; index < pageWords.Count; index++)
            {
                if (pageWords[index] == null)
                    continue;
                else if (pageWords[index][0] != ayahId)
                    break;
                else if (Discriminators.KeyExists(0, pageWords[index][2]))
                    clr = Discriminators.PageColors[pageWords[index][2]];

                if (clr.IsEmpty) clr = PageRichText.ForeColor;

                PageRichText.Select(index, 1);
                PageRichText.SelectionColor = clr;
                PageRichText.DeselectAll();
            }
        }

        public bool WordOf(int word)
        {
            if (prevWord >= 0) PrevWord(prevWord);

            CurrentWord = -1; prevWord = -1;
            if (WordMode && !isWordsDiscriminatorEmpty && word > 0 && word <= wordsCount)
            {
                int index = pageWords.FindIndex(ayahIdIndex, arr => arr?[1] == word);
                wordIndex = index;
                if (index == -1) return false;
                CurrentWord = word;
                Color clr;
                for (; index < pageWords.Count; index++)
                {
                    if (pageWords[index] == null)
                        continue;
                    else if (pageWords[index][0] != ayahId || pageWords[index][1] != word)
                        break;
                    else if (!Discriminators.KeyExists(2, pageWords[index][2]))
                        continue;

                    clr = Discriminators.WordColors[pageWords[index][2]];
                    if (clr.Name == "WordColor") clr = GetColor(2, darkMode);
                    if (!clr.IsEmpty)
                    {
                        PageRichText.Select(index, 1);
                        PageRichText.SelectionColor = clr;
                        PageRichText.DeselectAll();
                        prevWord = word;
                    }
                }
                return true;
            }
            return false;
        }

        private void PrevWord(int word)
        {
            if (WordMode && !isWordsDiscriminatorEmpty && word > 0 && word <= wordsCount)
            {
                int index = wordIndex;
                if (index == -1) return;

                Color clr = Color.Empty;
                for (; index < pageWords.Count; index++)
                {
                    if (pageWords[index] == null)
                        continue;
                    else if (pageWords[index][0] != ayahId || pageWords[index][1] != word)
                        break;
                    else if (Discriminators.KeyExists(1, pageWords[index][2]))
                        clr = Discriminators.AyahColors[pageWords[index][2]];
                    else if (Discriminators.KeyExists(0, pageWords[index][2]))
                        clr = Discriminators.PageColors[pageWords[index][2]];

                    if (clr.Name == "AyahColor") clr = GetColor(1, darkMode);
                    if (clr.IsEmpty) clr = PageRichText.ForeColor;

                    PageRichText.Select(index, 1);
                    PageRichText.SelectionColor = clr;
                    PageRichText.DeselectAll();
                }
            }
        }

        public bool SetCursor(int position = -1)
        {
            PageRichText.Enabled = false; PageRichText.Enabled = true;
            if (!success) return false;
            if (position < 0) position = PageRichText.SelectionStart;
            if (position > pageWords.Count) return false;

            int[] current = pageWords[position] ?? pageWords[position - 1];
            if (!AyahAt(current[0]))
                return false;

            WordOf(current[1]);
            return true;
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

        #region lines images
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

        private sealed class ExportFontLine
        {
            public readonly StringBuilder Text = new StringBuilder();
            public readonly List<Color> PageColors = new List<Color>();
            public readonly List<Color> WordColors = new List<Color>();
            public readonly Dictionary<(int Ayah, int Word), List<int>> GlyphIndices = new Dictionary<(int, int), List<int>>();
            public FontLineRenderer.Layout Layout;
            public Color[] Colors;
        }

        public void GetAyatInLinesWithWordsMarks(List<int> ayahword, int width, int height, int locx, int locy, int linWdth, int linHght, bool autoHeight, bool yEdit, string path, List<string> paths, int surah, int page)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            paths.Clear();
            var export = PrepareLineExport(ayahword, width, height, locx, locy, linWdth, linHght, autoHeight, yEdit, path, surah, page);
            if (export != null) paths.AddRange(export());
        }

        internal Func<List<string>> PrepareLineExport(List<int> ayahword, int width, int height, int locx, int locy, int linWdth, int linHght,
            bool autoHeight, bool yEdit, string path, int surah, int page)
        {
            if (!success || ayahword == null || ayahword.Count < 2 || page < 1 || page > pagesSizeUnits.Length) return null;
            var wordLines = new Dictionary<(int, int), ExportFontLine>();
            ExportFontLine line = ReadExportFontLines(surah, page, wordLines);
            string fontPath = FindFontFile(page);
            if (line == null || fontPath == null || pagesSizeUnits[page - 1] <= 0) return null;
            int[] intervals = ayahword.ToArray();
            float fontSize = linWdth / pagesSizeUnits[page - 1], underlineY = UnderlineY, strokeWidth = StrokeWidth;
            bool colorFont = UseColoringFont, dark = darkMode;
            return () => RenderLineExport(intervals, line, wordLines, fontPath, fontSize, underlineY, strokeWidth, colorFont, dark,
                width, height, locx, locy, linWdth, linHght, autoHeight, yEdit, path);
        }

        private static List<string> RenderLineExport(int[] intervals, ExportFontLine line, Dictionary<(int, int), ExportFontLine> wordLines,
            string fontPath, float fontSize, float underlineY, float strokeWidth, bool colorFont, bool dark,
            int width, int height, int locx, int locy, int linWdth, int linHght, bool autoHeight, bool yEdit, string path)
        {
            var paths = new List<string>(intervals.Length / 2);
            using (var fontPage = new PrivateFontCollection())
            {
                fontPage.AddFontFile(fontPath);
                if (fontPage.Families.Length == 0) return paths;
                using (var font = new Font(fontPage.Families[0], fontSize, GraphicsUnit.Pixel))
                using (var renderer = new FontLineRenderer(font, colorFont ? fontPath : null, dark, underlineY, strokeWidth))
                using (var output = new LineFrameWriter(path, width, height))
                {
                    for (int i = 0; i < intervals.Length / 2; i++)
                    {
                        var key = (intervals[i * 2], intervals[i * 2 + 1]);
                        if (key.Item2 >= 0 && wordLines.TryGetValue(key, out ExportFontLine selected)) line = selected;
                        if (line.Layout == null)
                        {
                            line.Layout = renderer.Prepare(line.Text.ToString());
                            line.Colors = line.PageColors.ToArray();
                        }
                        Color[] colors = line.Colors;
                        if (key.Item2 >= 0 && line.GlyphIndices.TryGetValue(key, out List<int> indices))
                        {
                            colors = (Color[])colors.Clone();
                            foreach (int glyph in indices) colors[glyph] = line.WordColors[glyph];
                        }
                        using (Bitmap rendered = renderer.Draw(line.Layout, colors))
                        {
                            int currentHeight = autoHeight ? (int)(1f * rendered.Height / rendered.Width * linWdth) : linHght;
                            int currentY = autoHeight && yEdit ? locy - (currentHeight - linHght) / 2 : locy;
                            var target = new Rectangle(locx + (linWdth - rendered.Width) / 2,
                                currentY + (currentHeight - rendered.Height) / 2, rendered.Width, rendered.Height);
                            paths.Add(output.Write(i, rendered, target, scale: false));
                        }
                    }
                }
            }
            return paths;
        }

        private ExportFontLine ReadExportFontLines(int surah, int page, Dictionary<(int, int), ExportFontLine> wordLines)
        {
            ExportFontLine first = null, line = null;
            Color defaultColor = darkMode ? Color.White : Color.Black;
            Color wordHighlight = GetColor(2, darkMode);
            int previousLine = int.MinValue;
            using (var connection = new SQLiteConnection(quran.ConnectionString))
            using (var query = connection.CreateCommand())
            {
                query.CommandText = @"SELECT ayah,line,word,discriminator,text FROM ayat
                    JOIN words ON words.ayah_id=ayat.id WHERE surah=@surah AND page=@page
                    ORDER BY line,ayah,word,words.rowid";
                query.Parameters.AddWithValue("@surah", surah);
                query.Parameters.AddWithValue("@page", page);
                connection.Open();
                using (var rows = query.ExecuteReader())
                    while (rows.Read())
                    {
                        int number = rows.GetInt32(1);
                        if (number != previousLine)
                        {
                            previousLine = number;
                            line = new ExportFontLine();
                            if (first == null) first = line;
                        }
                        var key = (rows.GetInt32(0), rows.GetInt32(2));
                        if (!wordLines.ContainsKey(key)) wordLines.Add(key, line);
                        if (!line.GlyphIndices.TryGetValue(key, out List<int> indices))
                            line.GlyphIndices.Add(key, indices = new List<int>());
                        indices.Add(line.Text.Length);

                        int discriminator = rows.GetInt32(3);
                        if (!Discriminators.PageColors.TryGetValue(discriminator, out Color pageColor)) pageColor = defaultColor;
                        if (Discriminators.WordColors.TryGetValue(discriminator, out Color wordColor) && wordColor.Name == "WordColor")
                            wordColor = wordHighlight;
                        line.PageColors.Add(pageColor);
                        line.WordColors.Add(wordColor.IsEmpty ? pageColor : wordColor);

                        string glyph = rows.GetString(4);
                        line.Text.Append(glyph.Length > 0 ? glyph[0] : ' ');
                    }
            }
            return first;
        }

        public Bitmap DrawText(string text, Font font, Color[] colors = null)
        {
            if (string.IsNullOrEmpty(text)) return new Bitmap(1, 1);
            using (var renderer = new FontLineRenderer(font, UseColoringFont ? FindFontFile(PageNumber) : null, darkMode, UnderlineY, StrokeWidth))
                return renderer.Draw(renderer.Prepare(text), colors);
        }
        #endregion

    }
}
