using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using MotifSeeker;
using MotifSeeker.Data.Dna;
using MotifSeeker.Data.DNaseI;
using MotifSeeker.Ga;
using MotifSeeker.Motiff;
using MotifSeeker.Sfx;
using ZedGraph;

namespace Sandbox
{
	class Program
    {
        #region regions
        public class GetRegionParams
	    {
	        public ChromosomeEnum? Chr = ChromosomeEnum.Chr1;
	        public int MaxCellsCount = 10;
            public int MinCellsPerRegion = 2;
	        public int MinAverageValue1 = 200;
	        public int MinSizeOfRegion = 100;
	        public bool RandomizePosOfNoise = true;
            public int RandomizePosOfNoiseSeed = 1;
	    }

	    public class Region
	    {
            public readonly ChromosomeEnum Chr;

	        public readonly int Start;

            public readonly int Size;

            public int End { get { return Start + Size; } }

            public Region() { }

	        public Region(MergedNarrowPeak peak)
	        {
	            Chr = peak.Chr;
	            Start = peak.StartPosMin;
	            Size = peak.EndPosMax - Start;
	        }

	        public Region(ChromosomeEnum chr, int start, int size)
	        {
	            Chr = chr;
	            Start = start;
	            Size = size;
	        }

	        public override string ToString()
	        {
	            return Chr + ",pos:" + Start + ",size:" + Size;
	        }
	    }

        /// <summary>
        /// Для хромосом полученных из разных типов клеток объединяет карту акнтивности ДНКазы, и возвращает две группы цепочек ДНК суммарной равно длины - с обнаруженной активностью и без оной.
        /// </summary>
        private static void GetRegions(GetRegionParams pars, out Region[] peaks, out Region[] noises)
	    {
            var t = DateTime.Now;
            var flow = NarrowPeaksMerger.GetMergedNarrowPeaks(pars.Chr, pars.MaxCellsCount).ToArray();
            //MergedBarGraph.DrawPeaks(flow, 5, 0, 2000000);
            var peakRegions = new List<Region>();
            var noiseRegions = new List<Region>();

            int lastPos = 0;
            int totalPeaksLen = 0;
            int totalNonpeaksLen = 0;
            int totalNonpeaksLenWoCut = 0;
            var lastChr = flow.First().Chr;
            var rnd = new Random(pars.RandomizePosOfNoiseSeed);

            var empyQ = new Queue<int>();


            foreach (var peak in flow)
            {
                // если пошли данные с другой хромосомы, то сбросим позицию
                if (lastChr != peak.Chr) 
                {
                    Debug.Assert(lastChr < peak.Chr);
                    lastPos = 0;
                    lastChr = peak.Chr;
                }
                // добавим регион без пиков, если он есть (если есть пустое пространство между последним и текущим регионом
                if (peak.StartPosMin - lastPos > 0)
                {
                    totalNonpeaksLenWoCut += peak.StartPosMin - lastPos;

                    while (true)
                    {
                        var size = peak.StartPosMin - lastPos;

                        if (size > pars.MinSizeOfRegion && empyQ.Count > 0)
                        {
                            var sizeQ = empyQ.Peek();
                            if (sizeQ < size)
                                size = empyQ.Dequeue();
                            else
                                break;
                            Region nr;
                            if (pars.RandomizePosOfNoise)
                            {
                                var startPos = rnd.Next(lastPos, peak.StartPosMin - size);
                                nr = new Region(lastChr, startPos, size);
                            }
                            else
                                nr = new Region(lastChr, lastPos, size);
                            totalNonpeaksLen += nr.Size;
                            noiseRegions.Add(nr);
                            Debug.Assert(nr.Start >= lastPos);
                            Debug.Assert(nr.Start + nr.Size <= peak.StartPosMin);
                            lastPos = nr.Start + nr.Size;
                        }
                        else
                            break;
                    }
                }
                Debug.Assert(lastPos <= peak.EndPosMax + pars.MinSizeOfRegion);
                lastPos = peak.EndPosMax;
                // определим качество региона
                if (peak.Count < pars.MinCellsPerRegion ||
                    peak.AvgValue1 < pars.MinAverageValue1 ||
                    peak.Size < pars.MinSizeOfRegion)
                    continue;
                Debug.Assert(peak.StartPos >= 0);
                Debug.Assert(peak.EndPos >= 0);
                var pr = new Region(peak);
                peakRegions.Add(pr);
                totalPeaksLen += pr.Size;
                empyQ.Enqueue(pr.Size);
            }

            Console.WriteLine("Expirement data merged, dt=" + (DateTime.Now - t));
            Console.WriteLine("Peaks: TotalLen=" + totalPeaksLen + ", Count=" + peakRegions.Count);
            Console.WriteLine("Noise: TotalLen=" + totalNonpeaksLen + ", Count=" + noiseRegions.Count + ", FullLen=" + totalNonpeaksLenWoCut);
            peaks = peakRegions.ToArray();
            noises = noiseRegions.ToArray();
	    }
        #endregion regions

