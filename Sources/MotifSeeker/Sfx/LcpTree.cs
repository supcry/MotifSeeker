using System.Collections.Generic;

namespace MotifSeeker.Sfx
{
    public class LcpTree
    {
        public int Lcp;
        public int LeftBound;
        public int RightBound;
        public List<LcpTree> ChildList;

        public LcpTree(int lcp, int leftBound, int rightBound, List<LcpTree> childList)
        {
            Lcp = lcp;
            LeftBound = leftBound;
            RightBound = rightBound;
            ChildList = childList;
        }

        public int Length()
        {
            return RightBound - LeftBound;
        }


        public static LcpTree NotDefinedInterval(int notDefindedRightBound)
        {
            return new LcpTree(0, 0, notDefindedRightBound, new List<LcpTree>());
        }
    }

    public class Interval
    {
        public  readonly int Left;
        public readonly int Rigth;

        public Interval(int leftBound, int rigthBound)
        {
            Left = leftBound;
            Rigth = rigthBound;
        }


        public override string ToString()
        {
            return Left + " - " + Rigth;
        }

        public override int GetHashCode()
        {
            return (Left + (Rigth << 16));
        }

        public override bool Equals(object obj)
        {
            var o = obj as Interval;
            if (o == null)
                return false;
            return o.Left == Left && o.Rigth == Rigth;

        }
    }

    public class LcpInterval
    {
        public int Left;
        public int Rigth;
        public int Lcp;

        public LcpInterval(Interval interval, int lcp)
        {
            Left = interval.Left;
            Rigth = interval.Rigth;
            Lcp = lcp;
        }

        public override string ToString()
        {
            return Left + " - " + Rigth + " : " + Lcp;
        }
    }

    public class IntervalComparer : IComparer<Interval>
    {
        public int Compare(Interval x, Interval y)
        {
            if (x.Left > y.Left)
                return 1;
            if (x.Left < y.Left)
                return -1;
            return 0;
        }
    }
}
