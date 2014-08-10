using System;
using System.Linq;
using System.Text;
using MotifSeeker2.Dna;

namespace MotifSeeker2.Motif
{
    /// <summary>
    /// Мотив, оснванный на точных совпадениях.
    /// </summary>
    public class StrictMotif : IMotif
    {
        /// <summary>
        /// Точная последовательность нуклеотид и три её инверсии.
        /// </summary>
        private readonly Nucleotide[][] _data;

        /// <summary>
        /// Длина мотива.
        /// </summary>
        public int Length { get; private set; }

        public StrictMotif(Nucleotide[] data)
        {
            Length = data.Length;
            _data = new Nucleotide[4][];
            _data[0] = data;
            _data[1] = data.Reverse().ToArray();
            _data[2] = data.Select(p => p.Inverse()).ToArray();
            _data[3] = _data[2].Reverse().ToArray();
        }

        public float GetScore(Nucleotide[] nucs)
        {
            if(nucs.Length != Length)
                throw new ArgumentException("nucs.Len != " + Length);
            return _data.Select(ds => ds.Zip(nucs, (a, b) => (a == b || a == Nucleotide.Any) ? 1 : 0).Sum())
                        .Concat(new[] {0})
                        .Max()/(float)Length;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("len:" + Length + ", ");
            foreach (var nuc in _data[0])
                sb.Append(nuc);
            return sb.ToString();
        }
    }
}
