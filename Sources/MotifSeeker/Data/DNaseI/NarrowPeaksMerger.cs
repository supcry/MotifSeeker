using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Data.DNaseI
{
    /// <summary>
    /// Класс работы с экспериментальными данными (NarrowPeak).
    /// Объединяет результаты классификации пиков по разным клеткам.
    /// </summary>
	public static class NarrowPeaksMerger
	{
		public static IEnumerable<MergedNarrowPeak> GetMergedNarrowPeaks(ChromosomeEnum? onlyChr, int maxCellsCount)
		{
			var filter = new Dictionary<string, string> { { "type", "narrowPeak" }, {"view", "Peaks"} };
			var files = DNaseIManager.GetFiles(filter, maxCellsCount).ToArray();
			return GetMergedNarrowPeaks(files, onlyChr);
		}

		private static IEnumerable<MergedNarrowPeak> GetMergedNarrowPeaks(string[] filePaths, ChromosomeEnum? onlyChr)
		{
			var tmp = filePaths.Select((p,i) => GetNarrowPeak(p, onlyChr, (short)i)).ToArray();
			return Merge(tmp);
		}

		/// <summary>
		/// Возвращает отсортированные в порядке положения участки с пиками.
		/// </summary>
		private static IEnumerable<MergedNarrowPeak> GetNarrowPeak(string filePath, ChromosomeEnum? onlyChr, short cellId)
		{
			return File.ReadLines(filePath)
						.Select(p => new MergedNarrowPeak(p, cellId))
						.Where(p =>  !onlyChr.HasValue || p.Chr == onlyChr.Value);
		}

		/// <summary>
		/// Слияет несколько потоков от разных типов клеток.
		/// </summary>
		private static IEnumerable<MergedNarrowPeak> Merge(IEnumerable<IEnumerable<MergedNarrowPeak>> flows)
		{
			var q = new Queue<IEnumerable<MergedNarrowPeak>>(flows);
			while (q.Count > 0)
			{
				var a = q.Dequeue();
				if (q.Count == 0)
					return a;
				var b = q.Dequeue();
				var c = Merge(a, b);
				q.Enqueue(c);
			}
			throw new Exception("Merge получих на входе пустой список данных");
		}

		private static IEnumerable<MergedNarrowPeak> Merge(IEnumerable<MergedNarrowPeak> f1, IEnumerable<MergedNarrowPeak> f2)
		{
			var f1E = f1.GetEnumerator();
			if (!f1E.MoveNext())
			{
				foreach (var f in f2)
					yield return f;
				yield break;
			}
			var f2E = f2.GetEnumerator();
			if (!f2E.MoveNext())
			{
				do yield return f1E.Current; while (f1E.MoveNext());
				yield break;
			}

			while (true)
			{
				var state = MergedNarrowPeak.GetMergeStatus(f1E.Current, f2E.Current);

				if (state == 0)
				{
					yield return MergedNarrowPeak.Merge(f1E.Current, f2E.Current);
					if (!f1E.MoveNext())
					{
						f1E = null;
						break;
					}
					if (f2E.MoveNext()) continue;
					f2E = null;
					break;
				}
				if (state == 1)
				{
					yield return f1E.Current;
					if (f1E.MoveNext()) continue;
					f1E = null;
					break;
				}
				Debug.Assert(state == -1);
				yield return f2E.Current;
				if (f2E.MoveNext()) continue;
				f2E = null;
				break;
			}
			var ff = f1E ?? f2E;
			do yield return ff.Current; while (ff.MoveNext());
		}
	}

    /// <summary>
    /// Регион ДНК с агрегированной информацией о 
    /// </summary>
	public class MergedNarrowPeak
	{
		/// <summary>
		/// Для какой хромосомы эта строка.
		/// </summary>
		public readonly ChromosomeEnum Chr;

        public readonly short[] CellIds;

		/// <summary>
		/// Начало региона (с 0)
		/// </summary>
		public readonly int StartPos;

		/// <summary>
		/// Конец региона (с 0)
		/// </summary>
		public readonly int EndPos;

		public int Size { get { return EndPos - StartPos; } }

		/// <summary>
		/// Первое значение (?)
		/// </summary>
		public readonly float[] Values1;

		/// <summary>
		/// Второе значение (?)
		/// </summary>
		public readonly float[] Values2;

		public readonly int StartPosMin;

		public readonly int StartPosMax;

		public readonly int EndPosMin;

		public readonly int EndPosMax;

		public int Count { get { return Values1.Length; } }

		public bool StrictMerge { get { return (EndPosMax == EndPosMin && StartPosMax == StartPosMin); } }

		private float _avgValue1 = float.NaN;

		public float AvgValue1
		{
			get
			{
				if (float.IsNaN(_avgValue1))
					_avgValue1 = Values1.Average();
				return _avgValue1;
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Chr + ",");
			if(Count > 1)
				sb.Append("cnt: " + Count + ",");
			if(!StrictMerge)
				sb.Append("n,");
			sb.Append("pos:{" + StartPos + "," + EndPos + "},");
			sb.Append("avg:" + Math.Round(AvgValue1,1));

		    sb.Append("cells:{" + string.Join(";", CellIds) + "}");
			return sb.ToString();
		}

		public MergedNarrowPeak(ChromosomeEnum chr, int start, int end,
			                    IEnumerable<float> v1, IEnumerable<float> v2,
                                int minStart, int maxStart, int minEnd, int maxEnd, short[] cellIds)
		{
            Debug.Assert(start >= 0);
            Debug.Assert(end > 0);
			Chr = chr;
			StartPos = start;
			EndPos = end;
			Values1 = v1.ToArray();
			Values2 = v2.ToArray();
			StartPosMin = minStart;
			EndPosMax = maxEnd;
			StartPosMax = maxStart;
			EndPosMin = minEnd;
		    CellIds = cellIds;
		}

		public static MergedNarrowPeak Merge(MergedNarrowPeak a, MergedNarrowPeak b)
		{
			Debug.Assert(a.Chr == b.Chr);
            var start = (int)Math.Round((a.StartPos * (long)a.Count + b.StartPos * (long)b.Count) / (double)(a.Count + b.Count));
            var end = (int)Math.Round((a.EndPos * (long)a.Count + b.EndPos * (long)b.Count) / (double)(a.Count + b.Count));
			var minStart = Math.Min(a.StartPosMin, b.StartPosMin);
			var maxStart = Math.Max(a.StartPosMax, b.StartPosMax);
			var minEnd = Math.Min(a.EndPosMin, b.EndPosMin);
			var maxEnd = Math.Max(a.EndPosMax, b.EndPosMax);
            return new MergedNarrowPeak(a.Chr, start, end, a.Values1.ConcatArray(b.Values1), a.Values2.ConcatArray(b.Values2),
                                        minStart, maxStart, minEnd, maxEnd, a.CellIds.ConcatArray(b.CellIds));
		}

		public MergedNarrowPeak(string line, short cellId)
		{
			//chr1	566760	566910	.	0	.	339	157.884	-1	-1
			var parts = line.Split('\t');
			Debug.Assert(parts.Length >= 9);
			Chr = (ChromosomeEnum)Enum.Parse(typeof(ChromosomeEnum), parts[0], true);
			StartPosMin = StartPosMax = StartPos = int.Parse(parts[1]);
			EndPosMin = EndPosMax = EndPos = int.Parse(parts[2]);
			Values1 = new []{float.Parse(parts[6], new NumberFormatInfo { CurrencyDecimalSeparator = "." })};
			Values2 = new []{float.Parse(parts[7], new NumberFormatInfo { CurrencyDecimalSeparator = "." })};
		    CellIds = new[] {cellId};
		}

		private MergedNarrowPeak() { }

		/// <summary>
		/// Возвращает статус возможности слияния.
		/// </summary>
		/// <returns>0 - можно сливать, 1 - a идёт до b, -1 - a идёт после b</returns>
		public static int GetMergeStatus(MergedNarrowPeak a, MergedNarrowPeak b)
		{
			if (a.Chr != b.Chr)
				return (int)b.Chr - (int)a.Chr > 0 ? 1 : -1;
			if (a.StartPos >= b.EndPos)
				return -1;
			if (b.StartPos >= a.EndPos)
				return 1;
			if (a.StartPos >= b.StartPos && a.EndPos <= b.EndPos)
				return 0;
			if (b.StartPos >= a.StartPos && b.EndPos <= a.EndPos)
				return 0;
			if (Math.Min(Math.Abs(a.StartPos - b.StartPos), Math.Abs(b.EndPos - a.EndPos)) < Math.Min(a.Size, b.Size)/5)
				return 0;
			return b.StartPos > a.StartPos ? 1 : -1;
		}
	}
}
