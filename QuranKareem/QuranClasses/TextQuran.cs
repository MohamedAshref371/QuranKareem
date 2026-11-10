using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static QuranKareem.Coloring;

namespace QuranKareem
{
    internal class TextQuran : IVisualQuran
    {
        #region Create The Class
        private bool success = false;

        private readonly SQLiteConnection quran;
        private readonly SQLiteCommand command;
        private SQLiteDataReader reader;

        public static readonly TextQuran Instance = new TextQuran();

        private TextQuran()
        {
            quran = new SQLiteConnection();
            command = new SQLiteCommand(quran);
            try
            {
                if (Directory.Exists("fonts"))
                {
                    string[] fontsFiles = Directory.GetFiles("fonts");
                    for (int i = 0; i < fontsFiles.Length; i++)
                        collection.AddFontFile(fontsFiles[i]);
                }
            }
            catch { }
        }
        #endregion
        //public int QuartersCount { get; private set; }
        public int PagesCount { get; private set; }

        public int SurahsCount { get; private set; }

        public int SurahNumber { get; private set; }
        public int AyahNumber { get; private set; }
        public int AyatCount { get; private set; }

        public int Narration { get; private set; }

        public int QuarterNumber { get; private set; }
        public int PageNumber { get; private set; }
        public bool Makya_Madanya { get; private set; }
        //public int AyahStart { get; private set; }

        public string Comment { get; private set; }

        private int pageStartId, ayahId;

        string /*fontFile,*/ fontName;

