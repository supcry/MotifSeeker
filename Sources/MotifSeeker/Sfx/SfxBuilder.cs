using System;
using System.Collections.Generic;
using System.Linq;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Построитель суфиксного массива по последовательности DNA или нескольких её цепочек.
    /// </summary>
    public class SfxBuilder
    {
        public SfxArray BuildMany(ICollection<Nucleotide[]> fragments)
        {
            var tmp = new byte[fragments.Count + fragments.Sum(p => p.Length)];
            int i = 0;
            foreach (var fragment in fragments)
            {
                foreach (var nucleotide in fragment)
                {
                    tmp[i++] = (byte) nucleotide;
                }
                tmp[i++] = (byte)Nucleotide.End;
            }
            tmp[i - 1]++;
            var sfx = new SfxArray(tmp);
            return sfx;
        }

        public SfxArray BuildOne(Nucleotide[] fragments)
        {
            var tmp = new byte[fragments.Length + 1];
            for (int i = 0; i < fragments.Length; i++)
                tmp[i] = (byte)fragments[i];
            tmp[tmp.Length - 1] = (byte)Nucleotide.End;
            var sfx = new SfxArray(tmp);
            return sfx;
        }
    }
}
