using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MotifSeeker;
using MotifSeeker.Data.Dna;
using MotifSeeker.Data.DNaseI;
using MotifSeeker.Helpers;
using MotifSeeker.Sfx;

namespace Sandbox
{
	class Program
	{
		static void Main(string[] args)
		{
            // План:
            //  1. получить последовательности участков с пиками и без пиков
            //  2. построить суффиксы по участкам с пиками и без пиков
            //  3. удостовериться, что участки в обоих суф.структурах находятся.

            //  1. получить последовательности участков с пиками и без пиков
		    var t = DateTime.Now;
            var flow = NarrowPeaksMerger.GetMergedNarrowPeaks(ChromosomeEnum.Chr1, 10);
		    var peakRegions = new List<MergedNarrowPeak>();
		    var nonRegions = new List<KeyValuePair<int, int>>();

		    const int minCellsPerRegion = 2;
		    const int minAverageValue1 = 200;
		    const int minSizeOfRegion = 100;
		    int lastPos = 0;

		    int totalPeaksLen = 0;
            int totalPeaksLenAvg = 0;
		    int totalNonpeaksLen = 0;

		    foreach (var peak in flow)
		    {
                // добавим регион без пиков, если он есть
                if (peak.StartPosMin - lastPos >= minSizeOfRegion)
		        {
                    if(totalNonpeaksLen < 300000)
    		            nonRegions.Add(new KeyValuePair<int, int>(lastPos, peak.StartPosMin));
                    totalNonpeaksLen += peak.StartPosMin - lastPos;
		        }
                Debug.Assert(lastPos <= peak.EndPosMax + minSizeOfRegion);
		        lastPos = peak.EndPosMax;
                // определим качество региона
                if (peak.Count < minCellsPerRegion || peak.AvgValue1 < minAverageValue1)
                    continue;
		        if (peak.Size < minSizeOfRegion)
		            continue;
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
            //  2. построить суффиксы по участкам с пиками и без пиков
		    t = DateTime.Now;
            var chr = ChrManager.GetChromosome(ChromosomeEnum.Chr1);
            Console.WriteLine("Chromosome converted, dt=" + (DateTime.Now - t));
            t = DateTime.Now;
		    var sfxPeaks = SuffixBuilder.BuildMany2(peakRegions.Select(p => chr.GetPack(p.StartPos, p.Size)).ToArray());
            Console.WriteLine("Peaks sfx build, dt=" + (DateTime.Now - t));
            t = DateTime.Now;
            var sfxEmpty = SuffixBuilder.BuildMany2(nonRegions.Select(p => chr.GetPack(p.Key, p.Value - p.Key)).ToArray());
            Console.WriteLine("Empty sfx build, dt=" + (DateTime.Now - t));
            
            //  3. удостовериться, что участки в обоих суф.структурах находятся.

            // подготовим тестовые образцы
		    var testPeakPos = peakRegions[peakRegions.Count/2];
		    var testEmptyPos = nonRegions[nonRegions.Count/2];
		    var testPeakData = chr.GetPack(testPeakPos.StartPos, testPeakPos.Size);
            var testEmptyData = chr.GetPack(testEmptyPos.Key, Math.Min(testEmptyPos.Value - testEmptyPos.Key, testPeakPos.Size));
		    for (int i = 0; i < 2; i++)
		    {
		        t = DateTime.Now;
		        var cites1 = sfxPeaks.GetAllCites(testPeakData.Select(p => (byte) p).ToArray(), testPeakData.Length - 1);
		        Console.WriteLine("Search peak in peaks, dt=" + (DateTime.Now - t) + ", cnt=" + cites1.Length);
		        Debug.Assert(cites1.Length == 1);
		        t = DateTime.Now;
		        var cites2 = sfxPeaks.GetAllCites(testEmptyData.Select(p => (byte) p).ToArray(), testEmptyData.Length - 1);
		        Console.WriteLine("Search empty in peaks, dt=" + (DateTime.Now - t) + ", cnt=" + cites2.Length);
		        Debug.Assert(cites2.Length == 0);
		        t = DateTime.Now;
		        var cites3 = sfxEmpty.GetAllCites(testPeakData.Select(p => (byte) p).ToArray(), testPeakData.Length - 1);
		        Console.WriteLine("Search peak in empties, dt=" + (DateTime.Now - t) + ", cnt=" + cites3.Length);
		        Debug.Assert(cites3.Length == 0);
		        t = DateTime.Now;
		        var cites4 = sfxEmpty.GetAllCites(testEmptyData.Select(p => (byte) p).ToArray(), testEmptyData.Length - 1);
		        Console.WriteLine("Search empty in empties, dt=" + (DateTime.Now - t) + ", cnt=" + cites4.Length);
		        Debug.Assert(cites4.Length == 1);
		    }
		    //var items = flow.ToArray();
            //var totalCount = items.Sum(p => p.Count);
            ////var a = items[334];
            ////var b = items[335];
            ////var s = MergedNarrowPeak.GetMergeStatus(a, b);

            //var byAvg = items.OrderByDescending(p => p.AvgValue1).Where(p => !p.StrictMerge).ToArray();
            //var byCnt = items.OrderByDescending(p => p.Count).Where(p => !p.StrictMerge).ToArray();
            //CheckSfxArrayBuilder();
		    //CheckSfxArrayBuilderForClass1();
			Console.WriteLine("Ok\nPress any key to exit");
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
