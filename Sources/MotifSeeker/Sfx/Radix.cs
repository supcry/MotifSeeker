using System;

namespace MotifSeeker.Sfx
{
    /// <summary>
    /// [ToDo] Переписать для случая замены uint->byte
    /// </summary>
    public class Radix
    {
        public uint[] Sort(uint[] sourceSufTab, uint[] labels, int realLength, int shift)
        {
            var bytesLength = labels.Length * sizeof(uint);
            var bytes = new byte[bytesLength];
            Buffer.BlockCopy(labels, 0, bytes, 0, bytesLength);

            var resSufTab = new uint[realLength];


            if (shift != 0)
                for (byte b = 0; b < 4; b++)
                {
                    SortByByte(sourceSufTab, resSufTab, bytes, realLength, b, shift);
                    var temp = resSufTab;
                    resSufTab = sourceSufTab;
                    sourceSufTab = temp;
                }

            for (byte b = 0; b < 4; b++)
            {
                SortByByte(sourceSufTab, resSufTab, bytes, realLength, b, 0);
                var temp = resSufTab;
                resSufTab = sourceSufTab;
                sourceSufTab = temp;
            }

            return sourceSufTab;
        }

        public void SortByByte(uint[] sourceSufTab, uint[] resSufTab, byte[] bytes, int realLength, byte b, int shift)
        {
            var count = new int[256];
            var index = new int[256];

            //посчитаем количество различных байтов
            for (int i = 0; i < realLength; i++)
            {
                var idx = sourceSufTab[i] + shift;
                var byteIdx = idx * 4 + b;
                var val = bytes[byteIdx];
                count[val]++;
            }
            //затем их смещения
            index[0] = 0;
            for (int i = 0; i < 255; i++)
                index[i + 1] = index[i] + count[i];

            //а теперь запишем новый порядок.
            for (uint i = 0; i < realLength; i++)
            {
                var idx = sourceSufTab[i] + shift;
                var byteIdx = idx * 4 + b;
                var val = bytes[byteIdx];
                var ofs = index[val]++;
                resSufTab[ofs] = sourceSufTab[i];
            }
        }

        /// <summary>
        /// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
        /// </summary>
        /// <param name="suftab">Суффиксный массив</param>
        /// <param name="labels">Хеши хешей.</param>
        /// <param name="maxLabel">Количество различных хешей.</param>
        /// <param name="depth">Глубина суффикса, по которой происходит сортировка</param>
        public static unsafe void RadixSort(ref uint[] suftab, uint[] labels, uint maxLabel, int depth = 0)
        {
            var length = suftab.Length;
            var bb = 1;
            var resSuftab = new uint[length];

            fixed (uint* pSt = suftab)
            fixed (uint* pLabels = labels)
            fixed (uint* pRst = resSuftab)
            {
                var pSuftab = pSt;
                var pResSuftab = pRst;


                for (int i = 0; i < 4; i++)
                {
                    if (maxLabel <= bb - 1)
                        break;

                    RadixPass(pSuftab, pResSuftab, pLabels, i + depth * 4, length);

                    var temp = pResSuftab;
                    pResSuftab = pSuftab;
                    pSuftab = temp;

                    bb = bb << 8;
                }
            }
            if (bb >> 8 == 1 || bb >> 24 == 1)
                suftab = resSuftab;
        }

        /// <summary>
        /// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
        /// </summary>
        /// <param name="suftab">Суффиксный массив</param>
        /// <param name="labels">Хеши хешей.</param>
        /// <param name="maxLabel">Количество различных хешей.</param>
        /// <param name="depth">Глубина суффикса, по которой происходит сортировка</param>
        public static void RadixSortPair(ref uint[] suftab, uint[] labels, uint maxLabel, int depth)
        {
            RadixSort(ref suftab, labels, maxLabel, depth);
            RadixSort(ref suftab, labels, maxLabel);
        }

        public static uint ReLabelling(int hashesLength, ref uint[] labels, uint[] suftab, int p, ref uint[] tempLabels)
        {
            uint label = 0;
            tempLabels[suftab[0]] = label;
            for (uint i = 1; i < hashesLength; i++)
            {
                if (labels[suftab[i] + p] != labels[suftab[i - 1] + p] || labels[suftab[i]] != labels[suftab[i - 1]])
                    label++;
                tempLabels[suftab[i]] = label;
            }

            var t = labels;
            labels = tempLabels;
            tempLabels = t;

            return label;
        }

        /// <summary>
        /// Сортировка суффиксов исходного текста. Суффиксы задаются индексами их начала.
        /// </summary>
        /// <param name="suftab">Исходный суффиксный массив.</param>
        /// <param name="resSuftab">Результат сортировки.</param>
        /// <param name="labels">Хеши.</param>
        /// <param name="shift">Сдвиг в байтах.</param>
        /// <param name="length">Длина текста.</param>
        private static unsafe void RadixPass(uint* suftab, uint* resSuftab, uint* labels, int shift, int length)
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
                count[*(bytes + (*(suftab + i)) * 4 + shift)]++;


            index[0] = 0;
            for (int i = 0; i < 255; i++)
                index[i + 1] = index[i] + count[i];

            for (uint i = 0; i < length; i++)
            {
                //var byteIdx = bytes + ((*(suftab + i))*4 + shift);
                //var tabIdx = index[byteIdx]++;
                //resSuftab[tabIdx] = suftab[i];
                resSuftab[index[*(bytes + ((*(suftab + i)) * 4 + shift))]++] = suftab[i];
            }
        }


    }
}