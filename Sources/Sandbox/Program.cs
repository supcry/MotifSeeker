using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MotifSeeker;
using MotifSeeker.Data.Dna;
using MotifSeeker.Data.DNaseI;
using MotifSeeker.Graph;
using MotifSeeker.Helpers;
using MotifSeeker.Sfx;
using ZedGraph;

namespace Sandbox
{
	class Program
	{
	    static void MainPlan()
	    {
            // План:
            //  1. получить последовательности участков с пиками и без пиков
            //  2. построить суффиксы по участкам с пиками и без пиков
            //  3. удостовериться, что участки в обоих суф.структурах находятся.

            //  1. получить последовательности участков с пиками и без пиков
            var t = DateTime.Now;
            var flow = NarrowPeaksMerger.GetMergedNarrowPeaks(ChromosomeEnum.Chr1, 10).ToArray();

            
            //MergedBarGraph.DrawPeaks(flow, 5, 0, 2000000);

            var peakRegions = new List<MergedNarrowPeak>();
            var noiseRegions = new List<MergedNarrowPeak>();
            var nonRegions = new List<KeyValuePair<int, int>>();

            const int minCellsPerRegion = 2;
            const int minAverageValue1 = 200;
            const int minSizeOfRegion = 100;
			const int maxSizeOfRegion = 10000;
            int lastPos = 0;

            int totalPeaksLen = 0;
            int totalPeaksLenAvg = 0;
            int totalNonpeaksLen = 0;

            foreach (var peak in flow)
            {
                // добавим регион без пиков, если он есть
				if (peak.StartPosMin - lastPos >= minSizeOfRegion && peak.StartPosMin - lastPos <= maxSizeOfRegion)
                {
                    if (totalNonpeaksLen < 150000)
                        nonRegions.Add(new KeyValuePair<int, int>(lastPos, peak.StartPosMin));
                    totalNonpeaksLen += peak.StartPosMin - lastPos;
                }
                Debug.Assert(lastPos <= peak.EndPosMax + minSizeOfRegion);
                lastPos = peak.EndPosMax;
                // определим качество региона
                if (peak.Count < minCellsPerRegion || peak.AvgValue1 < minAverageValue1 || peak.Size < minSizeOfRegion)
                {
                    noiseRegions.Add(peak);
                    continue;
                }
                Debug.Assert(peak.StartPos >= 0);
                Debug.Assert(peak.EndPos >= 0);
                peakRegions.Add(peak);
                totalPeaksLen += peak.EndPosMax - peak.StartPosMin;
                totalPeaksLenAvg += peak.EndPos - peak.StartPos;
            }
            peakRegions.TrimExcess();
            nonRegions.TrimExcess();
            Console.WriteLine("Expirement data merged, dt=" + (DateTime.Now - t));
            Console.WriteLine("PeaksTotalLen=" + totalPeaksLen + ", EmptyTotalLen=" + totalNonpeaksLen);

            //MergedBarGraph.DrawPeaks(peakRegions, 5, 0, 20000000);
            //  2. построить суффиксы по участкам с пиками и без пиков
            t = DateTime.Now;
            var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
            Console.WriteLine("Chromosome converted, dt=" + (DateTime.Now - t));
            t = DateTime.Now;
            var sfxPeaks = SuffixBuilder.BuildMany2(peakRegions.Select(p => chr.GetPack(p.StartPos, p.Size)).ToArray(), 10);
            Console.WriteLine("Peaks sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxPeaks.StrokeSize + ", elementGroups=" + sfxPeaks.GetElementGroups().Count);
            t = DateTime.Now;
            var sfxEmpty = SuffixBuilder.BuildMany2(nonRegions.Select(p => chr.GetPack(p.Key, p.Value - p.Key)).ToArray(), 10);
			Console.WriteLine("Empty sfx build, dt=" + (DateTime.Now - t) + ", size=" + sfxEmpty.StrokeSize + ", elementGroups=" + sfxEmpty.GetElementGroups().Count);

		    var peaksGroups = sfxPeaks.GetElementGroups();
			var emptyGroups = sfxEmpty.GetElementGroups();

			var peaksGroups2 = peaksGroups.OrderByDescending(p => p.Count).ToArray();
            var emptyGroups2 = emptyGroups.OrderByDescending(p => p.Count).ToArray();

			Console.WriteLine("TransTest (peaks on peaks)");
			TransTest(sfxPeaks.StrokeSize, peaksGroups2.Take(10), sfxPeaks);
			Console.WriteLine("TransTest (peaks on empty)");
		    TransTest(sfxPeaks.StrokeSize, peaksGroups2.Take(10), sfxEmpty);
			Console.WriteLine("TransTest (empty on peaks)");
			TransTest(sfxEmpty.StrokeSize, emptyGroups2.Take(10), sfxPeaks);

	        var peakClustering = new Clustering(peaksGroups2);
            var emptyClustering = new Clustering(emptyGroups2);

            peakClustering.WriteElementsLog("results/peakGroups.log.txt", "Совпавшие группы в интересных регионах");
            emptyClustering.WriteElementsLog("results/emptyGroups.log.txt", "Совпавшие группы в фоновых регионах");

            peakClustering.WriteToFileForGephi("results/peakGroupsM3");
            emptyClustering.WriteToFileForGephi("results/emptyGroupsM3");

	        var peakCLusters = peakClustering.Work(5);
            var emptyCLusters = emptyClustering.Work(5);

            Console.WriteLine("fin");
	        Console.ReadKey();
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
			Console.WriteLine("origFactor=" + cntOrig + ", transFactor=" + Math.Round((double)origLen * cntTrans / (double)(cmp.StrokeSize), 3));
		}

	    static void DrawForm()
	    {

	        // * Вывести на экран простенькую форму с графиком
            // * Научиться выводить график нужного вида + научиться экспортировать в eps.
            // * Автоматизировать процесс, вывести вывод графиков в отдельную библиотеку
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new Form {Width = 800, Height = 600};

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

	        var grow = Enumerable.Range(1, 10).Select(p => (double) p*p).ToArray();
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


		static void Main(string[] args)
		{
		    MainPlan();
		    //DrawForm();
			//Console.WriteLine("Ok\nPress any key to exit");
			Console.ReadKey();

		}

	    static void CheckSfxArrayBuilder()
	    {
	        try
	        {
	            var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
	            for (int i = 2; i < chr.Count; i *= 2)
	            {
	                var sw = new Stopwatch();
	                var pack = chr.GetPack(0, i);
	                sw.Start();
                    var tmp = SuffixBuilder.BuildOne(pack);
	                sw.Stop();
	                Console.WriteLine("count=" + i + ", time=" + sw.Elapsed);

	                if (i > 1000000)
	                    break;
	            }
	        }
	        catch (Exception ex)
	        {
	            Console.WriteLine("ex=" + ex);
	        }
	    }

        static void CheckSfxArrayBuilderForClass1()
        {
            try
            {
                var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
                var pars1 = new Dictionary<string, string> { { "type", "narrowPeak" }, {"view", "Peaks"}, { "cell", "A549" }, { "replicate", "1" } };
                var exp = DNaseIManager.GetClassifiedRegions(ChromosomeEnum.Chr1, pars1, false)[ClassifiedRegion.MotifContainsStatus.Present];
                var parts = new List<Nucleotide[]>();
                int len = 0;
                int partId = 0;
                SuffixArray sfx;
                for (int i = 2; i < chr.Count && partId < exp.Count; i *= 2)
                {
                    while (len < i && partId < exp.Count)
                    {
                        var region = exp[partId++];
                        len += region.EndPos - region.StartPos;
                        parts.Add(chr.GetPack(region.StartPos, region.EndPos - region.StartPos));
                    }
                    var sw = new Stopwatch();
                    sw.Start();
                    sfx = SuffixBuilder.BuildMany(parts);
                    sw.Stop();
                    Console.WriteLine("len=" + i + ",\tparts=" + partId + ",\ttime=" + sw.Elapsed);
                }
                //sfx.PointerDown().GetAllCites(new Pointer(0,0,100), )

            }
            catch (Exception ex)
            {
                Console.WriteLine("ex=" + ex);
            }
        }

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

	    static void BenchmarkDics()
	    {
            var rnd = new Random(1);
            const int count = 1000000;
            var hs = new HashSet<int>();
            var pairs = Enumerable.Repeat(1, int.MaxValue)
                                  .Select(_ => new KeyValuePair<int, int>(rnd.Next(), rnd.Next()))
                                  .Where(p => hs.Add(p.Key))
                                  .Take(count)
                                  .ToArray();

            BenchmarkDictionary(pairs);
            BenchmarkStaticDictionary(pairs);
	    }

        static void BenchmarkDictionary(ICollection<KeyValuePair<int, int>> data)
        {
            var sw = new Stopwatch();
            sw.Start();
            var dic = new Dictionary<int, int>(data.Count);
            foreach (var d in data)
                dic[d.Key] = d.Value;
            sw.Stop();
            var sw2 = new Stopwatch();
            sw2.Start();
            foreach (var d in data.Reverse())
                if (d.Value != dic[d.Key])
                    throw new Exception();
            sw2.Stop();
            Console.WriteLine("Dictionary: creation=" + sw.Elapsed + ", search=" + sw2.Elapsed);
        }

        static void BenchmarkStaticDictionary(ICollection<KeyValuePair<int, int>> data)
        {
            var sw = new Stopwatch();
            sw.Start();
            var dic = new StaticDictionary<int, int>(data);
            sw.Stop();
            var sw2 = new Stopwatch();
            sw2.Start();
            foreach (var d in data)
                if(d.Value != dic[d.Key])
                    throw new Exception();
            sw2.Stop();
            Console.WriteLine("StaticDictionary: creation=" + sw.Elapsed + ", search=" + sw2.Elapsed);
        }
	}
}
