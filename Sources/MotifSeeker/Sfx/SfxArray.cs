using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MotifSeeker.Sfx
{
	public class SfxArray
	{
		private readonly byte[] _hashes;
		private readonly int _hashesLength;

		private readonly uint[] _suftab;
		private readonly int[] _inversSuftab;
		private readonly int[] _lcptab;
		private readonly Dictionary<HashKey, LcpValue> _cldtab;
		private readonly Dictionary<Interval, Interval> _linktab;

		public int HashesLength
		{
			get { return _hashesLength; }
		}

		public uint[] Suftab
		{
			get { return _suftab; }
		}

		public int[] Lcptab
		{
			get { return _lcptab; }
		}

		public SfxArray(byte[] hashes)
		{
			_hashes = hashes;
			_hashesLength = hashes.Length;

			_suftab = BuildSuftab();
			_inversSuftab = BuildInverSuftab();

			_lcptab = BuildLcpTable();

			BuildHashTables(out _cldtab, out _linktab);

			_inversSuftab = null;
		}

		#region BuildSuffixArray

		/// <summary>
		/// Построение суффиксного массива
		/// </summary>
		/// <returns></returns>
		public uint[] BuildSuftab()
		{
			return Radix.Sort(_hashes);
		}

		/// <summary>
		/// Построение таблицы lcp (longest common prefix)
		/// </summary>
		/// <returns></returns>
		private int[] BuildLcpTable()
		{
			var lcptab = new int[_hashesLength];
			lcptab[0] = 0;

			var h = 0;

			for (int i = 0; i < _hashesLength; i++)
			{
				if (_inversSuftab[i] > 0)
				{
					var k = _suftab[_inversSuftab[i] - 1];
					while (_hashes[i + h] == _hashes[k + h])
						h++;
					lcptab[_inversSuftab[i]] = h;
					if (h > 0)
						h--;
				}
			}
			return lcptab;
		}

		/// <summary>
		/// Инвертированный суффиксный массив
		/// </summary>
		/// <returns></returns>
		private int[] BuildInverSuftab()
		{
			var rank = new int[_hashesLength];
			for (int i = 0; i < _hashesLength; i++)
				rank[_suftab[i]] = i;
			return rank;
		}

		/// <summary>
		/// Дерево на основе lcp интервалов.
		/// </summary>
		/// <returns></returns>
		private LcpTree BuildLcpTree()
		{
			var lenght = _lcptab.Length;

			var stack = new Stack<LcpTree>();
			var lastInterval = LcpTree.NotDefinedInterval();

            var peek = LcpTree.NotDefinedInterval();
            stack.Push(peek);

			for (int i = 1; i < lenght; i++)
			{
				var leftBound = i - 1;
			    
				while (_lcptab[i] < peek.Lcp)
				{
                    peek.RightBound = i - 1;
					lastInterval = stack.Pop();
				    peek = stack.Peek();
					leftBound = lastInterval.LeftBound;
                    if (_lcptab[i] <= peek.Lcp)
					{
                        peek.ChildList.Add(lastInterval);
						lastInterval = LcpTree.NotDefinedInterval();
					}
				}
                if (_lcptab[i] > peek.Lcp)
				{
					if (lastInterval.RightBound != NotDefined)
					{
						stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree> { lastInterval }));
						lastInterval = LcpTree.NotDefinedInterval();
					}
					else
						stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree>()));
				    peek = stack.Peek();
				}
			}

			var root = stack.Pop();
			root.RightBound = lenght - 1;
			root.Lcp = 0;

			return root;
		}

		private void BuildHashTables(out Dictionary<HashKey, LcpValue> cldtab,
			out Dictionary<Interval, Interval> linkstab)
		{
			var root = BuildLcpTree();
			var dic = new Dictionary<HashKey, LcpValue>(HashesLength * 2);

			var maxLcp = Lcptab.Max() + 1;
			var lcpIntervals = new List<Interval>[maxLcp];


			for (int i = 0; i < maxLcp; i++)
				lcpIntervals[i] = new List<Interval>();

			BuildHashTableCustomStack(root, lcpIntervals, dic);

			cldtab = dic;
			linkstab = BuildSuffixLinks2(lcpIntervals);
		}


		private void BuildHashTableCustomStack(LcpTree root, List<Interval>[] lcpIntervals,
			                                   Dictionary<HashKey, LcpValue> dictionary)
		{
			var stack = new Stack<KeyValuePair<LcpTree, int>>();
			stack.Push(new KeyValuePair<LcpTree, int>(root, 0));
			while (stack.Count != 0)
			{
				var item = stack.Pop();
				var tree = item.Key;
				var childId = item.Value;

				if (childId == 0)
				{
					lcpIntervals[tree.Lcp].Add(new Interval(tree.LeftBound, tree.RightBound));
					AddInterval(tree, dictionary);
					//AddSingleHashes(tree, dictionary);
				}
				if (tree.ChildList.Count > childId)
				{
					var childTree = tree.ChildList[childId];
					stack.Push(new KeyValuePair<LcpTree, int>(tree, childId + 1));
					stack.Push(new KeyValuePair<LcpTree, int>(childTree, 0));
				}
			}
		}


		/// <summary>
		/// Добавляет в таблицу значение Хеш-Интервал ( начало - конец )
		/// </summary>
		/// <param name="interval"></param>
		/// <param name="dic"></param>
		private void AddInterval(LcpTree interval, Dictionary<HashKey, LcpValue> dic)
		{
			foreach (var child in interval.ChildList)
			{
				var id = _suftab[child.LeftBound] + interval.Lcp;
				var hash = _hashes[id];
				dic[new HashKey(hash, interval)] = new LcpValue(child);
			}
			AddSingleHashes(interval, dic);
		}

		/// <summary>
		/// Добавляет значение хешей на одиночных интервалах ( когда конец и начало интервала одинаковы) в хештаблицу.
		/// </summary>
		private void AddSingleHashes(LcpTree interval, Dictionary<HashKey, LcpValue> hashTable)
		{
			var left = interval.LeftBound;
			var right = interval.RightBound;

			foreach (var c in interval.ChildList)
			{
				while (left < c.LeftBound)
				{
					var hash = _hashes[_suftab[left] + interval.Lcp];
					hashTable[new HashKey(hash, interval.LeftBound, interval.RightBound)] =
						new LcpValue((_hashesLength - (int)_suftab[left]) - 1, left, left);
					left++;
				}
				left = c.RightBound + 1;
			}

			while (left <= right)
			{
				var hash = _hashes[_suftab[left] + interval.Lcp];
				hashTable[new HashKey(hash, interval.LeftBound, interval.RightBound)] =
					new LcpValue((_hashesLength - (int)_suftab[left]) - 1, left, left);
				left++;
			}
		}

		#endregion


		private Dictionary<Interval, Interval> BuildSuffixLinks2(List<Interval>[] lcpIntervals)
		{
			var dic = new Dictionary<Interval, Interval>();

			var maxLcp = lcpIntervals.Length;
			var tempList = new List<KeyValuePair<Interval, int>>[maxLcp];

			for (int i = 0; i < maxLcp; i++)
				tempList[i] = new List<KeyValuePair<Interval, int>>();

			for (int i = 1; i < maxLcp; i++)
			{
				var intervals = lcpIntervals[i];
				var j = 0;
				foreach (var interval in intervals)
					tempList[i].Add(new KeyValuePair<Interval, int>(
						new Interval(
							_inversSuftab[Suftab[interval.Left] + 1],
							_inversSuftab[Suftab[interval.Right] + 1]),
						j++));
				tempList[i].Sort((x, y) => x.Key.CompareTo(y.Key));
			}

			dic[lcpIntervals[0][0]] = lcpIntervals[0][0];

			for (int i = 1; i < maxLcp; i++)
			{
				var l1 = lcpIntervals[i - 1];
				var l2 = lcpIntervals[i];
				var n2 = tempList[i];
				var l = 0;
				for (int n = 0; n < n2.Count; n++)
				{
					while (l < l1.Count)
					{
						if (n2[n].Key.Left >= l1[l].Left && n2[n].Key.Right <= l1[l].Right)
						{
							dic[l2[n2[n].Value]] = l1[l];
							break;
						}
						l++;
					}
				}
			}

			return dic;
		}


		public bool PointerDown(Pointer pointer, byte hash, int depth)
		{
			if (pointer.Depth == depth)
			{
				pointer.LastLeft = pointer.Left;
				pointer.LastRigth = pointer.Rigth;
				pointer.LastDepth = pointer.Depth;

				var key = new HashKey(hash, pointer.Left, pointer.Rigth);
				LcpValue value;
				if (!_cldtab.TryGetValue(key, out value))
					return false;

				pointer.Left = value.Left;
				pointer.Rigth = value.Right;
				pointer.Depth = value.Lcp;
				return true;
			}
			var id = _suftab[pointer.Left] + depth;
			var res = _hashes[id] == hash;
			return res;
		}






		public bool UseSuffixLink(Pointer pointer)
		{
			var interval = new Interval(pointer.LastLeft, pointer.LastRigth);
			Interval value;
			if (_linktab.TryGetValue(interval, out value))
			{
				pointer.LastLeft = value.Left;
				pointer.LastRigth = value.Right;

				pointer.Left = value.Left;
				pointer.Rigth = value.Right;
				pointer.Depth--;

				return true;
			}
			return false;
		}


		public bool DownByLink(Pointer pointer, byte hash, int depth)
		{
			if (pointer.Depth < depth)
			{
				var key = new HashKey(hash, pointer.Left, pointer.Rigth);
				LcpValue value;

				if (!_cldtab.TryGetValue(key, out value))
					throw new Exception();
				{
					pointer.Left = value.Left;
					pointer.Rigth = value.Right;
					pointer.Depth = value.Lcp;
					return true;
				}
			}
			pointer.Depth = depth;
			return false;
		}

		/// <summary>
		/// Проходим по Лсп массиву слева направо
		/// </summary>
		public List<SuffixSubstr> GetAllCites(Pointer pointer, int minLength, int depth, int srcId)
		{
			var list = new List<SuffixSubstr>
            {
                new SuffixSubstr
                {
                    SrcIdx = srcId,
                    ChkIdx = (int) Suftab[pointer.Left],
                    Length = Math.Min(depth, pointer.Depth)
                }
            };

			return list;

			//Налево от найденной цитаты

			var i = pointer.Left;
			while (_lcptab[i] >= minLength)
			{
				--i;
				list.Add(new SuffixSubstr
				{
					SrcIdx = srcId,
					ChkIdx = (int)Suftab[i],
					Length = Math.Min(depth, _lcptab[i + 1])
				});

			}

			//return list;
			//Направо от найденной цитаты
			i = pointer.Left;
			while (_lcptab[++i] >= minLength)
				list.Add(new SuffixSubstr
				{
					SrcIdx = srcId,
					ChkIdx = (int)Suftab[i],
					Length = Math.Min(depth, _lcptab[i])
				});
			return list;
		}
		#region Методы, которые возможно пригодятся.

		private void CreateChildTable()
		{
			var depth = new int[_hashesLength - 1];
			var extLcp = new int[_hashesLength - 1];
		}

		// Медленный , но коректно работающий метод получения суффиксных ссылок.
		private Dictionary<Interval, Interval> BuildSuffixLinks(List<Interval>[] lcpIntervals)
		{
			var dic = new Dictionary<Interval, Interval>();

			var maxLcp = lcpIntervals.Length;
			var tempList = new List<Interval>[maxLcp];

			for (int i = 0; i < maxLcp; i++)
				tempList[i] = new List<Interval>();

			for (int i = 1; i < maxLcp; i++)
			{
				var intervals = lcpIntervals[i];
				foreach (var interval in intervals)
					tempList[i].Add(new Interval(
						_inversSuftab[Suftab[interval.Left] + 1],
						_inversSuftab[Suftab[interval.Right] + 1]));
			}
			for (int i = 1; i < maxLcp; i++)
			{
				var l1 = lcpIntervals[i - 1];
				var l2 = lcpIntervals[i];
				var n2 = tempList[i];

				for (int n = 0; n < n2.Count; n++)
				{
					var l = 0;
					while (l < l1.Count)
					{
						if (n2[n].Left >= l1[l].Left && n2[n].Right <= l1[l].Right)
						{
							dic[l2[n]] = l1[l];
							break;
						}
						l++;
					}
				}
			}
			return dic;
		}

		private int[] FindChild()
		{
			var chltab = new int[_hashesLength];
			var stack = new Stack<int>();
			var lcp = _lcptab;
			var lastId = -1;
			stack.Push(0);
			for (int i = 1; i < _hashesLength; i++)
			{
				while (lcp[i] < lcp[stack.Peek()])
				{
					lastId = stack.Pop();
					if (lcp[i] < lcp[stack.Peek()] && lcp[stack.Peek()] != lcp[lastId])
						chltab[stack.Pop()] = lastId;
				}
				if (lastId != -1)
				{
					chltab[i - 1] = lastId;
					lastId = -1;
				}
				stack.Push(i);
			}
			return chltab;
		}

		private void Next(int[] chltab)
		{
			var lastId = 0;
			//var chltab = new int[_hashesLength];
			var stack = new Stack<int>();
			stack.Push(0);
			for (int i = 1; i < _hashesLength; i++)
			{
				while (_lcptab[i] < _lcptab[stack.Peek()])
					stack.Pop();
				if (_lcptab[i] == _lcptab[stack.Peek()])
				{
					lastId = stack.Pop();
					chltab[lastId] = i;
				}
				stack.Push(i);
			}
		}

		public void ExtLcp()
		{
			var ranking = new int[_hashesLength]; //=1
			var numchild = new int[_hashesLength]; //=0
			for (int i = 0; i < ranking.Length; i++)
				ranking[i] = 1;

			var stack = new Stack<int>();
			stack.Push(0);

			var lcp = _lcptab;
			var lastId = 0;

			for (int i = 0; i < _hashesLength; i++)
			{
				while (lcp[i] < lcp[stack.Peek()])
				{
					lastId = stack.Pop();
					numchild[lastId] = ranking[lastId] + 1;
				}
				if (lcp[i] == lcp[stack.Peek()])
					ranking[i] = ranking[stack.Pop()] + 1;
				stack.Push(i);
			}
			stack.Push(_hashesLength - 1);

			for (int i = _hashesLength - 1; i > 0; i--)
			{
				while (lcp[i] < lcp[stack.Peek()])
					stack.Pop();
				if (lcp[i] == lcp[stack.Pop()])
					numchild[i] = numchild[stack.Pop()];
			}
		}

		/// <summary>
		/// Хештаблица для lcp.
		/// </summary>
		/// <returns></returns>
		private Dictionary<HashKey, LcpValue> BuildCldTable()
		{
			var dic = new Dictionary<HashKey, LcpValue>(); //((int) (_hashesLength*2.5));
			var lenght = _lcptab.Length;

			var stack = new Stack<LcpTree>();
			var lastInterval = LcpTree.NotDefinedInterval();

			stack.Push(LcpTree.NotDefinedInterval());

			for (int i = 1; i < lenght; i++)
			{
				var leftBound = i - 1;
				while (_lcptab[i] < stack.Peek().Lcp)
				{
					stack.Peek().RightBound = i - 1;
					lastInterval = stack.Pop();
					AddInterval(lastInterval, dic);
					leftBound = lastInterval.LeftBound;
					if (_lcptab[i] <= stack.Peek().Lcp)
					{
						stack.Peek().ChildList.Add(lastInterval);
						lastInterval = LcpTree.NotDefinedInterval();
					}
				}
				if (_lcptab[i] > stack.Peek().Lcp)
				{
					if (lastInterval.RightBound != NotDefined)
					{
						stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree> { lastInterval }));
						lastInterval = LcpTree.NotDefinedInterval();
					}
					else
						stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree>()));
				}
			}

			var root = stack.Pop();
			root.RightBound = lenght - 1;
			root.Lcp = 0;

			AddInterval(root, dic);
			return dic;
		}

		#endregion


		/// <summary> 
		/// Сортирует интервалы, и возвращает порядок отсортированных значений.
		/// ToDo Переписать как только так сразу. ( или возможно у C# есть что-то более подходящее)
		/// </summary>
		/// <param name="arrayList"></param>
		/// <returns></returns>
		private int[][] SortLcpIntervals(List<Interval>[] arrayList)
		{
			var length = arrayList.Length;
			var res = new int[length][];

			for (int i = 0; i < length; i++)
			{
				var l = arrayList[i].Count;
				res[i] = new int[l];
				for (int j = 0; j < l; j++)
					res[i][j] = j;

				Array.Sort(arrayList[i].ToArray(), res[i], new IntervalComparer());
				arrayList[i].Sort(new IntervalComparer());
			}
			return res;
		}

		private const int NotDefined = int.MaxValue;



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


			public static LcpTree NotDefinedInterval()
			{
				return new LcpTree(0, 0, NotDefined, new List<LcpTree>());
			}
		}

		public struct Interval : IComparable<Interval>, IEquatable<Interval>
		{
			public readonly int Left;
			public readonly int Right;

			public Interval(int leftBound, int rightBound)
			{
				Left = leftBound;
				Right = rightBound;
			}


			public int CompareTo(Interval other)
			{
				return Left.CompareTo(other.Left);
			}

			public bool Equals(Interval o)
			{
				return o.Left == Left && o.Right == Right;
			}

			public override string ToString()
			{
				return Left + " - " + Right;
			}

			public override int GetHashCode()
			{
				return (Left + (Right << 16));
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
}
