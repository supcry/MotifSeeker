using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MotifSeeker.Data.Dna;

namespace MotifSeeker
{
    public class Motiff
    {
        /// <summary>
        /// Простейщее представление мотива. N - нуклеотид содержится минимум в половине случаев, n - в трети, ? - нет явного победителя.
        /// </summary>
        public readonly string MaskStr;

        /// <summary>
        /// Частоты встречамости нуклеотид. [позиция][нуклеотид]
        /// </summary>
        public readonly int[][] Freq;

        /// <summary>
        /// Сумма каждого столбца из Freq
        /// </summary>
        public readonly int[] Norm;

        /// <summary>
        /// На скольких элементарных мотивах построен этот вероятностный мотив.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// Длина мотива в нуклеотидах.
        /// </summary>
        public int Length { get { return Freq.Length; } }


        /// <summary>
        /// Создаёт мотив по выровненным цепочкам.
        /// </summary>
        public static Motiff ExtractMotiff(Nucleotide[][] map, int[] mapFactor)
        {
            bool started = false;
            var len = map.Max(p => p.Length);
            var freq = new List<int[]>();
            var cnt = mapFactor.Sum();
            for (int i = 0; i < len; i++)
            {
                var tmp = new int[4];
                for (int k = 0; k < map.Length; k++)
                {
                    var m = map[k];
                    if (m.Length <= i || (int) m[i] > 3)
                        continue;
                    tmp[(int) m[i]] += mapFactor[k];
                }
                if (!started)
                {
                    if (tmp.Sum() > cnt / 2)
                        started = true;
                    else
                        continue;
                }
                freq.Add(tmp);
            }
            var drop = freq.ToArray().Reverse().TakeWhile(p => p.Sum() < cnt / 2).Count();
            freq.RemoveRange(freq.Count - drop, drop);

            return new Motiff(cnt, freq.ToArray(), new string(freq.Select(p => GetMaskChar(p, cnt)).ToArray()));
        }

        

        private Motiff(int cnt, int[][] freq, string maskStr)
        {
            Count = cnt;
            Freq = freq;
            Norm = freq.Select(p => p.Sum()).ToArray();
            MaskStr = maskStr;
            CalcScoreNormFactor = 1.0 / Freq.Aggregate(1.0, (a, b) => a * b.Max());
        }

        private static char GetMaskChar(int[] freq, int cnt)
        {
            for (int i = 0; i < 4; i++)
                if (freq[i] > cnt/2)
                    return ((Nucleotide) i).ToString()[0];
            for (int i = 0; i < 4; i++)
                if (freq[i] > cnt/2.5)
                    return ((Nucleotide) i).ToString().ToLower()[0];
            for (int i = 0; i < 4; i++)
                if (freq[i] > cnt/3)
                    return ((Nucleotide)i).ToString().ToLower()[0];
            return '?';
        }

        public override string ToString()
        {
            return MaskStr + ", cnt:" + Count;
        }

        public readonly double CalcScoreNormFactor;


        /// <summary>
        /// Вычисляет коэффициент близости.
        /// </summary>
        /// <returns>1 - точное совпадение, 0 - отсутствие совпадения.</returns>
        public double CalcScore(Nucleotide[] data, int pos)
        {
            var ret = 1.0;
            if (pos < 0 || data.Length <= pos + Length)
                return 0;
            for (int i = 0; i < Length; i++)
            {
                var d = data[pos + i];
                var c = (d == Nucleotide.All) ? Freq[i].Min() : Freq[i][(byte) d];
                if (c == 0)
                    c = 1;//return 0;
                ret *= c;
            }
            return ret*CalcScoreNormFactor;
        }

        public double CalcMaxScore(Nucleotide[] data)
        {
            var ret = 0.0;
            foreach (Direction direction in Enum.GetValues(typeof (Direction)))
            {
                var b = data.GetChain(direction);
                for (int i = 0; i < data.Length - Length; i++)
                {
                    var tmp = CalcScore(b, i);
                    if (tmp > ret)
                        ret = tmp;
                }
            }
            
            return ret;
        }

        public double[] CalcMaxScore(Nucleotide[][] data)
        {
            return data.Select(CalcMaxScore).ToArray();
        }

        public double CalcMaxScore(Nucleotide[] data, int startPos, int endPos)
        {
            Debug.Assert(startPos > 0);
            var ret = 0.0;
            var end = Math.Min(data.Length - Length, endPos);
            for (int i = startPos; i < end; i++)
            {
                var tmp = CalcScore(data, i);
                if (tmp > ret)
                    ret = tmp;
            }
            return ret;
        }
    }
}
