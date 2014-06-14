using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MotifSeeker.Sfx
{
	public class SuffixArray
	{
		private readonly byte[] _hashes;
		private readonly int _hashesLength;

		private readonly uint[] _suftab;
		private readonly int[] _inversSuftab;
		private readonly int[] _lcptab;
		private readonly Dictionary<HashKey, LcpValue> _cldtab;
		private readonly Dictionary<Interval, LcpInterval> _linktab;

		private readonly List<ElementGroup> _elementGroups;
		private readonly int _minGroupSize;

		public List<ElementGroup> GetElementGroups()
		{
			return _elementGroups;
		}

		public int StrokeSize { get { return _hashesLength; } }

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

		public SuffixArray(byte[] hashes, int minGroupSize = 0)
		{
			_minGroupSize = minGroupSize;
			if(minGroupSize > 0)
				_elementGroups = new List<ElementGroup>();
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

        private const int NotDefinedRigthBound = int.MaxValue;

		/// <summary>
		/// Дерево на основе lcp интервалов.
		/// 
		/// Михаил Корольков: 
		/// Смотри
		/// lcptab - это просто некоторая функция, заданная в целых точках какого-то отрезка
		/// мы можем "сечь" эту функцию горизонтальными прямыми
		/// и нас интересуют непрерывные интервалы, получающиеся при таких рассечениях
		/// потому что такие интервалы характеризуют довольно "глубокие" вершины  в суф. дереве
		/// вот предположим мы с какого-то момента читаем lcp-массив.
		/// Читаем следующие числа:    0 2  10 11 146 3 1
		/// Фактически, вот этот участок будет результатом ккого-то сечения функции горизонтальной прямой:
		/// 2  10 11 146 3
		/// потому что внутри него все значения большие, а по краям меньше
		/// Павел: 10 11 146 тоже
		/// Павел:  11 146
		/// Да.
		/// и это тоже
		/// но явным образом сечь функцию всеми горизонтальными прямыми тупо и долго
		/// и здесь моделируется хитрая штука на стеке
		/// ведь на самом деле довольно понятно вот чо
		/// пусть мы все прочитанные числа закидываем в стек
		/// ПОКА эти данные монотонно не убывают
		/// то есть вот на этом участке
		///   0 2  10 11 146
		/// мы только заполняли стек, ничего оттуда не вытаскивали
		/// и внезапно приходит число, которое меньше последнего прочитанного
		/// а именно, 3, в моем примере
		/// что это означает? Это значит, что у функции сначала был рост монотонный, а потом внезапно пошел спад. ЭТо значит, что где-то между началом этого "роста", и точкой, в которой пошел спад, есть локальный максимум
		/// в совокупности с теоремкой, что лсп интервалы бывают либо вложенными, либо не пересекающимися, это дает следующий факт
		/// вот нам пришло число 3
		/// и мы начинаем с вершины стека выталкивать все элементы, большие трех
		/// потому что они гарантированно будут принадлежать некоторому лсп-интервалу, который в тройке заканчивается
		/// но мы не знаем, где он начинается. И вот мы выталкиваем со стека все элементы, пока не обнаружим левую границу интервала, правая граница которого (тройка) нам уже известна
		/// бинго! у нас есть лсп интервал
		/// 
		/// но, как я уже сказал, есть проблемка
		/// там более сложная версия используется
		/// которая "алгоритм 4.4"
		/// надо из него лишнее повырезать
		/// в частности, "дочерние" отношения нас совсем не интересуют сейчас
		/// а там каждый элемент стека хранит еще лист его "потомков"
		/// </summary>
		/// <returns></returns>
		private LcpTree BuildLcpTree()
		{
			var lenght = _lcptab.Length;

			var stack = new Stack<LcpTree>();
            var lastInterval = LcpTree.NotDefinedInterval(NotDefinedRigthBound);

            var peek = LcpTree.NotDefinedInterval(NotDefinedRigthBound);
            stack.Push(peek);

			for (int i = 1; i < lenght; i++)
			{
				var leftBound = i - 1;
			    
				while (_lcptab[i] < peek.Lcp)
				{
                    peek.RightBound = i - 1;
					lastInterval = stack.Pop();
					if (_minGroupSize > 0)
						ProcessGroup(lastInterval);// анализ интервала на наличие нужной группы
				    peek = stack.Peek();
					leftBound = lastInterval.LeftBound;
                    if (_lcptab[i] <= peek.Lcp)
					{
                        peek.ChildList.Add(lastInterval);
                        lastInterval = LcpTree.NotDefinedInterval(NotDefinedRigthBound);
					}
				}
                if (_lcptab[i] > peek.Lcp)
				{
					if (lastInterval.RightBound != NotDefined)
					{
						stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree> { lastInterval }));
                        lastInterval = LcpTree.NotDefinedInterval(NotDefinedRigthBound);
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

		private byte[] CutAndCheck(uint startPos, int cnt)
		{
			var ret = new byte[cnt];
			var endPos = startPos + cnt;
			var tmp = new int[4];
			for (uint i = startPos; i < endPos; i++)
			{
				var id = ret[i - startPos] = _hashes[i];
				if (id > 3)
					return null; // совпадение содержит границу отрезков
				tmp[id]++;
			}
		    if (ret.Take(3).All(p => p == 3))// Отбросим все CCC...
		        return null;
			var thr = 2*cnt/3;
			return tmp.Any(p => p >= thr) ? null : ret;
		}

		private void ProcessGroup(LcpTree interval)
		{
			var size = interval.Lcp; // размер совпадения
			var cnt = interval.RightBound - interval.LeftBound + 1; // число совпавших
			if (size < _minGroupSize || size > 100)
				return;
			Debug.Assert(cnt > 1);
			// определим позиции совпавших кусков ДНК
			var pos = new uint[cnt];
			Array.Copy(_suftab, interval.LeftBound, pos, 0, cnt);
			// определим сам совпавший кусок
			var str = CutAndCheck(pos[0], size);
			if (str == null)
				return;
			if (_elementGroups.Count > 0)
			{
				// проверим, не стоит ли слить с предыдущей группой
				var last = _elementGroups.Last();
				if (last.Count < cnt && str.Length < last.Chain.Length && str.SequenceEqual(last.Chain.Take(str.Length)))
				{
					_elementGroups[_elementGroups.Count-1] =new ElementGroup(str, pos);
					return;
				}
			}
			// сформируем группу и сохраним её
			var group = new ElementGroup(str, pos);
			_elementGroups.Add(group);
		}

		private void BuildHashTables(out Dictionary<HashKey, LcpValue> cldtab,
			out Dictionary<Interval, LcpInterval> linkstab)
		{
			var root = BuildLcpTree();
			var dic = new Dictionary<HashKey, LcpValue>(HashesLength * 2);

			var maxLcp = Lcptab.Max() + 1;
			var lcpIntervals = new List<Interval>[maxLcp];


			for (int i = 0; i < maxLcp; i++)
				lcpIntervals[i] = new List<Interval>();

			BuildHashTableCustomStack(root, lcpIntervals, dic);

			cldtab = dic;
			linkstab = BuildSuffixLinks(lcpIntervals);
		}

		/// <summary>
		/// Миша: уже заполнение хеш-таблицы childtab
		/// </summary>
		/// <param name="root"></param>
		/// <param name="lcpIntervals"></param>
		/// <param name="dictionary"></param>
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

        /// <summary>
        /// Построение хеш-таблицы для быстрого перехода по суффиксной ссылке.
        /// </summary>
        private Dictionary<Interval, LcpInterval> BuildSuffixLinks(List<Interval>[] lcpIntervals)
        {
            var dic = new Dictionary<Interval, LcpInterval>();

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
                            _inversSuftab[Suftab[interval.Rigth] + 1]),
                        j++));
                // tempList[i].OrderBy(list => list.Key, new IntervalComparer());
                tempList[i].Sort((x, y) =>
                {
                    if (x.Key.Left > y.Key.Left)
                        return 1;
                    if (x.Key.Left < y.Key.Left)
                        return -1;
                    return 0;
                });
            }

            dic[lcpIntervals[0][0]] = new LcpInterval(lcpIntervals[0][0], 0);

            for (int i = 1; i < maxLcp; i++)
            {
                var l1 = lcpIntervals[i - 1];
                var l2 = lcpIntervals[i];
                var n2 = tempList[i];
                var l = 0;
                foreach (KeyValuePair<Interval, int> t in n2)
                {
                    while (l < l1.Count)
                    {
                        if (t.Key.Left >= l1[l].Left && t.Key.Rigth <= l1[l].Rigth)
                        {
                            dic[l2[t.Value]] = new LcpInterval(l1[l], i - 1);
                            break;
                        }
                        l++;
                    }
                }
            }

            return dic;
        }


        /// <summary>
        /// Поиск подстроки. ( Спуск по суффиксному дереву )
        /// </summary>
        // ToDo: этот метод не должен принимать depth. Данная величина должна быть внутренним состоянием Pointer'а,
        // никто снаружи не должен за ней следить. В частности, TextComparer будет передавать Pointer'у только 
        // очередно
		public bool PointerDown(Pointer pointer, byte hash, int depth)
		{
            if (pointer.Depth == depth)
            {
                var key = new HashKey(hash, pointer.Left, pointer.Right);
                LcpValue value;
                //pointer.LastLeft = pointer.Left;
                //pointer.LastRigth = pointer.Rigth;
                if (!_cldtab.TryGetValue(key, out value))
                    return false;
                // ToDo: Суффиксная ссылка не обновляется, если мы находимся в вершине и не можем пройти дальше по хешу.
                // Таким образом, в TextComparer'е будет использована старая суф. ссылка, что повлечет дополнительное время на спуск до нужной позиции.
                // Проще всего перенести следующие две строчки на пару позиций вверх.
                pointer.LastLeft = pointer.Left;
                pointer.LastRight = pointer.Right;

                pointer.Left = value.Left;
                pointer.Right = value.Right;
                pointer.Depth = value.Lcp;

                return true;
            }
            var id = _suftab[pointer.Left] + depth;
            var res = _hashes[id] == hash;
            return res;
		}

        public SuffixSubstr CreateSuffix(SubPointer p)
        {
            return new SuffixSubstr(_suftab[p.Left], p.SrcId, p.Length);
        }

        public SuffixSubstr CreateSuffixSrc(SubPointer p)
        {
            return new SuffixSubstr(p.SrcId, _suftab[p.Left], p.Length);
        }

        // Метод распространяет каждую из цитат в листе list влево по суф. массиву.
        // См. раздел "Поиск всех цитат со всеми позициями в источнике" в статье "Алгоритмы поиска в суффиксном массиве"
        public void AddSuffixesRightToLeft(SubPointer p, int minSubstrLength, List<SuffixSubstr> list)
        {
            var i = p.Left;
            var lastLen = p.Length;
            var len = Math.Min(lastLen, _lcptab[i]);

            while (len >= minSubstrLength)
            {
                i--;
                list.Add(new SuffixSubstr(_suftab[i], p.SrcId, len));
                lastLen = len;
                len = Math.Min(lastLen, _lcptab[i]);
            }

        }
        // Аналогично методу AddSuffixesRightToLeft, но распространяет вправо.
        public void AddSuffixesLeftToRight(SubPointer p, int minSubstrLength, List<SuffixSubstr> list)
        {
            var i = p.Left + 1;
            var lastLen = p.Length;
            var len = Math.Min(lastLen, _lcptab[i]);

            while (len >= minSubstrLength)
            {
                list.Add(new SuffixSubstr(_suftab[i], p.SrcId, len));
                i++;
                lastLen = len;
                len = Math.Min(lastLen, _lcptab[i]);
            }
        }


        /// <summary>
        /// Переход по суффиксной ссылке
        /// </summary>
        // ToDo: методы UseSuffixLink и PointerDownByLink логично сделать закрытыми, а уже над ними обёртку, которая и будет доступна
        // TextComparer'у. Ведь компарер не обязан следить за тем как именно нужно прыгать по ссылке и потом спускаться вниз.
        // Единственное действие, нужное компареру --- перейти от обработки цитаты axy...z к обработке цитаты xy...z. 
        // В документации метод UseSuffixLink как раз объединяет эти два
        public void UseSuffixLink(Pointer pointer)
        {
            var interval = new Interval(pointer.LastLeft, pointer.LastRight);
            LcpInterval value;
            if (!_linktab.TryGetValue(interval, out value))
                throw new Exception("Переход по суффиксной ссылке не удался!");

            pointer.LastLeft = value.Left;
            pointer.LastRight = value.Rigth;

            pointer.Left = value.Left;
            pointer.Right = value.Rigth;
            pointer.Depth = value.Lcp;
        }

        /// <summary>
        /// Поиск точки продолжения поиска после перехода по суффиксной ссылке.
        /// Аналогично PointerDown, но совершает быстрый спуск от вершины к вершине, не проверяя все значения на рёбрах.
        /// </summary>
        // ToDo: Спуск возможен всегда (читаем теорию), поэтому метод должен быть void, а не bool. В хеш-таблице всегда содержится нужное значение,
        // при условии её правильного построения.
        // Также данный метод должен внутри себя крутить цикл, проглатывающий ребра, до тех пор, пока не спустится до нужной позиции, а не оставлять эту обязанность компареру.
        // (см. псевдокод в документации).
        // Хеш и глубину передавать не нужно, хороший PointerDownByLink должен и так знать эти параметры.
        // Вообще, нужно объединить два метода UseSuffixLink и PointerDownByLink в один.
        public bool PointerDownByLink(Pointer pointer, byte hash, int depth)
        {
            var key = new HashKey(hash, pointer.Left, pointer.Right);
            LcpValue value;

            if (!_cldtab.TryGetValue(key, out value))
                return false;

            pointer.LastLeft = pointer.Left;
            pointer.LastRight = pointer.Right;

            pointer.Left = value.Left;
            pointer.Right = value.Right;
            pointer.Depth = value.Lcp;
            return true;
        }

        #region Методы, которые возможно пригодятся.

        //private void CreateChildTable()
        //{
        //    var depth = new int[_hashesLength - 1];
        //    var extLcp = new int[_hashesLength - 1];
        //}

        //private int[] FindChild()
        //{
        //    var chltab = new int[_hashesLength];
        //    var stack = new Stack<int>();
        //    var lcp = _lcptab;
        //    var lastId = -1;
        //    stack.Push(0);
        //    for (int i = 1; i < _hashesLength; i++)
        //    {
        //        while (lcp[i] < lcp[stack.Peek()])
        //        {
        //            lastId = stack.Pop();
        //            if (lcp[i] < lcp[stack.Peek()] && lcp[stack.Peek()] != lcp[lastId])
        //                chltab[stack.Pop()] = lastId;
        //        }
        //        if (lastId != -1)
        //        {
        //            chltab[i - 1] = lastId;
        //            lastId = -1;
        //        }
        //        stack.Push(i);
        //    }
        //    return chltab;
        //}

        //private void Next(int[] chltab)
        //{
        //    var lastId = 0;
        //    //var chltab = new int[_hashesLength];
        //    var stack = new Stack<int>();
        //    stack.Push(0);
        //    for (int i = 1; i < _hashesLength; i++)
        //    {
        //        while (_lcptab[i] < _lcptab[stack.Peek()])
        //            stack.Pop();
        //        if (_lcptab[i] == _lcptab[stack.Peek()])
        //        {
        //            lastId = stack.Pop();
        //            chltab[lastId] = i;
        //        }
        //        stack.Push(i);
        //    }
        //}

        //public void ExtLcp()
        //{
        //    var ranking = new int[_hashesLength]; //=1
        //    var numchild = new int[_hashesLength]; //=0
        //    for (int i = 0; i < ranking.Length; i++)
        //        ranking[i] = 1;

        //    var stack = new Stack<int>();
        //    stack.Push(0);

        //    var lcp = _lcptab;
        //    var lastId = 0;

        //    for (int i = 0; i < _hashesLength; i++)
        //    {
        //        while (lcp[i] < lcp[stack.Peek()])
        //        {
        //            lastId = stack.Pop();
        //            numchild[lastId] = ranking[lastId] + 1;
        //        }
        //        if (lcp[i] == lcp[stack.Peek()])
        //            ranking[i] = ranking[stack.Pop()] + 1;
        //        stack.Push(i);
        //    }
        //    stack.Push(_hashesLength - 1);

        //    for (int i = _hashesLength - 1; i > 0; i--)
        //    {
        //        while (lcp[i] < lcp[stack.Peek()])
        //            stack.Pop();
        //        if (lcp[i] == lcp[stack.Pop()])
        //            numchild[i] = numchild[stack.Pop()];
        //    }
        //}

        ///// <summary>
        ///// Хештаблица для lcp.
        ///// </summary>
        ///// <returns></returns>
        //private Dictionary<HashKey, LcpValue> BuildCldTable()
        //{
        //    var dic = new Dictionary<HashKey, LcpValue>(); //((int) (_hashesLength*2.5));
        //    var lenght = _lcptab.Length;

        //    var stack = new Stack<LcpTree>();
        //    var lastInterval = LcpTree.NotDefinedInterval();

        //    stack.Push(LcpTree.NotDefinedInterval());

        //    for (int i = 1; i < lenght; i++)
        //    {
        //        var leftBound = i - 1;
        //        while (_lcptab[i] < stack.Peek().Lcp)
        //        {
        //            stack.Peek().RightBound = i - 1;
        //            lastInterval = stack.Pop();
        //            AddInterval(lastInterval, dic);
        //            leftBound = lastInterval.LeftBound;
        //            if (_lcptab[i] <= stack.Peek().Lcp)
        //            {
        //                stack.Peek().ChildList.Add(lastInterval);
        //                lastInterval = LcpTree.NotDefinedInterval();
        //            }
        //        }
        //        if (_lcptab[i] > stack.Peek().Lcp)
        //        {
        //            if (lastInterval.RightBound != NotDefined)
        //            {
        //                stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree> { lastInterval }));
        //                lastInterval = LcpTree.NotDefinedInterval();
        //            }
        //            else
        //                stack.Push(new LcpTree(_lcptab[i], leftBound, NotDefined, new List<LcpTree>()));
        //        }
        //    }

        //    var root = stack.Pop();
        //    root.RightBound = lenght - 1;
        //    root.Lcp = 0;

        //    AddInterval(root, dic);
        //    return dic;
        //}

		#endregion


        ///// <summary> 
        ///// Сортирует интервалы, и возвращает порядок отсортированных значений.
        ///// ToDo Переписать как только так сразу. ( или возможно у C# есть что-то более подходящее)
        ///// </summary>
        ///// <param name="arrayList"></param>
        ///// <returns></returns>
        //private int[][] SortLcpIntervals(List<Interval>[] arrayList)
        //{
        //    var length = arrayList.Length;
        //    var res = new int[length][];

        //    for (int i = 0; i < length; i++)
        //    {
        //        var l = arrayList[i].Count;
        //        res[i] = new int[l];
        //        for (int j = 0; j < l; j++)
        //            res[i][j] = j;

        //        Array.Sort(arrayList[i].ToArray(), res[i], new IntervalComparer());
        //        arrayList[i].Sort(new IntervalComparer());
        //    }
        //    return res;
        //}

		private const int NotDefined = int.MaxValue;

	}
}
