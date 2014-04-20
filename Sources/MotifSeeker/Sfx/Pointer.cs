using System;
using System.Collections.Generic;

namespace MotifSeeker.Sfx
{
    public class Pointer
    {
        public int LastLeft;
        public int LastRight;

        public int Left;
        public int Right;

        /// <summary>
        /// Длина от корня до вершины. Lcp на заданном интервале.
        /// </summary>
        public int Depth;

        public Pointer()
        { }

        public Pointer(int depth, int left, int right)
        {
            Left = left;
            Right = right;

            Depth = depth;

            LastLeft = left;
            LastRight = right;
        }

        public void Reset(int rigthBound)
        {
            Left = 0;
            Right = rigthBound;
            Depth = 0;

            LastLeft = 0;
            LastRight = rigthBound;
        }

        public override string ToString()
        {
            return Left + " - " + Right + " : " + Depth + " ; " +
                   LastLeft + " - " + LastRight;

        }


    }

    public class SubPointer : IEquatable<SubPointer>
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Length;
        public readonly int SrcId;

        public SubPointer(Pointer pointer, int srcId, int length)
        {
            Left = pointer.Left;
            Right = pointer.Right;
            SrcId = srcId;
            Length = length;
        }

        public bool Equals(SubPointer other)
        {
            return (other.Left == Left && other.Right == Right && other.Length == Length);
        }

        public override bool Equals(object obj)
        {
            var point = obj as SubPointer;
            if (point == null)
                return false;
            return Equals(point);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Left;
                hashCode = (hashCode * 397) ^ Right;
                hashCode = (hashCode * 397) ^ Length;
                hashCode = (hashCode * 397) ^ SrcId;
                return hashCode;
            }
        }
    }


    public class PointerComparer : IComparer<Pointer>
    {
        public int Compare(Pointer x, Pointer y)
        {
            if (x.Left > y.Left) return 1;
            if (x.Left < y.Left) return -1;

            if (x.Right < y.Right) return 1;
            if (x.Right > y.Right) return -1;

            return 0;
        }
    }

    public class SubPointerComparer : IComparer<SubPointer>
    {
        public int Compare(SubPointer x, SubPointer y)
        {
            if (x.Left > y.Left) return 1;
            if (x.Left < y.Left) return -1;

            if (x.Right > y.Right) return 1;
            if (x.Right < y.Right) return -1;

            return 0;
        }
    }
}