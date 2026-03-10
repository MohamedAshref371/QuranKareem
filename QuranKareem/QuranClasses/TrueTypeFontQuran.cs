using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        readonly StringBuilder str = new StringBuilder();
        public bool Set(int surah = 0, int ayah = -2, bool next = false, int juz = 0, int hizb = 0, int quarter = 0, int page = 0)
        {
            if (!success) return false;

            #region SQL Building
            str.Length = 0;
            str.Append("SELECT id,surah,quarter,page,ayah,words_count FROM ayat WHERE ");
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
                str.Append($"surah >= {surah} AND ayah >= {ayah}");
            }

            // Surah and Ayah
            else if (surah >= 1 && surah <= SurahsCount + 1)
            {
                if (surah == SurahsCount + 1) surah = 1;
                if (ayah < 0) ayah = 0;
                str.Append($"surah >= {surah} AND ayah >= {ayah}");
            }

            // Ayah Plus
            else if (next && SurahNumber >= 1)
            {
                if (SurahNumber == SurahsCount && AyahNumber == AyatCount)
                    str.Append("ayah >= 0");
                else
                    str.Append($"id > {ayahId} AND ayah >= 0");
            }
            #endregion
            #region Juz, Hizb, Quarter and Page
            // Juz then Hizb and Quarter
            else if (juz >= 1 && juz <= 31)
            {
                if (juz == 31) juz = 1;
                if (quarter <= 0 || quarter > 8) quarter = 1;
                if (hizb == 2 && quarter >= 1 && quarter <= 4) quarter += 4;

                str.Append($"quarter >= {juz * 8 - 8 + quarter} AND ayah >= 0");
            }

            // Hizb then Quarter
            else if (hizb >= 1 && hizb <= 61)
            {
                if (hizb == 61) hizb = 1;
                if (quarter <= 0 || quarter > 4) quarter = 1;

                str.Append($"quarter >= {hizb * 4 - 4 + quarter} AND ayah >= 0");
            }

            // Quarter only
            else if (quarter >= 1 && quarter <= 241)
            {
                if (quarter == 241) quarter = 1;
                str.Append($"quarter >= {quarter} AND ayah >= 0");
            }

            // Page
            else if (page >= 1 && page <= PagesCount + 1)
            {
                if (page == PagesCount + 1) page = 1;
                str.Append($"page >= {page} AND ayah >= 0");
            }
            #endregion
            else
            {
                str.Append($"surah >= {SurahNumber} AND ayah >= {AyahNumber}");
            }
            str.Append(" LIMIT 1");
            #endregion

            #region SQL Execution
            quran.Open();
            command.CommandText = str.ToString();
            reader = command.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close(); quran.Close();
                return false;
            }
            ayahId = reader.GetInt32(0);
            surah = reader.GetInt32(1);
            QuarterNumber = reader.GetInt32(2);
            page = reader.GetInt32(3);
            AyahNumber = reader.GetInt32(4);
            wordsCount = reader.GetInt32(5);
            reader.Close(); quran.Close();
            #endregion

            if (surah != SurahNumber) SurahData(surah);
            if (page != PageNumber) PageData(page);
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
            string s = page.ToString().PadLeft(3, '0');

            coll?.Dispose();
            coll = new PrivateFontCollection();

            string filePath = $"{path}{s}{Extension}";

            if (File.Exists(filePath))
                coll.AddFontFile(filePath);
            else
            {
                filePath = $"{path}{page}{Extension}";

                if (File.Exists(filePath))
                    coll.AddFontFile(filePath);
                else
                {
                    string[] filesName = Directory.GetFiles(path, $"*{Extension}");

                    foreach (string file in filesName)
                    {
                        string name = Path.GetFileName(file);

                        if (name.Contains(s) || page == 0 && name.IndexOf("bsml", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            coll.AddFontFile(file);
                            break;
                        }
                    }
                }
            }

            return coll.Families.Length != 0;
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
            if (reader.Read())
            {
                ints[0] = reader.GetInt32(0);
                ints[1] = reader.GetInt32(1);
            }
            reader.Close();
            quran.Close();
            return ints;
        }

