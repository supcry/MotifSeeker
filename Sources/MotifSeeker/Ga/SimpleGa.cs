using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MotifSeeker.Data.Dna;
using MotifSeeker.Sfx;

namespace MotifSeeker.Ga
{
    public class SimpleGa
    {
        public class SimpleGaParams
        {
            /// <summary>
            /// Размер популяции.
            /// </summary>
            public int PopSize = 25;

            /// <summary>
            /// Число нуклеотид в особи.
            /// </summary>
            public int SpiceLen = 10;

            /// <summary>
            /// Зерно рендома при старте.
            /// </summary>
            public int Seed = 1;

        }

        private readonly TextComparer _posComparer;
        private readonly TextComparer _negComparer;
        private readonly SimpleGaParams _pars;

        private Spice[] _curPop;

        public Spice BestSpice { get; private set; }

        private readonly Random _rnd;

        private readonly int _pTotal;
        private readonly int _nTotal;

        public bool Finished { get; private set; }

        public SimpleGa(TextComparer posComparer, TextComparer negComparer, SimpleGaParams pars = null)
        {
            _pars = pars ?? new SimpleGaParams();
            _posComparer = posComparer;
            _negComparer = negComparer;
            _rnd = new Random(_pars.Seed);
            _pTotal = _posComparer.GetAllCites(new[] {(byte) Nucleotide.End}, 1).Count();
            _nTotal = _negComparer.GetAllCites(new[] { (byte)Nucleotide.End }, 1).Count();
        }

        private void ReInit()
        {
            _curPop = Enumerable.Repeat(1, _pars.PopSize).Select(p => new Spice(_pars.SpiceLen, _rnd.Next())).ToArray();
        }

        private bool CalcAndCompact()
        {
            //foreach (var spice in _curPop.Where(p => p.Score == null))
            //{
            //    var pos = spice.RawData.Sum(p => _posComparer.GetAllCites(p, p.Length).Length);
            //    var neg = spice.RawData.Sum(p => _negComparer.GetAllCites(p, p.Length).Length);
            //    spice.Score = new Score(pos, neg, _pTotal*100, _nTotal*100);
            //}
            Parallel.ForEach(_curPop.Where(p => p.Score == null),
                new ParallelOptions {MaxDegreeOfParallelism = 8},
                spice =>
                {
                    var pos = spice.RawData.Sum(p => _posComparer.GetAllCites(p, p.Length).Length);
                    var neg = spice.RawData.Sum(p => _negComparer.GetAllCites(p, p.Length).Length);
                    spice.Score = new Score(pos, neg, _pTotal * 10, _nTotal * 10);
                });
            _curPop = _curPop.Distinct().OrderByDescending(p => p).Take(_pars.PopSize).ToArray();
            if (BestSpice == null || _curPop[0].CompareTo(BestSpice) > 0)
            {
                BestSpice = _curPop[0];
                return true;
            }
            return false;
        }

        private void Grow()
        {
            var tot = 1L << (2*_pars.SpiceLen);
            var ng = new List<Spice>(_pars.PopSize);
            var failed = 0;
            while (ng.Count < _pars.PopSize)
            {
                foreach (var i in _rnd.GetShuffleFlow(_curPop.Length))
                {
                    var sp = _curPop[i];
                    if (_rnd.Next(2) == 0)
                    {
                        var spMut = sp.MutateByCount(_rnd.Next(_pars.SpiceLen/2) + 1, _rnd.Next());
                        if (spMut.IsFailed)
                        {
                            failed++;
                            continue;
                        }
                        ng.Add(spMut);
                    }
                    else
                    {
                        var j = i == 0 ? _rnd.Next(_curPop.Length - 1) + 1 : _rnd.Next(i);
                        var sp2 = _curPop[j];
                        var spCrs = Spice.Cross(_rnd.Next(), sp, sp2);
                        if (spCrs.IsFailed)
                        {
                            failed++;
                            continue;
                        }
                        ng.Add(spCrs);
                    }
                }
                for (int i = 0; i < _pars.PopSize / 10; i++)
                {
                    var spR = new Spice(_pars.SpiceLen, _rnd.Next());
                    if (spR.IsFailed)
                    {
                        failed++;
                        continue;
                    }
                    ng.Add(spR);
                }
            }
            if (failed > ng.Count*500)
            {
                Console.WriteLine("Перебрали значительную часть результатов: failed=" + failed + ", population=" + ng.Count);
                Console.WriteLine("Всего возможных комбинаций: =" + tot + ", из них перебрали:" + Spice.HashUniq.Count + ", i.e.: " + Math.Round(100.0*Spice.HashUniq.Count/tot,2) + "%");
                Finished = true;
            }
            

            if(BestSpice != null)
                ng.Add(BestSpice);
            _curPop = ng.ToArray();
        }

        public bool Iter()
        {
            if(_curPop == null)
                ReInit();
            else
                Grow();
            return CalcAndCompact();
        }
    }
}
