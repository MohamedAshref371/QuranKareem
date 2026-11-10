using System;
using System.Data;
using System.Data.SQLite;

namespace QuranKareem
{
    internal readonly struct VisualAyah
    {
        public readonly int Id, Surah, Quarter, Page, Ayah, WordCount;

        public VisualAyah(SQLiteDataReader reader, bool includeWordCount)
        {
            Id = reader.GetInt32(0);
            Surah = reader.GetInt32(1);
            Quarter = reader.GetInt32(2);
            Page = reader.GetInt32(3);
            Ayah = reader.GetInt32(4);
            WordCount = includeWordCount ? reader.GetInt32(5) : 0;
        }
    }

    // All three renderers resolve a destination the same way, then draw it independently.
    internal static class VisualQuranNavigation
    {
        public static VisualAyah? Find(SQLiteConnection connection, IVisualQuran current, int surahsCount,
            int pagesCount, int currentId, int surah, int ayah, bool next, int juz, int hizb, int quarter, int page,
            bool includeWordCount = false)
        {
            using (var query = new SQLiteCommand(connection))
            {
                string filter = BindDestination(query, current, surahsCount, pagesCount, currentId, surah, ayah, next, juz, hizb, quarter, page);
                string wordColumn = includeWordCount ? ",words_count" : "";
                query.CommandText = "SELECT id,surah,quarter,page,ayah" + wordColumn + " FROM ayat WHERE " + filter + " LIMIT 1";

                bool openedHere = connection.State != ConnectionState.Open;
                if (openedHere) connection.Open();
                try
                {
                    using (SQLiteDataReader reader = query.ExecuteReader())
                        return reader.Read() ? new VisualAyah(reader, includeWordCount) : (VisualAyah?)null;
                }
                finally { if (openedHere) connection.Close(); }
            }
        }

        private static string BindDestination(SQLiteCommand query, IVisualQuran current, int surahsCount, int pagesCount,
            int currentId, int surah, int ayah, bool next, int juz, int hizb, int quarter, int page)
        {
            // The controls pass count + 1 when stepping past the end. Keep their wrap behavior.
            if ((surah <= 0 || surah == current.SurahNumber) && ayah >= -1)
            {
                surah = current.SurahNumber;
                if (ayah == current.AyatCount + 1)
                {
                    surah = surah == surahsCount ? 1 : surah + 1;
                    ayah = 0;
                }
                else if (ayah > current.AyatCount + 1)
                    ayah = current.AyatCount;

                return BindAyah(query, surah, Math.Max(0, ayah));
            }

            if (surah >= 1 && surah <= surahsCount + 1)
                return BindAyah(query, surah == surahsCount + 1 ? 1 : surah, Math.Max(0, ayah));

            if (next && current.SurahNumber >= 1)
            {
                if (current.SurahNumber == surahsCount && current.AyahNumber == current.AyatCount)
                    return "ayah >= 0";
                query.Parameters.AddWithValue("@id", currentId);
                return "id > @id AND ayah >= 0";
            }

            if (juz >= 1 && juz <= 31)
            {
                if (juz == 31) juz = 1;
                if (quarter < 1 || quarter > 8) quarter = 1;
                if (hizb == 2 && quarter <= 4) quarter += 4;
                return BindQuarter(query, (juz - 1) * 8 + quarter);
            }

            if (hizb >= 1 && hizb <= 61)
            {
                if (hizb == 61) hizb = 1;
                if (quarter < 1 || quarter > 4) quarter = 1;
                return BindQuarter(query, (hizb - 1) * 4 + quarter);
            }

            if (quarter >= 1 && quarter <= 241)
                return BindQuarter(query, quarter == 241 ? 1 : quarter);

            if (page >= 1 && page <= pagesCount + 1)
            {
                query.Parameters.AddWithValue("@page", page == pagesCount + 1 ? 1 : page);
                return "page >= @page AND ayah >= 0";
            }

            return BindAyah(query, current.SurahNumber, current.AyahNumber);
        }

        private static string BindAyah(SQLiteCommand query, int surah, int ayah)
        {
            query.Parameters.AddWithValue("@surah", surah);
            query.Parameters.AddWithValue("@ayah", ayah);
            return "surah >= @surah AND ayah >= @ayah";
        }

        private static string BindQuarter(SQLiteCommand query, int quarter)
        {
            query.Parameters.AddWithValue("@quarter", quarter);
            return "quarter >= @quarter AND ayah >= 0";
        }
    }
}
