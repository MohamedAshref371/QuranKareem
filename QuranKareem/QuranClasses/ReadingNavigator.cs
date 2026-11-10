using System;

namespace QuranKareem
{
    internal sealed class ReadingPosition
    {
        public int Surah { get; }
        public int Ayah { get; }
        public int AyatCount { get; }
        public int Quarter { get; }
        public int Page { get; }
        public int Word { get; }

        public int Juz => (Quarter - 1) / 8 + 1;
        public int Hizb => (Quarter - 1) / 4 + 1;

        public ReadingPosition(IVisualQuran visual)
        {
            Surah = visual.SurahNumber;
            Ayah = visual.AyahNumber;
            AyatCount = visual.AyatCount;
            Quarter = visual.QuarterNumber;
            Page = visual.PageNumber;
            Word = visual.CurrentWord;
        }
    }

    // Owns reading navigation. The form only chooses a destination and displays the result.
    internal sealed class ReadingNavigator
    {
        private readonly IVisualQuran visual;

        public ReadingNavigator(IVisualQuran visual)
        {
            this.visual = visual ?? throw new ArgumentNullException(nameof(visual));
        }

        public ReadingPosition Position => new ReadingPosition(visual);

        public bool GoToSurah(int surah) => visual.Set(surah: surah);
        public bool GoToAyah(int surah, int ayah) => visual.Set(surah: surah, ayah: ayah);
        public bool GoToPage(int page) => visual.Set(page: page);
        public bool GoToQuarter(int quarter) => visual.Set(quarter: quarter);
        public bool GoToJuz(int juz) => GoToQuarter((juz - 1) * 8 + 1);
        public bool GoToHizb(int hizb) => GoToQuarter((hizb - 1) * 4 + 1);
        public bool GoToWord(int word) => visual.WordOf(word);
    }
}