        #region BuildSfx

        public class GetSfxParams
        {
            /// <summary>
            /// Минимальный размер слитых цепочек (в нуклеотидах).
            /// </summary>
            public int MinGroupSize = 10;
        }

	    private static void GetSfx(GetSfxParams pars, IEnumerable<Region> peaks, IEnumerable<Region> noises, 
                                   out TextComparer posComp, out TextComparer negComp)
	    {
	        var t = DateTime.Now;
	        var chrDic = new Dictionary<ChromosomeEnum, Chromosome>();

	        // peaks
	        var peaksTmp = peaks.Select(p =>
	        {
	            if (!chrDic.ContainsKey(p.Chr))
	                chrDic.Add(p.Chr, ChrManager.GetChromosome(p.Chr));
	            return chrDic[p.Chr].GetPack(p.Start, p.Size);
	        }).ToArray();
	        TextComparer sfxPeaks = SuffixBuilder.BuildMany2(peaksTmp, pars.MinGroupSize);
	        Console.WriteLine("Peaks sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxPeaks.StrokeSize);
	        // noise
	        var noiseTmp = noises.Select(p =>
	        {
	            if (!chrDic.ContainsKey(p.Chr))
	                chrDic.Add(p.Chr, ChrManager.GetChromosome(p.Chr));
	            return chrDic[p.Chr].GetPack(p.Start, p.Size);
	        }).ToArray();
	        TextComparer sfxNoise = SuffixBuilder.BuildMany2(noiseTmp, pars.MinGroupSize);
	        Console.WriteLine("Noise sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxNoise.StrokeSize);
	        posComp = sfxPeaks;
	        negComp = sfxNoise;
	    }

	    #endregion BuildSfx

        #region  GetCandidateElements
        public class GetCandidateElementsParams
	    {
            /// <summary>
            /// Делать ли и выводить ли на консоль результаты кросс-теста?
            /// </summary>
	        public bool PrintCross = false;

            /// <summary>
            /// Минимальный размер слитых цепочек (в нуклеотидах).
            /// </summary>
	        public int MinGroupSize = 10;

            /// <summary>
            /// Отбросить те элементы, которые встречаются одновременно и в пиках и в шумах.
            /// </summary>
            public bool DropCross = true;

	    }

        /// <summary>
        /// Для регионов активности и неактивности строит по своему суф.массиву и в результате получает слитые цепочки.
        /// </summary>
        private static void GetCandidateElements(GetCandidateElementsParams pars, IEnumerable<Region> peaks, IEnumerable<Region> noises,
	                                    out ElementGroup[] elPeaks, out ElementGroup[] elNoise)
	    {
            var t = DateTime.Now;
            var chrDic = new Dictionary<ChromosomeEnum, Chromosome>();

            // peaks
            var peaksTmp = peaks.Select(p =>
            {
                if (!chrDic.ContainsKey(p.Chr))
                    chrDic.Add(p.Chr, ChrManager.GetChromosome(p.Chr));
                return chrDic[p.Chr].GetPack(p.Start, p.Size);
            }).ToArray();
            TextComparer sfxPeaks = SuffixBuilder.BuildMany2(peaksTmp, pars.MinGroupSize);
            elPeaks = sfxPeaks.GetElementGroups().ToArray();
            Console.WriteLine("Peaks sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxPeaks.StrokeSize +
                              ", elCnt=" + elPeaks.Length + ", elTotal=" + elPeaks.Sum(p => p.Count));
            // noise
            var noiseTmp = noises.Select(p =>
            {
                if (!chrDic.ContainsKey(p.Chr))
                    chrDic.Add(p.Chr, ChrManager.GetChromosome(p.Chr));
                return chrDic[p.Chr].GetPack(p.Start, p.Size);
            }).ToArray();
            TextComparer sfxNoise = SuffixBuilder.BuildMany2(noiseTmp, pars.MinGroupSize);
            elNoise = sfxNoise.GetElementGroups().ToArray();
            Console.WriteLine("Noise sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxNoise.StrokeSize +
                              ", elCnt=" + elNoise.Length + ", elTotal=" + elNoise.Sum(p => p.Count));
            Array.Sort(elPeaks);
            Array.Sort(elNoise);
            if (pars.PrintCross)
            {
                Console.WriteLine("TransTest (peaks on peaks)");
                TransTest(sfxPeaks.StrokeSize, elPeaks.Take(10), sfxPeaks);
                Console.WriteLine("TransTest (peaks on empty)");
                TransTest(sfxPeaks.StrokeSize, elPeaks.Take(10), sfxNoise);
                Console.WriteLine("TransTest (empty on peaks)");
                TransTest(sfxNoise.StrokeSize, elNoise.Take(10), sfxPeaks);
            }
            if (pars.DropCross)
            {
                var pc = elPeaks.Length;
                var nc = elNoise.Length;
                Console.WriteLine("Drop cross elements:");
                //elPeaks = elPeaks.Where(p => sfxNoise.GetAllCites(p.Chain, p.Chain.Length).Count() < p.Count/2).ToArray();
                //Console.WriteLine("\tpeaks - was:" + pc + ", now:" + elPeaks.Length + ", dropped=" + (pc - elPeaks.Length));
                elNoise = elNoise.Where(p => sfxPeaks.GetAllCites(p.Chain, p.Chain.Length).Count() < /*p.Count / 2*/1).ToArray();
                Console.WriteLine("\tnoise - was:" + nc + ", now:" + elNoise.Length + ", dropped=" + (nc - elNoise.Length));
            }
	    }
        #endregion  GetCandidateElements

        #region GetMotiffs

	    public class GetMotiffsParams
	    {
	        public int CutWeight = 6;

	        public int Seed = 100;

	        public bool SetCoreWeights = false;

	        public bool CrossTest = true;

	        public int SkipClusterSize = 16;
	    }

	    public static void GetMotiffs(GetMotiffsParams pars, ElementGroup[] elPeaks, ElementGroup[] elNoise, out Motiff[] mfPeaks, out Motiff[] mfNoise)
	    {
            var peakClustering = new Clustering(elPeaks);
            var emptyClustering = new Clustering(elNoise);

            var peakClusters = peakClustering.Work3(pars.CutWeight, pars.Seed, pars.SetCoreWeights);
            var emptyClusters = emptyClustering.Work3(pars.CutWeight, pars.Seed, pars.SetCoreWeights);

            var peakClusters2 = peakClusters.Where(p => p.TotalCount > pars.SkipClusterSize).Select(p => p.Align()).ToArray();
            var emptyClusters2 = emptyClusters.Where(p => p.TotalCount > pars.SkipClusterSize).Select(p => p.Align()).ToArray();

            var peakMotifs = peakClusters2.Select(p => Motiff.ExtractMotiff(p.Map, p.MapFactors)).ToArray();
            var emptyMotifs = emptyClusters2.Select(p => Motiff.ExtractMotiff(p.Map, p.MapFactors)).ToArray();

	        if (pars.CrossTest)
	        {
	            TransMotiffTest(peakMotifs, peakClusters2, "peaks vs peaks");
	            TransMotiffTest(peakMotifs, emptyClusters2, "peaks vs empty");

	            TransMotiffTest(emptyMotifs, emptyClusters2, "empty vs empty");
	            TransMotiffTest(emptyMotifs, peakClusters2, "empty vs peaks");
	        }

            mfPeaks = peakMotifs;
	        mfNoise = emptyMotifs;
	    }
        #endregion GetMotiffs

        #region GetROC

	    public class GetRocParams
	    {
	        public int DeltaCounts = 10000;
	    }

        /// <summary>
        /// Данные для ROC-кривой от мотива.
        /// </summary>
	    public class RocCurve
	    {
            /// <summary>
            /// Имя графика (мотива).
            /// </summary>
	        public string Name;

            /// <summary>
            /// Размер точки на графике.
            /// </summary>
            public double PointSize { get { return Math.Log(Count); } }

            /// <summary>
            /// Число прецендентов, на которых строится мотив.
            /// </summary>
	        public int Count;

            /// <summary>
            /// Значение по горизонтальной координате.
            /// </summary>
            public double[] X;

            /// <summary>
            /// Значения по вертикальной координате.
            /// </summary>
            public double[] Y;

            /// <summary>
            /// Доля площади под графиком (1 - весь график, 0 - ничего от графика, 0.5 - половина графика (худшее значение)
            /// </summary>
	        public double Area;

            public IMotiff Motiff;

	        public RocCurve(string name, int count, double[] x, double[] y, IMotiff mf)
	        {
	            Name = name;
	            Count = count;
	            X = x;
	            Y = y;
                Area = CalcArea(x.Reverse().ToArray(), y.Reverse().ToArray());
	            Motiff = mf;
	        }
	    }

	    private static double CalcArea(double[] x, double[] y)
	    {
            double lastX = 0;
            double lastY = 0;
            double area = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if (Math.Abs(lastX - x[i]) < 0.000001 && i == 0)
                    continue;
                var dx = x[i] - lastX;
                var dy = y[i] - lastY;
                var yy = lastY;
                var area1 = yy * dx + dy * dx / 2.0;
                area += area1;
                lastX = x[i];
                lastY = y[i];
            }
            if (lastX < 100.0)
            {
                var dx = 100.0 - lastX;
                var dy = 100.0 - lastY;
                var yy = lastY;
                var area1 = yy * dx + dy * dx / 2.0;
                area += area1;
            }
            return area / 10000.0;
	    }

        public static RocCurve GetROC(GetRocParams pars, Region[] rgPeaks, Region[] rgNoises, IMotiff motiff)
	    {
            var chrDic = new Dictionary<ChromosomeEnum, Chromosome>();
            // подготовим данные для пиков
            var vsPeak = new double[rgPeaks.Length];
            for (int i = 0; i < rgPeaks.Length; i++)
            {
                var rgPeak = rgPeaks[i];
                Chromosome c;
                if (!chrDic.TryGetValue(rgPeak.Chr, out c))
                    chrDic.Add(rgPeak.Chr, c = ChrManager.GetChromosome(rgPeak.Chr));
                var pack = c.GetPack(rgPeak.Start, rgPeak.Size);
                vsPeak[i] = motiff.CalcMaxScore(pack);
            }
            Array.Sort(vsPeak);
            int vsPeakId = 0;
            // подготовим данные для шума
            var vsNoise = new double[rgNoises.Length];
            for (int i = 0; i < rgNoises.Length; i++)
            {
                var rgNoise = rgNoises[i];
                Chromosome c;
                if (!chrDic.TryGetValue(rgNoise.Chr, out c))
                    chrDic.Add(rgNoise.Chr, c = ChrManager.GetChromosome(rgNoise.Chr));
                var pack = c.GetPack(rgNoise.Start, rgNoise.Size);
                vsNoise[i] = motiff.CalcMaxScore(pack);
            }
            Array.Sort(vsNoise);
            int vsNoiseId = 0;
            // начинаем строить график
            var x = new List<double>();
            var y = new List<double>();

            x.Add(100);
            y.Add(100);

            for (double thr = 0.0; thr <= 1.0; thr += 1.0/pars.DeltaCounts)
            {
                while (vsPeakId < vsPeak.Length && vsPeak[vsPeakId] < thr)
                    vsPeakId++;
                while (vsNoiseId < vsNoise.Length && vsNoise[vsNoiseId] < thr)
                    vsNoiseId++;
                var sensitivity = 100.0*(vsPeak.Length - vsPeakId)/vsPeak.Length;
                var specifity = 100.0 * (vsNoise.Length - vsNoiseId) / vsNoise.Length;

                y.Add(sensitivity);
                x.Add(specifity);
            }
            x.Add(0);
            y.Add(0);
            return new RocCurve(motiff.ToString(), motiff.Count, x.ToArray(), y.ToArray(), motiff);
	    }

	    private static bool _drawInited;
        static void DrawROC(RocCurve[] pos, RocCurve[] neg, RocCurve[] spc = null)
        {
            #region head

            if (!_drawInited)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                _drawInited = true;
            }
            var form = new Form { Width = 800, Height = 800 };

            var f = new ZedGraphControl
            {
                Anchor = AnchorStyles.Top,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Name = "ROC-кривая для мотивов Chr1",
            };
            

            var yaxis = f.GraphPane.YAxis;
            yaxis.Type = AxisType.Linear;
            yaxis.Scale.MaxAuto = false;
            yaxis.Scale.MinAuto = false;
            yaxis.Scale.Max = 100;
            yaxis.Scale.Min = 0;
            yaxis.Title = new AxisLabel("yyy ааа", "times", 12, Color.Black, false, false, false);

            var xaxis = f.GraphPane.XAxis;
            xaxis.Type = AxisType.Linear;
            xaxis.Scale.MaxAuto = false;
            xaxis.Scale.MinAuto = false;
            xaxis.Scale.Max = 100;
            xaxis.Scale.Min = 0;
            xaxis.Title = new AxisLabel("xxx БББ", "roman", 14, Color.Black, true, false, false);

            f.GraphPane.BarSettings.Type = BarType.SortedOverlay;
            f.GraphPane.BarSettings.MinBarGap = 1000;
            #endregion head

            f.GraphPane.AddCurve("center", new double[] { 0, 100 }, new double[] { 0, 100 }, Color.Red);
            Console.WriteLine("ROC info:");
            Console.WriteLine("positive:");
            for (int i = 0; i < pos.Length; i++)
            {
                var item = pos[i];
                f.GraphPane.AddCurve(item.Name, item.X, item.Y,
                    Color.FromArgb(0, (int) Math.Round(255.0*(1.0 - i/(double) pos.Length)), 0));
                Console.WriteLine(item.Name + ", cnt:" + item.Count + ", area=" + item.Area.ToString("F4"));
            }
            Console.WriteLine("negative:");
            for (int i = 0; i < neg.Length; i++)
            {
                var item = neg[i];

                f.GraphPane.AddCurve(item.Name, item.X, item.Y,
                    Color.FromArgb(0, 0, (int) Math.Round(255.0*(1.0 - i/(double) neg.Length))));
                Console.WriteLine(item.Name + ", cnt:" + item.Count + ", area=" + item.Area.ToString("F4"));
            }
            if (spc != null)
            {
                Console.WriteLine("custom:");
                for (int i = 0; i < spc.Length; i++)
                {
                    var item = spc[i];
                    f.GraphPane.AddCurve(item.Name, item.X, item.Y,
                        Color.FromArgb((int) Math.Round(255.0*(1.0 - i/(double) spc.Length)), 0, 0));
                    Console.WriteLine(item.Name + ", cnt:" + item.Count + ", area=" + item.Area.ToString("F4"));
                }
            }
            form.Controls.Add(f);
            f.AxisChange();
            f.Invalidate();
            Application.Run(form);
            
        }

