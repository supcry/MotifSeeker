using System;
using System.Linq;

namespace MotifSeeker2.Dna
{
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

    public static class DirectionExt
    {
        /// <summary>
        /// Переупорядочивание последовательности нуклеотид в зависимости от направления движения.
        /// </summary>
        public static Nucleotide[] Redirect(this Nucleotide[] ns, Direction d)
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
    }
}
