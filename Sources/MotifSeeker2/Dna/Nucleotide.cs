using System;
using System.Linq;

namespace MotifSeeker2.Dna
{
    /// <summary>
    /// Двубитный нуклеотид. Всего четыре значения: ATGC
    /// </summary>
    public enum Nucleotide : byte
    {
        A = 0,
        T = 1,
        G = 2,
        C = 3,
        End = 4,
        Any = 5
    }

    public static class NucleotideExt
    {
        public static Nucleotide ToNucleotide(this byte b)
        {
            return (Nucleotide)b;
        }

        public static byte ToByte(this Nucleotide b)
        {
            return (byte)b;
        }

        public static Nucleotide Inverse(this Nucleotide n)
        {
            switch (n)
            {
                case Nucleotide.A:
                    return Nucleotide.T;
                case Nucleotide.T:
                    return Nucleotide.A;
                case Nucleotide.G:
                    return Nucleotide.C;
                case Nucleotide.C:
                    return Nucleotide.G;
                case Nucleotide.End:
                    return n;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string ChainToString(this Nucleotide[] chain)
        {
            return String.Join("", chain.Select(p => p != Nucleotide.End ? p.ToString().Substring(0, 1) : " "));
        }
    }
}