#warning very slow
        public void GetAyatInLinesWithWordsMarks(List<int> ayahword, int width, int height, int locx, int locy, int linWdth, int linHght, bool autoHeight, bool yEdit, string path, List<string> paths, int surah, int page)
        {
            paths.Clear();
            if (!success || ayahword == null || paths == null || ayahword.Count == 0) return;

            try
            {
                Directory.CreateDirectory($"{path}\\img\\");

                List<List<int>> lines = new List<List<int>>();
                List<List<char>> texts = new List<List<char>>();
                List<List<Color>> pColors = new List<List<Color>>();
                List<List<Color>> wColors = new List<List<Color>>();

                GetLinesData(lines, texts, pColors, wColors, surah, page);

                if (lines.Count == 0) return;

                PrivateFontCollection fontPage = null;
                CatchFontFile(page, ref fontPage);

                if (fontPage?.Families.Length == 0) return;

                Font f = new Font(fontPage.Families[0], linWdth / pagesSizeUnits[page - 1], GraphicsUnit.Pixel);

                int lineIdx = 0;

                for (int i = 0; i < ayahword.Count / 2; i++)
                {
                    if (ayahword[i * 2 + 1] >= 0)
                        lineIdx = GetIndexLineAtAyahWord(lines, ayahword[i * 2], ayahword[i * 2 + 1]);

                    if (lineIdx < 0 || lineIdx >= lines.Count) continue;

                    Color[] lineColors = GetLineColorsAtAyahWord(lines[lineIdx], pColors[lineIdx], wColors[lineIdx], ayahword[i * 2], ayahword[i * 2 + 1]);
                    Bitmap bmp0 = DrawText(string.Concat(texts[lineIdx]), f, lineColors);

                    int currentHeight = linHght;
                    int currentLocy = locy;

                    if (autoHeight)
                    {
                        currentHeight = (int)(1f * bmp0.Height / bmp0.Width * linWdth);
                        if (yEdit) currentLocy -= (currentHeight - linHght) / 2;
                    }

                    using (Bitmap bmp = new Bitmap(width, height))
                    using (Graphics gr = Graphics.FromImage(bmp))
                    {
                        gr.Clear(Color.Transparent);
                        gr.DrawImage(bmp0, locx + (linWdth - bmp0.Width) / 2, currentLocy + (currentHeight - bmp0.Height) / 2);
                        bmp.Save($"{path}\\img\\{i}.png", System.Drawing.Imaging.ImageFormat.Png);
                    }

                    bmp0.Dispose();
                    paths.Add($"img\\{i}.png");
                }

                f.Dispose();
                fontPage?.Dispose();
            }
            catch { }
        }

        private Color[] GetLineColorsAtAyahWord(List<int> line, List<Color> pClrs, List<Color> wClrs, int ayah, int word)
        {
            Color[] clrs = pClrs.ToArray();

            // Binary-like search for the starting index
            int idx = 0;
            int pairCount = line.Count / 2;

            // Linear search (acceptable since line typically contains few pairs)
            for (int i = 0; i < pairCount; i++)
            {
                if (line[i * 2] == ayah && line[i * 2 + 1] == word)
                {
                    idx = i;
                    break;
                }
            }

            // Apply word colors for matching ayah and word
            while (idx < pairCount && line[idx * 2] == ayah && line[idx * 2 + 1] == word)
            {
                clrs[idx] = wClrs[idx];
                idx++;
            }

            return clrs;
        }

        private int GetIndexLineAtAyahWord(List<List<int>> lines, int ayah, int word)
        {
            int idx = lines.FindIndex(ln => ln[0] > ayah || (ln[0] == ayah && ln[1] >= word));

            if (idx == -1)
                idx = lines.Count - 1;
            else if (lines[idx][0] != ayah || lines[idx][1] != word)
                idx -= 1;

            return idx;
        }

        private void GetLinesData(List<List<int>> lines, List<List<char>> texts, List<List<Color>> pColors, List<List<Color>> wColors, int surah, int page)
        {
            List<int> lLine = null;
            List<char> lChars = null;
            List<Color> lpc = null;
            List<Color> lwc = null;
            Color clrP, clr;
            int previousLine = -1;

            try
            {
                command.CommandText = $"SELECT ayah,line,word,discriminator,text FROM ayat JOIN words ON words.ayah_id = ayat.id WHERE surah={surah} AND page={page} ORDER BY line,ayah,word";
                quran.Open();
                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int currentLine = reader.GetInt32(1);

                    if (currentLine != previousLine)
                    {
                        previousLine = currentLine;
                        lLine = new List<int>(32);
                        lines.Add(lLine);
                        lChars = new List<char>(32);
                        texts.Add(lChars);
                        lpc = new List<Color>(32);
                        pColors.Add(lpc);
                        lwc = new List<Color>(32);
                        wColors.Add(lwc);
                    }

                    lLine.Add(reader.GetInt32(0));
                    lLine.Add(reader.GetInt32(2));

                    int discriminator = reader.GetInt32(3);

                    // Get page color
                    if (Discriminators.KeyExists(0, discriminator))
                        clrP = Discriminators.PageColors[discriminator];
                    else
                        clrP = darkMode ? Color.White : Color.Black;
                    lpc.Add(clrP);

                    // Get word color
                    clr = Color.Empty;
                    if (Discriminators.KeyExists(2, discriminator))
                    {
                        clr = Discriminators.WordColors[discriminator];
                        if (clr.Name == "WordColor")
                            clr = GetColor(2, darkMode);
                    }

                    if (clr.IsEmpty)
                        clr = clrP;
                    lwc.Add(clr);

                    string text = reader.GetString(4);
                    lChars.Add(text.Length > 0 ? text[0] : ' ');
                }
            }
            finally
            {
                reader?.Close();
                quran?.Close();
            }
        }

        public Bitmap DrawText(string text, Font font, Color[] colors = null)
        {
            if (UseColoringFont && !string.IsNullOrEmpty(text))
            {
                Bitmap skiaResult = DrawTextWithSkiaSharp(text, font, colors);
                if (skiaResult != null)
                    return skiaResult;
            }

            // Fallback to GDI+ rendering
            return DrawTextWithGDI(text, font, colors);
        }

        private Bitmap DrawTextWithSkiaSharp(string text, Font font, Color[] colors = null)
        {
            if (string.IsNullOrEmpty(text))
                return new Bitmap(1, 1);

            string fontPath = Path.Combine(path, $"{PageNumber.ToString().PadLeft(3, '0')}{Extension}");
            if (!File.Exists(fontPath))
            {
                fontPath = Path.Combine(path, $"{PageNumber}{Extension}");
            }
            if (!File.Exists(fontPath))
            {
                return null; // Fallback to GDI+
            }

            SKTypeface typeface = SKTypeface.FromFile(fontPath);

            SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                Typeface = typeface,
                TextSize = font.Size,
                SubpixelText = true,
                LcdRenderText = true
            };

            float width = 0;
            float height = paint.FontMetrics.Descent - paint.FontMetrics.Ascent;

            float[] charWidths = new float[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                charWidths[i] = paint.MeasureText(text[i].ToString());
                width += charWidths[i];
            }

            int bmpWidth = (int)Math.Ceiling(width);
            int bmpHeight = (int)Math.Ceiling(height);

            SKImageInfo info = new SKImageInfo(bmpWidth, bmpHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

            SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            bool hasColors = colors != null && colors.Length == text.Length;

            float x = bmpWidth;
            float baseline = -paint.FontMetrics.Ascent;

            for (int i = 0; i < text.Length; i++)
            {
                float w = charWidths[i];
                x -= w;

                Color c = hasColors ? colors[i] : (darkMode ? Color.White : Color.Black);

                bool isWhite = c.R == 255 && c.G == 255 && c.B == 255;
                bool isBlack = c.R == 0 && c.G == 0 && c.B == 0;
                bool shouldUnderline = !isWhite && !isBlack;

                paint.Color = new SKColor(c.R, c.G, c.B, c.A);

                canvas.DrawText(text[i].ToString(), x, baseline, paint);

                if (shouldUnderline)
                {
                    paint.StrokeWidth = 2f; 
                    float underlineY = baseline + 70f; // يجب ان يكون رقم 70 متغير وليس ثابت

                    canvas.DrawLine(x, underlineY, x + w, underlineY, paint);
                }
            }

            SKImage img = surface.Snapshot();
            SKData data = img.Encode(SKEncodedImageFormat.Png, 100);

            MemoryStream ms = new MemoryStream(data.ToArray());
            Bitmap bmp = new Bitmap(ms);

            if (darkMode)
                for (int y = 0; y < bmp.Height; y++)
                    for (int x0 = 0; x0 < bmp.Width; x0++)
                    {
                        Color c = bmp.GetPixel(x0, y);
                        if (c.R < 40 && c.G < 40 && c.B < 40 && c.A > 0)
                            bmp.SetPixel(x0, y, Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B));
                    }

            data.Dispose();
            img.Dispose();
            surface.Dispose();
            paint.Dispose();
            typeface.Dispose();

            return bmp;
        }

        private Bitmap DrawTextWithGDI(string text, Font font, Color[] colors = null)
        {
            if (string.IsNullOrEmpty(text))
                return new Bitmap(1, 1);

            StringFormat sf = new StringFormat { Trimming = StringTrimming.Character };
            SizeF textSize;

            using (Bitmap imgT = new Bitmap(1, 1))
            using (Graphics drawingT = Graphics.FromImage(imgT))
            {
                textSize = drawingT.MeasureString(text, font);
            }

            int width = (int)textSize.Width;
            Bitmap img = new Bitmap(width, (int)textSize.Height);

            using (Graphics drawing = Graphics.FromImage(img))
            {
                drawing.CompositingQuality = CompositingQuality.HighQuality;
                drawing.InterpolationMode = InterpolationMode.HighQualityBilinear;
                drawing.PixelOffsetMode = PixelOffsetMode.HighQuality;
                drawing.SmoothingMode = SmoothingMode.HighQuality;
                drawing.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                drawing.Clear(Color.Transparent);

                bool hasColors = colors?.Length == text.Length;
                float xPos = width;

                // Pre-calculate all character widths
                int[] charWidths = new int[text.Length];
                using (Bitmap tmpBmp = new Bitmap(1, 1))
                using (Graphics tmpGfx = Graphics.FromImage(tmpBmp))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        SizeF charSize = tmpGfx.MeasureString(text[i].ToString(), font);
                        charWidths[i] = (int)charSize.Width;
                    }
                }

                // Draw characters with kerning adjustment
                for (int i = 0; i < text.Length; i++)
                {
                    int charWidth = charWidths[i];
                    int kernAdjust = 0;

                    if (i > 0 && i < text.Length)
                    {
                        using (Bitmap tmpBmp = new Bitmap(1, 1))
                        using (Graphics tmpGfx = Graphics.FromImage(tmpBmp))
                        {
                            SizeF pairSize = tmpGfx.MeasureString(text[i - 1] + text[i].ToString(), font);
                            kernAdjust = (int)pairSize.Width - charWidths[i - 1] - charWidth;
                        }
                        charWidth += kernAdjust;
                    }

                    xPos -= charWidth;

                    Color drawColor = hasColors ? colors[i] : (darkMode ? Color.White : Color.Black);
                    using (Brush textBrush = new SolidBrush(drawColor))
                    {
                        drawing.DrawString(text[i].ToString(), font, textBrush, new PointF(xPos, 0), sf);
                    }
                }
            }

            return img;
        }
        #endregion

    }
}
