using System;
using System.CodeDom;
using System.Linq;

namespace MotifSeeker.Data.Dna
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
        All = 5,
// ReSharper disable InconsistentNaming
        a = 10,
        t = 11,
        g = 12,
        c = 13,
// ReSharper restore InconsistentNaming
	}

    /// <summary>
    /// Направление движения.
    /// </summary>
    public enum Direction : byte
    {
        /// <summary>
        /// Обычный порядок.
        /// </summary>
        Straight,

        /// <summary>
        /// С конца в начало.
        /// </summary>
        Backward,

        /// <summary>
        /// Замена T-A, G-C.
        /// </summary>
        Inverted,

        /// <summary>
        /// Замена T-A, G-C и из конца в начало
        /// </summary>
        BackwardInverted

    }

    public static class NucleotideExt
    {
        public static Nucleotide ToNucleotide(this byte b)
        {
            return (Nucleotide) b;
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
                default:
                    throw new NotSupportedException();
            }
        }

        public static Nucleotide[] GetChain(this Nucleotide[] ns, Direction d)
        {
            switch (d)
            {
                case Direction.Straight:
                    return ns;
                case Direction.Backward:
                    return ns.Reverse().ToArray();
                case Direction.Inverted:
                    return ns.Select(p => p.Inverse()).ToArray();
                case Direction.BackwardInverted:
                    return ns.Reverse().Select(p => p.Inverse()).ToArray();
                default:
                    throw new NotSupportedException();
            }
        }

        public static string ChainToString(this Nucleotide[] chain)
        {
            return String.Join("", chain.Select(p => p!= Nucleotide.All ? p.ToString().Substring(0, 1) : " "));
        }
    }
}