        public int CurrentWord { get; private set; } = -1;
        private bool isWordTableEmpty = true;
        public bool WordMode { get; set; } = true;

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
                        pageRichText.ForeColor = Color.White;
                        if (clr.IsEmpty || clr == Color.Transparent)
                            pageRichText.BackColor = Color.Black;
                        else
                            pageRichText.BackColor = clr;
                    }
                    else
                    {
                        clr = Light.BackColor;
                        pageRichText.ForeColor = Color.Black;
                        if (clr.IsEmpty || clr == Color.Transparent)
                            pageRichText.BackColor = Color.White;
                        else
                            pageRichText.BackColor = clr;
                    }
                    PageNumber = 0;
                    Set(SurahNumber,AyahNumber);
                }
            }
        }

        public string PageText { get; private set; }
        public string PageTextHTML { get; private set; }

        private readonly RichTextBox pageRichText = new RichTextBox()
        {
            RightToLeft = RightToLeft.Yes,
            WordWrap = false,
            Font = new Font("Tahoma", 20F),
            BackColor = SystemColors.Control,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            //ScrollBars = RichTextBoxScrollBars.None
        };

        private readonly PrivateFontCollection collection = new PrivateFontCollection();
        private readonly MemoryStream stream = new MemoryStream();

        private bool added = false;
        public void AddRichTextBoxInControls(Control.ControlCollection Controls, int locX, int locY, int width, int height)
        {
            if (!added) try
                {
                    pageRichText.Location = new Point(locX, locY);
                    pageRichText.Size = new Size(width > 0 ? width : 10, height > 0 ? height : 10);
                    textType = TextType.rich;
                    pageRichText.SelectAll();
                    pageRichText.SelectionAlignment = HorizontalAlignment.Center;
                    pageRichText.DeselectAll();
                    Controls.Add(pageRichText);
                    added = true;
                }
                catch { }
        }

        public bool Start(string file, int sura = 1, int aya = 0)
        {
            if (!File.Exists(file)) return false;

            quran.ConnectionString = $"Data Source={file}; Version=3;";
            success = false;
            try
            {
                quran.Open();

                command.CommandText = $"SELECT * FROM description";
                reader = command.ExecuteReader();

                if (!reader.HasRows) return false;
                reader.Read();

                if (reader.GetInt32(0)/*type 1:text, 2:picture, 3: audios*/ != 1 || reader.GetInt32(1) != 4) return false;
                Narration = reader.GetInt32(2); // العمود الثالث
                SurahsCount = reader.GetInt32(3);
                //quartersCount = reader.GetInt32(4);
                PagesCount = reader.GetInt32(5);
                //fontFile = reader.GetString(6);
                fontName = reader.GetString(7);
                Comment = reader.GetString(8);
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
                pageRichText.Font = new Font(collection.Families.FirstOrDefault(f => f.Name == fontName) ?? pageRichText.Font.FontFamily, pageRichText.Font.Size);
                GetChars();
                Set(sura, aya);
            }
            return success;
        }

        private void GetChars()
        {
            List<char> list = new List<char>();
            quran.Open();

            command.CommandText = $"SELECT char,type FROM chars WHERE type == 2 OR type == 3";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(Convert.ToChar(reader.GetString(0)));
                if (reader.GetInt32(1) == 3)
                    symbolStart = list.Last();
            }
            symbols = list.ToArray();
            reader.Close();

            list.Clear();
            command.CommandText = $"SELECT char FROM chars WHERE type == 1";
            reader = command.ExecuteReader();
            while (reader.Read())
                list.Add(Convert.ToChar(reader.GetString(0)));
            decorations = list.ToArray();

            reader.Close(); quran.Close();
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

        private bool AyahAt(int id)
        {
            quran.Open();
            command.CommandText = $"SELECT id,surah,quarter,page,ayah FROM ayat WHERE id >= {id} AND ayah >= 0 LIMIT 1";
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
            reader.Close(); quran.Close();

            if (surah != SurahNumber) SurahData(surah);
            AyahData();

            return true;
        }

        public bool Set(int surah = 0, int ayah = -2, bool next = false, int juz = 0, int hizb = 0, int quarter = 0, int page = 0)
        {
            if (!success) return false;

            VisualAyah? match = VisualQuranNavigation.Find(quran, this, SurahsCount, PagesCount, ayahId,
                surah, ayah, next, juz, hizb, quarter, page);
            if (!match.HasValue) return false;

            VisualAyah verse = match.Value;
            ayahId = verse.Id;
            QuarterNumber = verse.Quarter;
            AyahNumber = verse.Ayah;
            if (verse.Surah != SurahNumber) SurahData(verse.Surah);
            if (verse.Page != PageNumber) PageTextAt(verse.Page);
            AyahData();

            return true;
        }

        private void AyahData()
        {

            CurrentWord = -1;
            PageText = originalPageText.ToString();

            // التلوين
            int k = ayahId - pageStartId;
            int start = 0, finish;
            if (k > 0) start = finishedPosition[k - 1];
            finish = finishedPosition[k];

            if (textType == TextType.html)
            {
                if (GetColor(1, darkMode).A != 0)
                {
                    PageTextHTML = (start > 0 ? PageText.Substring(0, start) : "") + GetHtmlTextColor() + PageText.Substring(start, finish - start) + "</span>" + PageText.Substring(finish);
                }
                PageTextHTML = "<span dir=\"rtl\">" + PageText.Replace(Environment.NewLine, "<br>") + "</span>";
            }
            else if (textType == TextType.rich)
            {
                if (pageRichText.Text == "")
                {
                    pageRichText.Text = originalPageText.ToString();
                    for (int i = 0; i < pageRichText.Text.Length; i++)
                    {
                        if (symbols.Contains(pageRichText.Text[i]))
                        {
                            pageRichText.Select(i, 1);
                            pageRichText.SelectionColor = AyahEndColor;
                        }
                    }
                    stream.Position = 0;
                    pageRichText.SaveFile(stream, RichTextBoxStreamType.RichText);
                }
                else
                {
                    stream.Position = 0;
                    pageRichText.LoadFile(stream, RichTextBoxStreamType.RichText);
                }
                if (GetColor(1, darkMode).A != 0)
                {
                    pageRichText.Select(start, finish - start);
                    pageRichText.SelectionColor = GetColor(1, darkMode);
                    pageRichText.SelectionStart = start;
                }
                pageRichText.DeselectAll();
            }
        }

        private void SurahData(int surah)
        {
            SurahNumber = surah;
            command.CommandText = $"SELECT makya_madanya,ayat_count FROM surahs WHERE id={surah}";
            quran.Open();
            reader = command.ExecuteReader();
            reader.Read();
            Makya_Madanya = reader.GetBoolean(0);
            AyatCount = reader.GetInt32(1);
            reader.Close(); quran.Close();
        }

        private readonly List<int> finishedPosition = new List<int>();
        private readonly List<List<int>> wordsPosition = new List<List<int>>();

        #region Part Of Ayah Function
        private readonly StringBuilder originalPageText = new StringBuilder();
        private char[] decorations = { }, symbols = { };
        private char symbolStart = '۞';
        private void PageTextAt(int i)
        {
            PageNumber = i;
            originalPageText.Clear();
            finishedPosition.Clear(); int length = 0;
            wordsPosition.Clear();
            string s;
            command.CommandText = $"SELECT id,text FROM ayat WHERE page={i}";
            quran.Open();
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (finishedPosition.Count == 0)
                    pageStartId = reader.GetInt32(0);
                s = reader.GetString(1).Replace(Environment.NewLine, "\n");
                originalPageText.Append(s);
                length += s.Length;
                finishedPosition.Add(length);
            }
            reader.Close(); quran.Close();
            pageRichText.Text = "";

            if (WordMode && !isWordTableEmpty) AyatWords(i);
        }

        private void AyatWords(int page)
        {
            quran.Open();
            command.CommandText = $"SELECT aya.id,pure_text FROM (SELECT id FROM ayat WHERE page={page}) AS aya INNER JOIN words ON aya.id = words.ayah_id";
            int id = pageStartId - 1; int length = 0;
            string s;
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt32(0) > id)
                {
                    for (int k = 1; k < reader.GetInt32(0) - id; k++) wordsPosition.Add(null);
                    id = reader.GetInt32(0);
                    if (wordsPosition.Count >= 1) length = finishedPosition[wordsPosition.Count - 1];
                    wordsPosition.Add(new List<int>());
                    if (originalPageText[length] == symbolStart) length += 1;
                    wordsPosition.Last().Add(length);
                }
                s = reader.GetString(1);
                //مقارنة الكلمة
                for (int j = 0; j < s.Length;)
                {
                    if (!decorations.Contains(originalPageText[length]) && originalPageText[length] == s[j])
                    {
                        length++; j++;
                    }
                    else length++;
                }
                while (length < originalPageText.Length)
                    if (decorations.Contains(originalPageText[length])) length++;
                    else break;

                wordsPosition.Last().Add(length);
            }
            reader.Close(); quran.Close();
        }
        #endregion

        public bool WordOf(int word)
        {
            try
            {
                int verse = ayahId - pageStartId;
                if (textType == TextType.rich && verse >= 0 && verse < wordsPosition.Count && wordsPosition[verse] != null)
                {
                    List<int> offsets = wordsPosition[verse];
                    if (CurrentWord > 0 && CurrentWord < offsets.Count)
                    {
                        pageRichText.Select(offsets[CurrentWord - 1], offsets[CurrentWord] - offsets[CurrentWord - 1]);
                        Color ayahColor = GetColor(1, darkMode);
                        pageRichText.SelectionColor = ayahColor.A == 0 ? pageRichText.ForeColor : ayahColor;
                    }
                    CurrentWord = -1;
                    if (WordMode && word > 0 && word < offsets.Count && GetColor(2, darkMode).A != 0)
                    {
                        pageRichText.Select(offsets[word - 1], offsets[word] - offsets[word - 1]);
                        pageRichText.SelectionColor = GetColor(2, darkMode);
                        CurrentWord = word;
                    }
                    pageRichText.DeselectAll();
                    return CurrentWord > 0;
                }
            }
            catch { }
            return false;
        }

        public bool SetCursor(int position = -1)
        {
            if (!success) return false;
            if (position < 0) position = pageRichText.SelectionStart;
            bool successful = false; int i = 0;
            while (i < finishedPosition.Count)
            {
                if (position < finishedPosition[i])
                {
                    successful = AyahAt(i + pageStartId);
                    break;
                }
                i++;
            }
            if (successful && WordMode && !isWordTableEmpty && i < wordsPosition.Count)
            {
                for (int j = 1; j < wordsPosition[i]?.Count; j++)
                {
                    if (position < wordsPosition[i][j])
                    {
                        WordOf(j); break;
                    }
                }
            }
            return true;
        }
        #endregion

        public string AyahText(int sura, int aya)
        {
            if (!success) return "";
            quran.Open();
            command.CommandText = $"SELECT text FROM ayat WHERE surah={sura} AND ayah={aya}";
            reader = command.ExecuteReader();
            reader.Read();
            string tempString = reader.GetString(0);
            reader.Close(); quran.Close();
            return tempString.Replace(Environment.NewLine, " ").Trim();
        }

        public string AyahAbstractText(int sura, int aya)
        {
            if (!success) return "";
            quran.Open();
            command.CommandText = $"SELECT abstract_text FROM ayat WHERE surah={sura} AND ayah={aya}";
            reader = command.ExecuteReader();
            reader.Read();
            string tempString = reader.GetString(0);
            reader.Close(); quran.Close();
            return tempString;
        }

        public string[] SurahAbstractTexts(int sura, int wordsCount = -1, bool end = false)
        {
            if (!success) return null;
            lst.Clear();
            quran.Open();
            command.CommandText = $"SELECT ayah,abstract_text FROM ayat WHERE surah={sura}";
            reader = command.ExecuteReader();
            string[] words;
            string tempString;
            while (reader.Read())
            {
                tempString = reader.GetString(1);
                words = tempString.Split(' ');
                if (wordsCount >= 1 && wordsCount < words.Length)
                    if (end) tempString = string.Join(" ", words, words.Length - wordsCount, wordsCount);
                    else tempString = string.Join(" ", words, 0, wordsCount);
                tempString += " ";
                if (reader.GetInt32(0) > 0) tempString += reader.GetInt32(0);
                lst.Add(tempString);
            }
            reader.Close(); quran.Close();
            return lst.ToArray();
        }

        readonly List<string> lst = new List<string>();
        public string SpellingErrors(string s, bool taaMarbota = true)
            => QuranSearchService.Normalize(s, taaMarbota);

        internal int[] GetStartAndEndOfPage()
        {
            using (var query = new SQLiteCommand("SELECT MIN(ayah),MAX(ayah) FROM ayat WHERE surah=@surah AND page=@page AND ayah>=1", quran))
            {
                query.Parameters.AddWithValue("@surah", SurahNumber);
                query.Parameters.AddWithValue("@page", PageNumber);
                quran.Open();
                try
                {
                    using (var rows = query.ExecuteReader())
                        return rows.Read() && !rows.IsDBNull(0) ? new[] { rows.GetInt32(0), rows.GetInt32(1) } : null;
                }
                finally { quran.Close(); }
            }
        }

        public List<string[]> SelectAbstractWords(int surah, int ayahStart = 0, int ayahEnd = 287)
        {
            List<string[]> list = new List<string[]>();
            command.CommandText = $"SELECT abstract_text FROM ayat WHERE surah={surah} AND ayah>={ayahStart} AND ayah<={ayahEnd}";
            quran.Open();
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(reader.GetString(0).Split(' '));
            }
            reader.Close(); quran.Close();
            return list;
        }

        private string GetHtmlTextColor()
        {
            string s = "<span";
            Color clr = GetColor(1, darkMode);
            if (clr.A != 0) s += $" style=\"color: rgb({clr.R},{clr.G},{clr.B});\"";
            s += ">";
            return s;
        }

        public void AddEventHandler(EventHandler eh) { pageRichText.Click += eh; }

        public TextType textType = TextType.plain;
        public enum TextType
        {
            plain = 0, html = 1, rich = 2
        }
    }
}
