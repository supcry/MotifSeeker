using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Data.DNaseI
{
    /// <summary>
    /// Модель данных результатов экспериментов из http://genome.ucsc.edu/cgi-bin/hgFileUi?db=hg19\&g=wgEncodeUwDnase
    /// для Hotspots и Peaks.
    /// </summary>
    [DataContract]
    public class SensitivityResults
    {
        /// <summary>
        /// Если не null, значит данные заданы только для выбранной хромосомы.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly ChromosomeEnum? Chr;
        
        /// <summary>
        /// Данные эксперимента парами ключ-значение.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly KeyValuePair<string, string>[] Params;

        /// <summary>
        /// Сами данные.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly Item[] Items;

        public SensitivityResults(string fpath, KeyValuePair<string, string>[] pars = null, ChromosomeEnum? onlyChr = null)
        {
            Items = File.ReadLines(fpath)
                        .Select(p => new Item(p))
                        .Where(p => !onlyChr.HasValue || p.Chr == onlyChr.Value)
                        .ToArray();
            Chr = onlyChr;
            Params = pars;
        }

        public class Item
        {
            /// <summary>
            /// Для какой хромосомы эта строка.
            /// </summary>
            public readonly ChromosomeEnum Chr;

            /// <summary>
            /// Начало региона (с 0)
            /// </summary>
            public readonly int StartPos;

            /// <summary>
            /// Конец региона (с 0)
            /// </summary>
            public readonly int EndPos;

            /// <summary>
            /// Первое значение (?)
            /// </summary>
            public readonly float Value1;

            /// <summary>
            /// Второе значение (?)
            /// </summary>
            public readonly float Value2;

            public Item(ChromosomeEnum chr, int start, int end, float v1, float v2)
            {
                Chr = chr;
                StartPos = start;
                EndPos = end;
                Value1 = v1;
                Value2 = v2;
            }

            public Item(string line)
            {
                //chr1	566760	566910	.	0	.	339	157.884	-1	-1
                var parts = line.Split('\t');
                Debug.Assert(parts.Length >= 9);
                Chr = (ChromosomeEnum)Enum.Parse(typeof(ChromosomeEnum), parts[0], true);
                StartPos = int.Parse(parts[1]);
                EndPos = int.Parse(parts[2]);
                Value1 = float.Parse(parts[6], new NumberFormatInfo { CurrencyDecimalSeparator = "." });
                Value2 = float.Parse(parts[7], new NumberFormatInfo { CurrencyDecimalSeparator = "." });
            }

            public Item() { }
        }
    }
}
