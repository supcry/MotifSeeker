//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using MotifSeeker.Data.Dna;
//using MotifSeeker.Sfx;

//namespace MotifSeeker.Motiff
//{
//    public class SpreadMotiff : IMotiff
//    {
//        /// <summary>
//        /// Режим суммирования очков за каждую из сторон.
//        /// </summary>
//        public enum DiffMode
//        {
//            /// <summary>
//            /// Берётся среднее.
//            /// </summary>
//            Avg,

//            /// <summary>
//            /// Берётся максимальное.
//            /// </summary>
//            Max
//        }

//        private readonly ElementGroup[] _elPos;

//        private readonly ElementGroup[] _elNeg;

//        /// <summary>
//        /// На скольких элементарных мотивах построен этот вероятностный мотив.
//        /// </summary>
//        public int Count { get { return _elPos.Sum(p => p.Count) + _elNeg.Sum(p => p.Count); } }

//        public int Length{ get{ return Math.Max(_elPos.Max(p => p.Chain.Length), _elNeg.Max(p => p.Chain.Length)); } }

//        public SpreadMotiff(ElementGroup[] elPos, ElementGroup[] elNeg)
//        {
//            _elPos = elPos;
//            _elNeg = elNeg;
//        }

//        public override string ToString()
//        {
//            return "SpreadMotiff";
//        }

//        public static DiffMode Mode = DiffMode.Avg;


//        public double CalcScore(Nucleotide[] data, int pos, CalcMode? mode)
//        {
            
//            double ret = 0.0;
//            var mo = mode ?? DefaultCalcMode;
//            foreach (var ms in new[] {_posMotiffs, _negMotiffs})
//            {
//                var sum = new List<double>();
//                foreach (var m in ms)
//                {
//                    switch (mo)
//                    {
//                        case CalcMode.Degree:
//                            sum.Add(m.CalcScoreDegree(data, pos));
//                            break;
//                        case CalcMode.Eps:
//                            sum.Add(m.CalcScoreEps(data, pos));
//                            break;
//                        case CalcMode.Strict:
//                            sum.Add(m.CalcScoreStrict(data, pos));
//                            break;
//                        default:
//                            throw new NotSupportedException();
//                    }
//                }
//                var tmp = Mode == DiffMode.Avg ? sum.Average() : sum.Max();
//                if (ms == _posMotiffs)
//                    ret += tmp;
//                else
//                    ret -= tmp;
//            }
//            return (ret + 1.0)/2.0;
//        }

//        public double CalcMaxScore(Nucleotide[] data, CalcMode? mode = null)
//        {
//            var ret = 0.0;
//            foreach (Direction direction in Enum.GetValues(typeof (Direction)))
//            {
//                var b = data.GetChain(direction);
//                for (int i = 0; i < data.Length - Length; i++)
//                {
//                    var tmp = CalcScore(b, i, mode);
//                    if (tmp > ret)
//                        ret = tmp;
//                }
//            }
            
//            return ret;
//        }

//        public double[] CalcMaxScore(Nucleotide[][] data, CalcMode? mode = null)
//        {
//            return data.Select(p => CalcMaxScore(p, mode)).ToArray();
//        }

//        public double CalcMaxScore(Nucleotide[] data, int startPos, int endPos, CalcMode? mode = null)
//        {
//            Debug.Assert(startPos > 0);
//            var ret = 0.0;
//            var end = Math.Min(data.Length - Length, endPos);
//            for (int i = startPos; i < end; i++)
//            {
//                var tmp = CalcScore(data, i, mode);
//                if (tmp > ret)
//                    ret = tmp;
//            }
//            return ret;
//        }
//    }
//}
