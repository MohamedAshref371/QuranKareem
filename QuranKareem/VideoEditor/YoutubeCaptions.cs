using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QuranKareem
{
    internal static class YoutubeCaptions
    {
        public static string TextPath;
        public static string Mp3Url = "";
        public static int Surah;

        public static void SetCaptionsVTT(string path, string savePath)
        {
            #region تمهيد
            List<string> lines = File.ReadAllLines(path).ToList();

            lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == "WEBVTT" || lines[i] == "Kind: captions" || lines[i].Contains("Language: "))
                {
                    lines.RemoveAt(i); i--;
                }
                else break;
            }

            lines = lines.Select(l => l.Trim().Replace("<c> ", "").Replace("</c>", "").Replace(" align:start position:100%", "")).ToList();

            lines = lines.Where(l => !l.Contains(" ") && !l.Contains("[") || l.Contains(">")).ToList();

            for (int i = 0; i < lines.Count - 1; i++)
            {
                if (lines[i].Contains(" --> ") && lines[i + 1].Contains(" --> "))
                {
                    lines.RemoveAt(i); i--;
                }
            }

            if (lines.Last().Contains(" --> "))
                lines.RemoveAt(lines.Count - 1);

            for (int i = 1; i < lines.Count - 1; i++)
            {
                if (lines[i - 1].Contains(" --> ") && (lines[i + 1].Contains("<") || !lines[i + 1].Contains(" --> ")))
                {
                    lines.RemoveAt(i); i--;
                }
            }

            for (int i = 0; i < lines.Count - 1; i += 2)
            {
                if (ConvertTimeToInteger(lines[i].Substring(17, 12)) - ConvertTimeToInteger(lines[i].Substring(0, 12)) <= 10)
                {
                    lines.RemoveAt(i);
                    lines.RemoveAt(i);
                    i -= 2;
                }
            }
            #endregion

            // بسم الله
            List<string> csv = new List<string>();
            int start, end;
            string text;
            string[] arr;

            for (int i = 0; i < lines.Count; i += 2)
            {
                start = ConvertTimeToInteger(lines[i].Substring(0, 12));
                end = ConvertTimeToInteger(lines[i].Substring(17, 12));
                text = lines[i + 1];
                if (!text.Contains("<"))
                {
                    csv.Add($"{start}|{end}|{text}");
                }
                else
                {
                    arr = text.Split('<', '>');
                    for (int j = 0; j < arr.Length; j += 2)
                    {
                        if (j != arr.Length - 1)
                        {
                            csv.Add($"{start}|{ConvertTimeToInteger(arr[j + 1])}|{arr[j]}");
                            start = ConvertTimeToInteger(arr[j + 1]);
                        }
                        else
                        {
                            csv.Add($"{start}|{end}|{arr[j]}");
                        }
                    }
                }
            }

            TextPath = savePath;
            File.WriteAllLines(savePath, csv);
            Process.Start(savePath);
        }

        private static int ConvertTimeToInteger(string s)
        {
            string[] ss = s.Split(':');
            int num = int.Parse(ss[0]) * 60 * 60 + int.Parse(ss[1]) * 60;

            ss = ss[2].Split('.');
            num += int.Parse(ss[0]);
            num *= 1000;
            num += int.Parse(ss[1]);

            return num;
        }

        public static float[] SetFinalText(string path, int surah, int ayahStart, int ayahEnd, List<int> ayahword, List<float> timestamps)
        {
            ayahword.Clear();
            timestamps.Clear();
            List<string> lines = File.ReadAllLines(path).ToList();
            lines = lines.Where(ln => !string.IsNullOrWhiteSpace(ln)).ToList();

            List<string[]> list = TextQuran.Instance.SelectAbstractWords(surah, ayahStart, ayahEnd);

            int ayah = 0, l = 0, word = 1;
            string[] line;
            int time = 0, timeCurrent;
            while (l < lines.Count)
            {
                line = lines[l].Split('|');

                if (l != 0)
                {
                    timeCurrent = int.Parse(line[0]);
                    if (timeCurrent - time > 1)
                    {
                        if (timeCurrent - time <= 12)
                        {
                            timestamps[timestamps.Count - 1] += (timeCurrent - time) / 1000f;
                        }
                        else
                        {
                            ayahword.Add(ayah + ayahStart);
                            ayahword.Add(-1);
                            timestamps.Add((timeCurrent - time) / 1000f);
                        }
                        time = int.Parse(line[0]);
                    }
                }
                else
                    time = int.Parse(line[0]);

                if (word == 0)
                {
                    if (ayah <= list.Count - 2 && Equals(list[ayah + 1][0], line[2]))
                    {
                        ayah++;
                        word = 1;
                    }
                    else
                    {
                        for (int i = list[ayah].Length - 1; i >= 0; i--){
                            if (Equals(list[ayah][i], line[2]))
                            {
                                word = i + 1;
                                break;
                            }
                        }
                    }
                }
                if (word == 0) { ayah++; word = 1; } //
                //if (word == 0) { l++; continue; }

                int w;
                if (Equals(list[ayah][word - 1], line[2]))
                {
                    ayahword.Add(ayah + ayahStart);
                    ayahword.Add(word);
                    timestamps.Add((int.Parse(line[1]) - int.Parse(line[0])) / 1000f);
                    time = int.Parse(line[1]);
                    if (word == list[ayah].Length) word = 0;
                    else word++;
                }
                else
                {
                    w = word;
                    word = 0;
                    for (int i = w - 1; i >= 0; i--)
                    {
                        if (Equals(list[ayah][i], line[2]))
                        {
                            word = i + 1;
                            ayahword.Add(ayah + ayahStart);
                            ayahword.Add(word);
                            timestamps.Add((int.Parse(line[1]) - int.Parse(line[0])) / 1000f);
                            time = int.Parse(line[1]);
                            word++;
                            break;
                        }
                    }
                    if (w < list[ayah].Length && word == 0)
                    {
                        word = w + 1;
                    }
                }
                l++;
            }

            return new float[2]
            {
                int.Parse(lines[0].Split('|')[0]) / 1000f,
                int.Parse(lines.Last().Split('|')[1]) / 1000f
            };
        }

        private static bool Equals(string s1, string s2)
            => TextQuran.Instance.SpellingErrors(s1, false) == TextQuran.Instance.SpellingErrors(s2, false);

        public static float[] WordsList(int ayahStart, int ayahEnd, out string mp3Url, List<int> ayahword, List<float> timestamps)
        {
            mp3Url = Mp3Url;
            return SetFinalText(TextPath, Surah, ayahStart, ayahEnd, ayahword, timestamps);
        }
    }
}
