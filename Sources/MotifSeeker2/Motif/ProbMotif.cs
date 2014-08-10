using System;
using System.Linq;
using System.Text;
using MotifSeeker2.Dna;
using MotifSeeker2.Helpers;

namespace MotifSeeker2.Motif
{
    /// <summary>
    /// Мотив, оснванный на частотах совпадений первого порядка.
    /// </summary>
    public class ProbMotif : IMotif
    {
        /// <summary>
        /// Для каждой позиции: количества совпадений на ATGC, а также полное их число.
        /// </summary>
        private readonly int[][] _data;

        /// <summary>
        /// Длина мотива.
        /// </summary>
        public int Length { get; private set; }

        private ProbMotifCache _cache;

        public ProbMotif(int len)
        {
            Length = len;
            _data = Enumerable.Repeat(0, 5).Select(p => new int[len]).ToArray();
        }

        /// <summary>
        /// Добавление прецедента в мотив.
        /// </summary>
        public void AddItem(Nucleotide[] nucs, int factor = 1)
        {
            if(nucs == null || nucs.Length != Length)
                throw new ArgumentException("nucs");
            for (int i = 0; i < nucs.Length; i++)
            {
                var nucId = (int) nucs[i];
                if (nucId < 4)
                {
                    var d = _data[i];
                    d[nucId] += factor;
                    d[4] += factor;
                }
                else
                    if (nucId == (int) Nucleotide.Any)
                    _data[i][4] += factor;
                else
                    throw new NotSupportedException();
            }
            _cache = null;
        }

        public float GetScore(Nucleotide[] nucs)
        {
            if (nucs.Length != Length)
                throw new ArgumentException("nucs.Len != " + Length);
            if (_cache == null)
                _cache = new ProbMotifCache(_data);
            var norm = _cache.NormFactor;
            var max = 0.0;
            foreach (var ds in _cache.DataInversions)
            {
                var cur = 1.0;
                for (int i = 0; i < Length; i++)
                {
                    var nicId = (int)nucs[i];
                    var d = ds[i];
                    if (nicId < 4)
                        cur *= d[nicId];
                    else
                        if (nicId == (int) Nucleotide.Any)
                        cur *= 0.1;
                    else
                        throw new NotSupportedException();
                }
                if (cur > max)
                    max = cur;
            }
            return (float)(max*norm);
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("len:" + Length + ", ");
            foreach (var line in _data)
                sb.Append(ElementToString(line));
            return sb.ToString();
        }

        private static string ElementToString(int[] line)
        {
            int val = 0;
            int id = -1;
            int id2 = -1;
            var cnt = line[4];
            if (cnt == 0) // в позиции никого нет и статистика вообще нулевая.
                return "*";
            for (int i = 0; i < 4; i++)
            {
                var v = line[i];
                if (v > val)
                {
                    id2 = id;
                    val = v;
                    id = i;
                }
            }
            if (id == -1) // в позиции никого нет, хотя статистика 
                return "*";
            if (id2 == -1) // строгое совпадение
                return ((Nucleotide) id).ToString();
            if(val*2 > cnt) // абсолютное преобладание
                return ((Nucleotide)id).ToString();
            if(val > line[id2]*2) // абсолютное преобрадание над ближайшим конкурентом
                return ((Nucleotide)id).ToString();
            if (val * 3 > cnt) // абсолютное преобладание
                return ((Nucleotide)id).ToString().ToLower();
            return "?";
        }

        class ProbMotifCache
        {
            /// <summary>
            /// Все четыре комбинаций инверсий от _data.
            /// Актуализируется по мене необходимости.
            /// </summary>
            public readonly int[][][] DataInversions;

            /// <summary>
            /// Коэффициент нормализации.
            /// Актуализируется по мене необходимости.
            /// </summary>
            public readonly float NormFactor = float.NaN;

            private int[] ReverseElement(int[] element)
            {
                var ret = new int[5];
                ret[0] = element[1];
                ret[1] = element[0];
                ret[2] = element[3];
                ret[3] = element[2];
                ret[4] = element[4];
                return ret;
            }

            public ProbMotifCache(int[][] data)
            {
                NormFactor = (float)(1.0 / data.Select(p => (double)p[4]).Prod());
                DataInversions = new int[4][][];
                DataInversions[0] = data;
                DataInversions[1] = data.Reverse().ToArray();
                DataInversions[2] = data.Select(ReverseElement).ToArray();
                DataInversions[1] = DataInversions[2].Reverse().ToArray();
            }
        }
    }
}