        #endregion GetROC

        static void MainPlan()
	    {
	        // План:
	        //  1. получить последовательности участков с пиками и без пиков
            Region[] peaksPos;
            Region[] noisePos;
            GetRegions(new GetRegionParams(), out peaksPos, out noisePos);

	        //  2. построить суффиксы по участкам с пиками и без пиков, выделить склеенные цепочки
	        //     удостовериться, что участки в обоих суф.структурах находятся.
	        ElementGroup[] elPeaks;
	        ElementGroup[] elNoise;
	        GetCandidateElements(new GetCandidateElementsParams{MinGroupSize = 10}, peaksPos, noisePos, out elPeaks, out elNoise);

            //  3. кластеризовать элементы 
	        Motiff[] mfPeaks;
            Motiff[] mfNoise;
            GetMotiffs(new GetMotiffsParams(), elPeaks, elNoise, out mfPeaks, out mfNoise);

            var lineGgc = new []{Nucleotide.T, Nucleotide.G, Nucleotide.G, Nucleotide.C,Nucleotide.T, Nucleotide.G, Nucleotide.G, Nucleotide.C,Nucleotide.T};
            var lineGcc = new []{Nucleotide.T, Nucleotide.G, Nucleotide.C, Nucleotide.C,Nucleotide.T, Nucleotide.G, Nucleotide.C, Nucleotide.C,Nucleotide.T};
            var linecGc = new []{Nucleotide.T, Nucleotide.C, Nucleotide.G, Nucleotide.C,Nucleotide.T, Nucleotide.C, Nucleotide.G, Nucleotide.C,Nucleotide.T};
            var lineGgg = new[] { Nucleotide.T, Nucleotide.G, Nucleotide.G, Nucleotide.G, Nucleotide.T, Nucleotide.G, Nucleotide.G, Nucleotide.G, Nucleotide.T};
            var lineGgcGcc = new[] { Nucleotide.T, Nucleotide.G, Nucleotide.G, Nucleotide.C, Nucleotide.T, Nucleotide.G, Nucleotide.C, Nucleotide.C, Nucleotide.T};

            var lineGgCc = new[] { Nucleotide.G, Nucleotide.G, Nucleotide.C, Nucleotide.C, Nucleotide.G, Nucleotide.G, Nucleotide.C, Nucleotide.C, Nucleotide.G, Nucleotide.G };

            var lineGcCg = new[] { Nucleotide.G, Nucleotide.C, Nucleotide.C, Nucleotide.G, Nucleotide.G, Nucleotide.C, Nucleotide.C, Nucleotide.G, Nucleotide.G, Nucleotide.C};

            Motiff[] mfSpec = 
            {
                //Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 100, 100, 100}),

                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {1000, 100, 100, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 1000, 100, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 100, 1000, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 100, 100, 1000}),

                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {1000, 1000, 100, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 1000, 1000, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 100, 1000, 1000}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {1000, 100, 1000, 100}),
                ////Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 1000, 100, 1000}),

