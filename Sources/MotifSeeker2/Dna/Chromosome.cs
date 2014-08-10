using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MotifSeeker2.Helpers;
using ProtoBuf;

namespace MotifSeeker2.Dna
{
    /// <summary>
    /// Хромосома в компактном представлении. 2 бита на нуклеотид.
    /// </summary>
    [DataContract]
    public class Chromosome
    {
        [DataMember(Order = 1)]
        public int Count { get; private set; }

        [DataMember(Order = 2)]
        public ChromosomeEnum ChromosomeId { get; private set; }

        [DataMember(Order = 3)]
        private byte[] _data;

        private byte[] _mask;

        [DataMember(Order = 4)]
        private int[] _maskSizes;

        private Chromosome() { }

        private Chromosome(ChromosomeEnum id) : this()
        {
            ChromosomeId = id;
            _mask = new byte[0];
            _data = new byte[0];
        }

        private void MaskToSizes()
        {
            var sizes = new List<int>();
            var last = false;
            for (int i = 0; i < Count; i++)
            {
                var b = ((_mask[i/8] >> (i%8)) & 1) == 1;
                if (b != last)
                {
                    last = b;
                    sizes.Add(i);
                }
            }
            _maskSizes = sizes.ToArray();
        }

        private void SizesToMask()
        {
            _mask = new byte[Count];
            int id = 0;
            bool last = false;
            foreach (var s in _maskSizes.Concat(new[]{Count}))
            {
                if (last)
                {
                    // пустые заполнять не надо
                    last = false;
                    id = s;
                    continue;
                }
                
                while (id%8 != 0)
                {
                    _mask[id/8] |= (byte) (1 << (id%8));
                    id++;
                }
                while (s - id >= 8)
                {
                    _mask[id/8] = 0xFF;
                    id += 8;
                }
                while (id <= s - 1)
                {
                    _mask[id / 8] |= (byte)(1 << (id % 8));
                    id++;
                }
                last = true;
            }
        }

        public Nucleotide[] GetPack(int start, int count)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(start + count <= Count);

            var ret = new Nucleotide[count];
            if (count == 0)
                return ret;
            var byteId = start / 4;
            var lastByteId = (start + count) / 4;
            var packId = start % 4;
            var b = _data[byteId];
            var cnt = 0;
            for (int i = packId; i < 4; i++)
            {
                ret[cnt] = (Nucleotide)((b >> (2 * (i))) & 0x03);
                if (++cnt == count)
                    break;
            }
            byteId++;

            for (; byteId <= lastByteId && cnt != count; byteId++)
            {
                b = _data[byteId];
                for (int i = 0; i < 4; i++)
                {
                    ret[cnt] = (Nucleotide)(b & 0x03);
                    if (++cnt == count)
                        break;
                    b = (byte)(b >> 2);
                }
            }

            // добавим маску
            for (int i = 0; i < ret.Length; i++)
            {
                var mId = start + i;
                if(((_mask[mId/8] >> (mId%8)) & 1) != 1)
                    ret[i] = Nucleotide.Any;
            }

            return ret;
        }

        public void AddRange(ICollection<Nucleotide> items)
        {
            var nbc = (Count + items.Count + 3) / 4;
            if (nbc > _data.Length)
                Array.Resize(ref _data, Math.Max(nbc, _data.Length*2));

            var byteId = Count / 4;
            var packId = Count % 4;
            var curByte = _data[byteId];
            int skipHeader;
            if (packId > 0)
            {

                var itemHeader = items.Take(4 - packId).ToArray();
                skipHeader = itemHeader.Length;
                for (int i = packId; i < 4 && i < itemHeader.Length + packId; i++)
                {
                    var shift = 2 * i;
                    curByte = (byte)((curByte & ~(0x03 << shift)) | ((int)itemHeader[i - packId] << shift));
                }
                _data[byteId] = curByte;
                Count += skipHeader;
                byteId++;
                packId = 0;
            }
            else
                skipHeader = 0;

            byte tmp = 0;
            foreach (var item in items.Skip(skipHeader))
            {
                var shift = 2 * packId;
                tmp = (byte)(tmp | ((int)item << shift));
                Count++;
                packId = (packId + 1) % 4;
                if (packId == 0)
                    _data[byteId++] = tmp;
            }
            if (packId != 0)
                _data[byteId] = tmp;

            // допишем маску. 1 - есть, 0 - нет.
            var mbc = (Count + items.Count + 7) / 8;
            if (mbc > _mask.Length)
                Array.Resize(ref _mask, Math.Max(mbc, _mask.Length * 2));

            int pos = Count - items.Count;
            foreach (var item in items)
            {
                if (item != Nucleotide.Any)
                    _mask[pos/8] |= (byte)(1 << (pos%8));
                pos++;
            }
            _maskSizes = null;
        }

