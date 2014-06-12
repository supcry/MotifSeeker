using System;
using System.Linq;
using System.Text;
using MotifSeeker.Data.Dna;

namespace MotifSeeker
{
    /// <summary>
    /// Результат работы алгоритма парного выравнивания.
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

        public readonly int Id1;

        public readonly int Id2;

        public AlignmentResult(int weight, string mask, Direction direction, int shift1, int shift2, int id1, int id2)
        {
            Weight = weight;
            Mask = mask;
            Direction = direction;
            Shift1 = shift1;
            Shift2 = shift2;
            Id1 = id1;
            Id2 = id2;
        }
    }

    /// <summary>
    /// Результат работы алгоритма множественного выравнивания.
    /// </summary>
    public class MultiAlignmentResult
    {
        /// <summary>
        /// Выровненные последовательности (верхний регистр - нуклеотид занимает не менее 50% от мотива)
        /// </summary>
        public readonly Nucleotide[][] Map;

        /// <summary>
        /// Направление второй последовательности.
        /// </summary>
        public readonly Direction[] Directions;

        /// <summary>
        /// Смещение маски относительно первой последовательности.
        /// </summary>
        public readonly int[] Shifts;

        /// <summary>
        /// Число букв в верхнем регистре от Map.
        /// </summary>
        public int[] Weights;

        public string[] MapStr;

        public Nucleotide[] Mask;

        public string MaskStr;

        public int[][] MotiffRaw;

        public MultiAlignmentResult(int[] shifts, Direction[] directions, Nucleotide[][] map)
        {
            Shifts = shifts;
            Directions = directions;
            Map = map;
            Prepare();
        }

        private void Prepare()
        {
            MapStr = Map.Select(p => p.ChainToString()).ToArray();
            MotiffRaw = new int[Map.Max(p => p.Length)][];
            Mask = new Nucleotide[MotiffRaw.Length];
            for (int i = 0; i < MotiffRaw.Length; i++)
            {
                var tmp = new int[5];
                for (int k = 0; k < Map.Length; k++)
                {
                    var m = Map[k];
                    if(m.Length <= i)
                        continue;
                    var t = (int) m[i];
                    if (t == 5)
                        t = 4;
                    tmp[t]++;
                }
                MotiffRaw[i] = tmp;
                var total = tmp.Take(4).Sum();
                if (total > Map.Length/3)
                    Mask[i] = GetBest(tmp, total/2, total/3);
                else
                    Mask[i] = Nucleotide.All;
            }
            MaskStr = Mask.ChainToString();
            Weights = new int[Map.Length];
            for (int i = 0; i < Map.Length; i++)
            {
                var cnt = Map[i].Zip(Mask, (a, b) => a != Nucleotide.All && a == b ? 1 : 0).Count();
                Weights[i] = cnt;
            }
        }

        private static Nucleotide GetBest(int[] tmp, int threshold, int threshold2)
        {
            if (tmp[0] >= threshold)
                return Nucleotide.A;
            if (tmp[1] >= threshold)
                return Nucleotide.T;
            if (tmp[2] >= threshold)
                return Nucleotide.G;
            if (tmp[3] >= threshold)
                return Nucleotide.C;
            if (tmp[0] >= threshold2)
                return Nucleotide.a;
            if (tmp[1] >= threshold2)
                return Nucleotide.t;
            if (tmp[2] >= threshold2)
                return Nucleotide.g;
            if (tmp[3] >= threshold2)
                return Nucleotide.c;
            return Nucleotide.All;
        }

        public override string ToString()
        {
            return MaskStr.Trim().Replace(" ", "?");
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
        public static AlignmentResult Align(Nucleotide[] a0, Nucleotide[] b0, int id1 = 0, int id2 = 0)
        {
            var bestDir = Direction.Straight;
            var bestMask = string.Empty;
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

            return new AlignmentResult(bestWeight, bestMask, bestDir, bestShift1, bestShift2, id1, id2);
        }

        public static int[][] GetWeightMatrix(Nucleotide[][] map)
        {
            var w = new int[map.Length][];
            for (int i = 0; i < map.Length; i++)
                w[i] = new int[map.Length];
            for (int i = 0; i < map.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    var a = Alignment.Align(map[i], map[j], i, j);
                    w[i][j] = a.Weight;
                    w[j][i] = a.Weight;
                }
            return w;
        }
    }
}
