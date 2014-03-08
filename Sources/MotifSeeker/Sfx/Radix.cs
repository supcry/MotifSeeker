namespace MotifSeeker.Sfx
{
	/// <summary>
	/// Класс для постоения суффиксного массива.
	/// </summary>
	public class Radix
	{
		/// <summary>
		/// Сортировка значений радиксом. Возвращает порядок отсортированных значений, не изменяя исходные данные.
		/// </summary>
		/// <param name="values">Значения для сортировки.</param>
		/// <returns>Порядок отсортированных значений.</returns>
		public static uint[] Sort(uint[] values)
		{
			var length = values.Length;

			var labels = new uint[length * 2];
			var tempLabels = new uint[length * 2];

			var sorted = new uint[length];

			for (uint i = 0; i < length; i++)
			{
				labels[i] = values[i];
				sorted[i] = i;
			}

			RadixSort(ref sorted, labels, uint.MaxValue);
			var maxLabel = ReLabelling(ref labels, ref tempLabels, sorted, 0);
			var p = 1;
			while (maxLabel < length - 1)
			{
				RadixSortPair(ref sorted, labels, maxLabel, p);
				maxLabel = ReLabelling(ref labels, ref tempLabels, sorted, p);
				p *= 2;
			}

			return sorted;

		}


		/// <summary>
		/// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
		/// </summary>
		/// <param name="sorted">Порядок значений</param>
		/// <param name="labels">Значения</param>
		/// <param name="maxLabel">Количество уникальных значений ( в данном случае суффиксов ).</param>
		/// <param name="depth">Смещение в сортируемых значениях (глубина суффикса), по которому происходит сортировка</param>
		public static unsafe void RadixSort(ref uint[] sorted, uint[] labels, uint maxLabel, int depth = 0)
		{
			var length = sorted.Length;
			var bb = 1;
			var resSuftab = new uint[length];

			fixed (uint* pSt = sorted)
			fixed (uint* pLabels = labels)
			fixed (uint* pRst = resSuftab)
			{
				var pSuftab = pSt;
				var pResSuftab = pRst;


				for (int i = 0; i < 4; i++)
				{
					if (maxLabel <= bb - 1)
						break;

					RadixPass(pSuftab, pResSuftab, pLabels, i + depth * sizeof(uint), length);

					var temp = pResSuftab;
					pResSuftab = pSuftab;
					pSuftab = temp;

					bb = bb << 8;
				}
			}
			if (bb >> 8 == 1 || bb >> 24 == 1)
				sorted = resSuftab;
		}

		/// <summary>
		/// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
		/// </summary>
		/// <param name="sorted">Порядок значений</param>
		/// <param name="labels">Значения</param>
		/// <param name="maxLabel">Количество уникальных значений ( в данном случае суффиксов ).</param>
		/// <param name="depth">Смещение в сортируемых значениях (глубина суффикса), по которому происходит сортировка</param>
		public static void RadixSortPair(ref uint[] sorted, uint[] labels, uint maxLabel, int depth)
		{
			RadixSort(ref sorted, labels, maxLabel, depth);
			RadixSort(ref sorted, labels, maxLabel);
		}

		/// <summary>
		/// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
		/// </summary>
		/// <param name="sorted">Исходный порядок</param>
		/// <param name="resSorted">Порядок по отсортированным значеням.</param>
		/// <param name="labels">Сортируемые значения.</param>
		/// <param name="shift">Сдвиг в байтах.</param>
		/// <param name="length">Длина текста.</param>
		private static unsafe void RadixPass(uint* sorted, uint* resSorted, uint* labels, int shift, int length)
		{

			var bytes = (byte*)labels;

			var count = new int[256];
			var index = new int[256];

			//посчитаем количество различных байтов
			//for (int i = 0; i < realLength; i++)
			//{
			//    var idx = sourceSufTab[i] + shift;
			//    var byteIdx = idx * 4 + b;
			//    var val = bytes[byteIdx];
			//    count[val]++;
			//}
			for (int i = 0; i < length; i++)
				count[*(bytes + (*(sorted + i)) * 4 + shift)]++;


			index[0] = 0;
			for (int i = 0; i < 255; i++)
				index[i + 1] = index[i] + count[i];

			for (uint i = 0; i < length; i++)
			{
				//var byteIdx = bytes + ((*(suftab + i))*4 + shift);
				//var tabIdx = index[byteIdx]++;
				//resSuftab[tabIdx] = suftab[i];
				resSorted[index[*(bytes + ((*(sorted + i)) * 4 + shift))]++] = sorted[i];
			}
		}

		/// <summary>
		/// Возвращает количество уникальных суффиксов. 
		/// Перезаписывает исходных значения в соответсвии с их новым порядком ( массив sorted ),
		/// производя сравнение двух соседних суффиксов.
		/// </summary>
		/// <param name="labels">Сортируемые данные</param>
		/// <param name="tempLabels">Временный массив</param>
		/// <param name="sorted">Массив, содержащий порядок значений label</param>
		/// <param name="p">Глубина суффикса</param>
		/// <returns>Количество уникальных суффиксов</returns>
		private static uint ReLabelling(ref uint[] labels, ref uint[] tempLabels, uint[] sorted, int p)
		{
			var length = sorted.Length;
			uint label = 0;
			tempLabels[sorted[0]] = label;
			for (uint i = 1; i < length; i++)
			{
				if (labels[sorted[i] + p] != labels[sorted[i - 1] + p] || labels[sorted[i]] != labels[sorted[i - 1]])
					label++;
				tempLabels[sorted[i]] = label;
			}
			var t = labels;
			labels = tempLabels;
			tempLabels = t;

			return label;
		}
	}
}