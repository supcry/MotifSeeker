using System;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Построитель суфиксного массива по последовательности DNA или нескольких её цепочек.
    /// </summary>
    public class SfxBuilder
    {
        public SfxArray BuildMany(Nucleotide[][] fragments)
        {
            throw new NotImplementedException();
        }

        public SfxArray BuildOne(Nucleotide[] fragments)
        {
            var tmp = new byte[fragments.Length + 1];
            for (int i = 0; i < fragments.Length; i++)
                tmp[i] = (byte)fragments[i];
            tmp[tmp.Length - 1] = 4;
            var sfx = new SfxArray(tmp);
            return sfx;
        }
    }
}