                //Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg, lineGgcGcc}, new[] {100, 100, 100, 100, 1000}),
                //Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {1000, 100, 100, 100}),
                Motiff.ExtractMotiff(new[] {lineGgc, lineGcc, linecGc, lineGgg}, new[] {100, 1000, 100, 100}),

                //Motiff.ExtractMotiff(new[] {lineGgCc}, new[] {1}),
                //Motiff.ExtractMotiff(new[] {lineGcCg}, new[] {1}),
                
                Motiff.ExtractMotiff(new[] {lineGcCg, lineGgCc}, new[] {1,1})
            };

            var rocPeaks = mfPeaks.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray();
            var rocNoise = mfNoise.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray();
            var rocSpec = mfSpec.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray();

            //  4. Построить ROC-кривую по обучающей хромосоме
            DrawROC(rocPeaks,rocNoise,rocSpec);

            // 5. Построить ROC-кривую по подсмотренным данным

            var mfDiffAll = new DiffMotiff(mfPeaks, mfNoise);
            var rocFlow = rocPeaks.Concat(rocNoise)
                                  .Concat(rocSpec)
                                  .ToArray();
            var mfDiffBest = new DiffMotiff(rocFlow.OrderByDescending(p => p.Area).Take(2).Select(p => (Motiff)p.Motiff).ToArray(),
                                            rocFlow.OrderBy(p => p.Area).Take(2).Select(p => (Motiff)p.Motiff).ToArray());

