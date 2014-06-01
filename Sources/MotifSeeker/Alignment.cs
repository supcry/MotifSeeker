using System;
using System.Linq;
using System.Text;
using MotifSeeker.Data.Dna;

namespace MotifSeeker
{
    /// <summary>
    /// Результат работы алгоритма выравнивания.
    /// </summary>
    public class AlignmentResult
    {
        /// <summary>
        /// Сколько нуклеотид совпало.
        /// </summary>
        public readonly int Weight;

        /// <summary>
        /// Маска совпавших нуклеотид.
        /// </summary>
        public readonly string Mask;

        /// <summary>
        /// Направление второй последовательности.
        /// </summary>
        public readonly Direction Direction;

        /// <summary>
        /// Смещение маски относительно первой последовательности.
        /// </summary>
        public readonly int Shift1;

        /// <summary>
        /// Смещение маски относительно второй последовательности.
        /// </summary>
        public readonly int Shift2;

        public AlignmentResult(int weight, string mask, Direction direction, int shift1, int shift2)
        {
            Weight = weight;
            Mask = mask;
            Direction = direction;
            Shift1 = shift1;
            Shift2 = shift2;
        }
    }

    /// <summary>
    /// Набор методов выравнивания
    /// </summary>
    public static class Alignment
    {
        /// <summary>
        /// Выравнивание двух последовательностей.
        /// Учитывает перестановки A-T и G-C, а также реверсии.
        /// </summary>
        public static AlignmentResult Align(Nucleotide[] a0, Nucleotide[] b0)
        {
            var bestDir = Direction.Straight;
            string bestMask = string.Empty;
            int bestWeight = -1;
            int bestShift1 = 0;
            int bestShift2 = 0;
            var tmp = new StringBuilder(Math.Max(a0.Length, b0.Length));
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var a = a0;
                var b = b0.GetChain(direction);
                for (int s = 0; s < b.Length + a.Length - 1; s++) // s - смещение a относительно b
                {
                    int w = 0;
                    var sa = Math.Max(0, a.Length - s - 1); // откуда начинать отсчёт от a
                    var sb = Math.Max(0, s + 1 - a.Length); // откуда начинать отсчёт от b
                    var len = Math.Min(a.Length - sa, b.Length - sb);
                    for (int i = 0; i < len; i++) // безим по смещённым последовательностям
                    {
                        var na = a[sa + i];
                        var nb = b[sb + i];
                        if (na == nb)
                        {
                            tmp.Append(na);
                            w++;
                        }
                        else
                            tmp.Append("?");
                    }
                    if (w > bestWeight)
                    {
                        bestWeight = w;
                        bestMask = tmp.ToString();
                        bestDir = direction;
                        bestShift1 = sa;
                        bestShift2 = sb;
                    }
                    tmp.Clear();
                }
            }
            if (bestMask.StartsWith("?"))
            {
                var cnt = bestMask.TakeWhile(p => p == '?').Count();
                bestShift1 += cnt;
                bestShift2 += cnt;
                bestMask = bestMask.Trim('?');
            }
            else
                bestMask = bestMask.TrimEnd('?');

            return new AlignmentResult(bestWeight, bestMask, bestDir, bestShift1, bestShift2);
        }
    }
}
