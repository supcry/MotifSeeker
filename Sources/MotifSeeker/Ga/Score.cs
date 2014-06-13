using System;
using System.Diagnostics;

namespace MotifSeeker.Ga
{
    /// <summary>
    /// Результат оценки особи для оценки генетики по типу p,n,P,N.
    /// Если нужно тупое сравнение, то вычисляется точная информативность, которая уже и сравнивается.
    /// </summary>
    public sealed class Score
    {
        private readonly int _p;
        private readonly int _n;

        private readonly int _P;
        private readonly int _N;

        private double? _informativity;

        public double Informativity
        {
            get
            {
                if (!_informativity.HasValue)
                {
                    _informativity = InfoCombi.GetInfo(_p, _n, _P, _N);
                    if (Epsilon > 0.5)
                        _informativity = -Epsilon;
                }
                return _informativity.Value;
            }

        }

        public bool IsVeryGood
        {
            get
            {
                return _p == _P && _n == 0 && _N != 0 && _P != 0;
            }
        }

        public bool IsTrivial
        {
            get
            {
                if (_p == _P && _n == _N)
                    return true;
                return _p == 0 && _n == 0;
            }
        }

        public bool IsInteresting
        {
            get
            {
                if (IsVeryGood)
                    return true;
                if (IsTrivial)
                    return false;
                if (Epsilon < 0.5)
                    return true;
                return false;
            }
        }

        public double Epsilon
        {
            get
            {
                //return _p == 0 && _n == 0 ? 1 : _n/((double) (_p*_N/(double)_P + _n));
                return _p == 0 && _n == 0 ? 1 : _n / ((double)(_p + _n));
            }
        }

        public double Delta
        {
            get
            {
                return _P == 0 && _N == 0 ? 0 : (_n + _p) / (double)(_P + _N);
            }
        }

        public int PositiveCovered { get { return _p; } }

        public int NegativeCovered { get { return _n; } }

        public int PositiveParent { get { return _P; } }

        public int NegativeParent { get { return _N; } }

        public int CoveredCount { get { return _p + _n; } }

        public int ComputedWithSeed { get; private set; }

        private Score() { }

        public static Score Empty { get { return new Score(); } }

        public Score(int p, int n, int tp, int tn)
        {
            Debug.Assert(p <= tp);
            Debug.Assert(n <= tn);
            _n = n;
            _p = p;
            _P = tp;
            _N = tn;
        }

        public int CompareTo(object obj)
        {
            var score = obj as Score;
            if (score == null)
                throw new ArgumentException();
            return CompareTo(score);
        }

        public int CompareTo(Score score)
        {
            var r = Informativity - score.Informativity;
            if (r < 0)
                return -1;
            if (r > 0)
                return 1;
            return 0;
        }

        public override string ToString()
        {
            var ret = "Score," + "pnPN=(" + _p + ", " + _n + ", " + _P + ", " + _N + ")";
            ret = ret + ", Inf=" + Math.Round(Informativity, 4);
            return ret;
        }

        public static Score Failed = new Score(0,0,1,1);

        public bool IsFailed { get { return Equals(Failed); } }
    }
}