            var rocDiffAll = new[] { mfDiffAll }.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray();
            var rocDiffBest = new[] { mfDiffBest }.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray();

            DrawROC(rocDiffAll, rocDiffBest);

            Console.WriteLine("fin");
	        Console.ReadKey();
	    }

        static void GaPlan()
        {
            // План:
            //  1. получить последовательности участков с пиками и без пиков
            Region[] peaksPos;
            Region[] noisePos;
            GetRegions(new GetRegionParams(), out peaksPos, out noisePos);

            //  2. построить суффиксы по участкам с пиками и без пиков, выделить склеенные цепочки
            //     удостовериться, что участки в обоих суф.структурах находятся.
            TextComparer posComparer;
            TextComparer negComparer;
            GetSfx(new GetSfxParams(), peaksPos, noisePos, out posComparer, out negComparer);

            var ga = new SimpleGa(posComparer, negComparer, new SimpleGa.SimpleGaParams{PopSize = 300, SpiceLen = 9});
            while (!ga.Finished)
            {
                var r = ga.Iter();
                if (r && ga.BestSpice.Score.IsInteresting)
                {
                    Console.WriteLine("spice: " + ga.BestSpice + ", score=" + ga.BestSpice.Score);
                }
            }

            Console.WriteLine("fin");
            Console.ReadKey();
        }

	    static void TransMotiffTest(IEnumerable<Motiff> ms, MultiAlignmentResult[] rs, string name)
	    {
            Console.WriteLine("TransMotiffTest(" + name + ")");
	        foreach (var m in ms)
	        {
                Console.WriteLine("  m: " + m + ", cnt=" + m.Count);
	            foreach (var r in rs)
	            {
	                var fs = m.CalcMaxScore(r.Map);
                    Console.WriteLine("    r: " + r.MaskStr.Trim() + " factor: " + fs.MinMeanMaxStr(4, true));
	            }
	        }
	    }

		private static void TransTest(int origLen, IEnumerable<ElementGroup> groups, TextComparer cmp)
		{
			var cntOrig = 0;
			var cntTrans = 0;
			foreach (var gr in groups)
			{
				var cnt = cmp.GetAllCites(gr.Chain, gr.Chain.Length);
				Console.WriteLine(gr + ", otherCnt=" + cnt.Length);
				cntOrig += gr.Count;
				cntTrans += cnt.Length;
			}
			Console.WriteLine("origFactor=" + cntOrig + ", transFactor=" + Math.Round((double)origLen * cntTrans / cmp.StrokeSize, 3));
		}

        [STAThreadAttribute]
		static void Main()
        {
            // GGCCGGCCGGC
            // CGGCCGGCCGG
            // p = 406, n = 218, inf=33
            var inf1 = new Score(406, 218, 1024, 1024).Informativity;
            GaPlan();
		    //MainPlan();
			Console.WriteLine("Ok\nPress any key to exit");
			Console.ReadKey();
		}


        //#region trash
        //static void CheckChr1()
        //{
        //    var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
        //    Debug.Assert(chr != null);
        //}

        //static void GetExpDataForCsv()
        //{
        //    var pars1 = new Dictionary<string, string> { { "type", "broadPeak" }, { "cell", "A549" }, { "replicate", "1" } };
        //    var exp1 = DNaseIManager.GetSensitivityResults(pars1, ChromosomeEnum.Chr1);
        //    using (var f1 = File.CreateText("broadPeak.srt1.csv"))
        //    {
        //        foreach (var item in exp1.Items.OrderBy(p => -p.Value1))
        //            f1.WriteLine(item.Value1 + "; " + item.Value2);
        //        f1.Flush();
        //    }
        //    using (var f2 = File.CreateText("broadPeak.srt2.csv"))
        //    {
        //        foreach (var item in exp1.Items.OrderBy(p => -p.Value2))
        //            f2.WriteLine(item.Value2 + "; " + item.Value1);
        //        f2.Flush();
        //    }
        //}

        //static void GetClassifiedExpData()
        //{
        //    var pars1 = new Dictionary<string, string> { { "type", "broadPeak" }, { "cell", "A549" }, { "replicate", "1" } };
        //    var exp1 = DNaseIManager.GetClassifiedRegions(ChromosomeEnum.Chr1, pars1, true);
        //    Debug.Assert(exp1.Count == 3);
        //    var statP = exp1[ClassifiedRegion.MotifContainsStatus.Present].Select(p => p.RawValue2).MinMeanMax();
        //    var statN = exp1[ClassifiedRegion.MotifContainsStatus.NotPresent].Select(p => p.RawValue2).MinMeanMax();
        //    var statU = exp1[ClassifiedRegion.MotifContainsStatus.Unknown].Select(p => p.RawValue2).MinMeanMax();
        //    Debug.Assert(exp1.Count == 3);
        //}

        //static void DrawForm()
        //{

        //    // * Вывести на экран простенькую форму с графиком
        //    // * Научиться выводить график нужного вида + научиться экспортировать в eps.
        //    // * Автоматизировать процесс, вывести вывод графиков в отдельную библиотеку
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    var form = new Form { Width = 800, Height = 600 };

        //    var f = new ZedGraphControl
        //    {
        //        Anchor = AnchorStyles.Top,
        //        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        //        Dock = DockStyle.Fill
        //    };

        //    var yaxis = f.GraphPane.YAxis;
        //    yaxis.Type = AxisType.Log;
        //    yaxis.Scale.MaxAuto = false;
        //    yaxis.Scale.MinAuto = false;
        //    yaxis.Scale.Max = 1e+3;
        //    yaxis.Scale.Min = 1e-1;

        //    var xaxis = f.GraphPane.XAxis;
        //    xaxis.Scale.MaxAuto = false;
        //    xaxis.Scale.MinAuto = false;
        //    xaxis.Scale.Max = 110;
        //    xaxis.Scale.Min = -10;
        //    xaxis.Type = AxisType.LinearAsOrdinal;

        //    var grow = Enumerable.Range(1, 10).Select(p => (double)p * p).ToArray();
        //    var grow2 = Enumerable.Range(1, 10).Select(p => (double)p * p / 2.0).ToArray();
        //    var grow3 = Enumerable.Range(1, 10).Select(p => (double)p * p / 4.0).ToArray();

        //    var growx = Enumerable.Range(1, 10).Select(p => (double)p * p + 0.5).ToArray();

        //    f.GraphPane.BarSettings.Type = BarType.SortedOverlay;
        //    f.GraphPane.BarSettings.MinBarGap = 1000;

        //    var bar1 = f.GraphPane.AddBar("bar", grow, grow, Color.Blue);
        //    var bar2 = f.GraphPane.AddBar("bar2", grow, grow2, Color.Red);
        //    var bar3 = f.GraphPane.AddBar("bar3", growx, grow3, Color.Green);

        //    bar1.Bar.Fill.Type = FillType.Brush;
        //    bar1.Bar.Border.IsVisible = false;
        //    bar2.Bar.Fill.Type = FillType.Solid;
        //    bar2.Bar.Border.IsVisible = false;
        //    bar3.Bar.Fill.Type = FillType.GradientByY;
        //    bar3.Bar.Border.IsVisible = false;

        //    form.Controls.Add(f);

        //    f.AxisChange();
        //    f.Invalidate();

        //    Application.Run(form);
        //}

        
        //#endregion trash
    }
}
