using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MotifSeeker;
using MotifSeeker.Data.Dna;
using MotifSeeker.Data.DNaseI;
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
	        public int MaxSizeOfRegion = 10000;
	        public bool RandomizePosOfNoise = true;
            public int RandomizePosOfNoiseSeed = 10000;
	    }

	    public class Region
	    {
	        public ChromosomeEnum Chr;

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
	        
	    }

        public static KeyValuePair<double[], double[]> GetROC(GetRocParams pars, Region[] rgPeaks, Region[] rgNoises, Motiff motiff, int cnt = 10000)
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

            for (double thr = 0.0; thr <= 1.0; thr += 1.0/cnt)
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
            return new KeyValuePair<double[], double[]>(x.ToArray(), y.ToArray());
	    }

	    private static bool drawInited;
        static void DrawROC(KeyValuePair<double[], double[]>[] pos, KeyValuePair<double[], double[]>[] neg)
        {
            #region head

            if (!drawInited)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                drawInited = true;
            }
            var form = new Form { Width = 800, Height = 800 };

            var f = new ZedGraphControl
            {
                Anchor = AnchorStyles.Top,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };

            var yaxis = f.GraphPane.YAxis;
            yaxis.Type = AxisType.Linear;
            yaxis.Scale.MaxAuto = false;
            yaxis.Scale.MinAuto = false;
            yaxis.Scale.Max = 100;
            yaxis.Scale.Min = 0;

            var xaxis = f.GraphPane.XAxis;
            xaxis.Type = AxisType.Linear;
            xaxis.Scale.MaxAuto = false;
            xaxis.Scale.MinAuto = false;
            xaxis.Scale.Max = 100;
            xaxis.Scale.Min = 0;

            f.GraphPane.BarSettings.Type = BarType.SortedOverlay;
            f.GraphPane.BarSettings.MinBarGap = 1000;
            #endregion head

            f.GraphPane.AddCurve("center", new double[] { 0, 100 }, new double[] { 0, 100 }, Color.Red);
            for (int i = 0; i < pos.Length; i++)
                f.GraphPane.AddCurve("p" + i, pos[i].Key, pos[i].Value, Color.FromArgb(0, (int)Math.Round(255.0*(1.0-i/(double)pos.Length)),0));
            for (int i = 0; i < neg.Length; i++)
                f.GraphPane.AddCurve("n" + i, neg[i].Key, neg[i].Value, Color.FromArgb(0, 0, (int)Math.Round(255.0 * (1.0 - i / (double)neg.Length))));
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

            //  4. Построить ROC-кривую по обучающей хромосоме
            DrawROC(mfPeaks.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray(),
                    mfNoise.Select(motiff => GetROC(new GetRocParams(), peaksPos, noisePos, motiff)).ToArray());

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

		static void Main(string[] args)
		{
		    MainPlan();
			Console.WriteLine("Ok\nPress any key to exit");
			Console.ReadKey();
		}


        #region trash
        static void CheckChr1()
	    {
            var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
            Debug.Assert(chr != null);
	    }

	    static void GetExpDataForCsv()
	    {
            var pars1 = new Dictionary<string, string> { { "type", "broadPeak" }, { "cell", "A549" }, { "replicate", "1" } };
            var exp1 = DNaseIManager.GetSensitivityResults(pars1, ChromosomeEnum.Chr1);
            using (var f1 = File.CreateText("broadPeak.srt1.csv"))
            {
                foreach (var item in exp1.Items.OrderBy(p => -p.Value1))
                    f1.WriteLine(item.Value1 + "; " + item.Value2);
                f1.Flush();
            }
            using (var f2 = File.CreateText("broadPeak.srt2.csv"))
            {
                foreach (var item in exp1.Items.OrderBy(p => -p.Value2))
                    f2.WriteLine(item.Value2 + "; " + item.Value1);
                f2.Flush();
            }
	    }

        static void GetClassifiedExpData()
        {
            var pars1 = new Dictionary<string, string> { { "type", "broadPeak" }, { "cell", "A549" }, { "replicate", "1" } };
            var exp1 = DNaseIManager.GetClassifiedRegions(ChromosomeEnum.Chr1, pars1, true);
            Debug.Assert(exp1.Count == 3);
            var statP = exp1[ClassifiedRegion.MotifContainsStatus.Present].Select(p => p.RawValue2).MinMeanMax();
            var statN = exp1[ClassifiedRegion.MotifContainsStatus.NotPresent].Select(p => p.RawValue2).MinMeanMax();
            var statU = exp1[ClassifiedRegion.MotifContainsStatus.Unknown].Select(p => p.RawValue2).MinMeanMax();
            Debug.Assert(exp1.Count == 3);
        }

        static void DrawForm()
        {

            // * Вывести на экран простенькую форму с графиком
            // * Научиться выводить график нужного вида + научиться экспортировать в eps.
            // * Автоматизировать процесс, вывести вывод графиков в отдельную библиотеку
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new Form { Width = 800, Height = 600 };

            var f = new ZedGraphControl
            {
                Anchor = AnchorStyles.Top,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };

            var yaxis = f.GraphPane.YAxis;
            yaxis.Type = AxisType.Log;
            yaxis.Scale.MaxAuto = false;
            yaxis.Scale.MinAuto = false;
            yaxis.Scale.Max = 1e+3;
            yaxis.Scale.Min = 1e-1;

            var xaxis = f.GraphPane.XAxis;
            xaxis.Scale.MaxAuto = false;
            xaxis.Scale.MinAuto = false;
            xaxis.Scale.Max = 110;
            xaxis.Scale.Min = -10;
            xaxis.Type = AxisType.LinearAsOrdinal;

            var grow = Enumerable.Range(1, 10).Select(p => (double)p * p).ToArray();
            var grow2 = Enumerable.Range(1, 10).Select(p => (double)p * p / 2.0).ToArray();
            var grow3 = Enumerable.Range(1, 10).Select(p => (double)p * p / 4.0).ToArray();

            var growx = Enumerable.Range(1, 10).Select(p => (double)p * p + 0.5).ToArray();

            f.GraphPane.BarSettings.Type = BarType.SortedOverlay;
            f.GraphPane.BarSettings.MinBarGap = 1000;

            var bar1 = f.GraphPane.AddBar("bar", grow, grow, Color.Blue);
            var bar2 = f.GraphPane.AddBar("bar2", grow, grow2, Color.Red);
            var bar3 = f.GraphPane.AddBar("bar3", growx, grow3, Color.Green);

            bar1.Bar.Fill.Type = FillType.Brush;
            bar1.Bar.Border.IsVisible = false;
            bar2.Bar.Fill.Type = FillType.Solid;
            bar2.Bar.Border.IsVisible = false;
            bar3.Bar.Fill.Type = FillType.GradientByY;
            bar3.Bar.Border.IsVisible = false;

            form.Controls.Add(f);

            f.AxisChange();
            f.Invalidate();

            Application.Run(form);
        }

        
        #endregion trash
    }
}