        /// <summary>
        /// Удаляет лишние байты в хвосте.
        /// </summary>
        public void Compact()
        {
            var bcnt = (Count + 3) / 4;
            Debug.Assert(bcnt <= _data.Length);
            if (bcnt != _data.Length)
                Array.Resize(ref _data, bcnt);

            var mbc = (Count + 7) / 8;
            if (mbc != _mask.Length)
                Array.Resize(ref _mask, mbc);

        }

        #region (de-)serialize
        public long Serialize(string path)
        {
            var sw = Stopwatch.StartNew();
            Compact();
            MaskToSizes();
            using (var s = File.Create(path, 1024 * 1024 * 10))
            {
                s.SetLength(0);
                Serializer.Serialize(s, this);
                s.Flush();
                sw.Stop();
                Logs.Instance.Trace(ChromosomeId + " serialized, dt=" + sw.Elapsed.ToHuman() + ", len=" + s.Length.ToHuman());
                return s.Length;
            }
        }

        public static Chromosome Deserialize(string path)
        {
            var sw = Stopwatch.StartNew();
            using (var f = File.OpenRead(path))
            {
                var ret = Serializer.Deserialize<Chromosome>(f);
                ret.SizesToMask();
                Logs.Instance.Info(ret.ChromosomeId + " deserialized, dt=" + sw.Elapsed.ToHuman() + ", len=" + f.Length.ToHuman());
                return ret;
            }
        }
        #endregion (de-)serialize

        #region manager

        public static Chromosome GetChromosome(ChromosomeEnum chId)
        {
            const string srcBaseRoot = @"http://hgdownload.soe.ucsc.edu/goldenPath/hg19/chromosomes/";
            const string dataFolder = "chromosomes";
            var chIdStr = chId.ToString().ToLower();
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
            var protoPath = Path.Combine(dataFolder, chIdStr + ".proto");
            if (File.Exists(protoPath)) // если уже есть заготовка, то берём её и не паримся
                return Deserialize(protoPath);
            // ищем локальную копию или подгружаем из инета
            var fazName = chIdStr + ".fa.gz";
            var d = new Downloader(srcBaseRoot + fazName, dataFolder);
            var faPath = d.Get();
            // вычитываем в своё представление
            var sw = Stopwatch.StartNew();
            var faReader = new FastaReader(faPath);
            var chr = new Chromosome(chId);
            foreach (var f in faReader.ReadFlow())
                chr.AddRange(f.Select(ToNucleotide).ToArray());
            Debug.Assert(faReader.Description == chIdStr);

            Logs.Instance.Trace("FASTA to Chromosome converted, dt=" + sw.Elapsed.ToHuman() + ", " + chr.Count.ToHuman("pairs"));
            chr.Serialize(protoPath);// сериализуем в свою заготовку на будущее
            return chr;
        }

        private static Nucleotide ToNucleotide(FastaReader.FastaItem item)
        {
            switch (item)
            {
                case FastaReader.FastaItem.A:
                case FastaReader.FastaItem.Alow:
                    return Nucleotide.A;
                case FastaReader.FastaItem.T:
                case FastaReader.FastaItem.Tlow:
                    return Nucleotide.T;
                case FastaReader.FastaItem.G:
                case FastaReader.FastaItem.Glow:
                    return Nucleotide.G;
                case FastaReader.FastaItem.C:
                case FastaReader.FastaItem.Clow:
                    return Nucleotide.C;
                case FastaReader.FastaItem.Any:
                    return Nucleotide.Any;
                default:
                    throw new NotSupportedException();
            }
        }


        #endregion manager

    }
}
