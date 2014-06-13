using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Ga
{
    public class Spice : IEquatable<Spice>, IComparable<Spice>
    {
        #region globalId
        private static int _globalId;

        private static int GetNextId()
        {
            return Interlocked.Increment(ref _globalId);
        }
        #endregion globalId

        public static readonly HashSet<int> HashUniq = new HashSet<int>();

        public readonly int Id;

        private readonly Nucleotide[] _data;

        private readonly byte[][] _rawData;

        private readonly int _hash;

        public Score Score;

        public Spice(int len, int seed)
        {
            var rnd = new Random(seed);
            _data = Enumerable.Repeat(1, len).Select(p => (Nucleotide) rnd.Next(4)).ToArray();
            _hash = _data.Length > 16 ? _data.Aggregate(13, (a, b) => a * 13 + (int)b) : _data.Aggregate(0, (a, b) => (a << 2) + (int)b);
            if (CheckIsFailed(_data, _hash))
            {
                Score = Score.Failed;
                return;
            }
            
            Id = GetNextId();
            
            _rawData = GetRawData(_data);
        }

        private Spice(Nucleotide[] data)
        {
            _data = data;
            _hash = _data.Length > 16 ? _data.Aggregate(13, (a, b) => a * 13 + (int)b) : _data.Aggregate(0, (a, b) => (a << 2) + (int)b);
            if (CheckIsFailed(data, _hash))
            {
                Score = Score.Failed;
                return;
            }
            Id = GetNextId();
            
            _rawData = GetRawData(_data);
        }


        private static byte[][] GetRawData(Nucleotide[] ns)
        {
            var ret = new byte[4][];
            int i = 0;
            foreach (Direction direction in Enum.GetValues(typeof (Direction)))
                ret[i++] = ns.GetChain(direction).Select(p => p.ToByte()).ToArray();
            return ret;
        }

        public byte[][] RawData
        {
            get { return _rawData; }
        }

        private static bool CheckIsFailed(Nucleotide[] data, int hash)
        {
            if (HashUniq.Contains(hash))
                return true;
            HashUniq.Add(hash);

            var thr = 2*data.Length/3;
            var tmp = new int[4];
            data.ForEach(p => tmp[(int)p]++);

            if (tmp.Any(p => p > thr))
                return true;

            // GGCCGGCCGGC
            // CGGCCGGCCGG
            // p = 406, n = 218, inf=33
            if (tmp[2] + tmp[3] == data.Length || tmp[0] + tmp[1] == data.Length)
                return true;

            // CCCCGACGC
            // CGCAGCCCC
            // p = 15, n = 0, inf=10

            return false;
        }


        public Spice MutateByOrder(float order, int seed)
        {
            var tmp = new Nucleotide[_data.Length];
            Array.Copy(_data, tmp, tmp.Length);
            var rnd = new Random(seed);
            foreach(var i in rnd.GetShuffleFlow(Math.Max((int) Math.Round(order*_data.Length), 1)))
            {
                var last = tmp[i];
                var next = (Nucleotide)rnd.Next(4);
                while(next == last)
                    next = (Nucleotide)rnd.Next(4);
                tmp[i] = next;
            }
            return new Spice(tmp);
        }

        public Spice MutateByCount(int cnt, int seed)
        {
            var tmp = new Nucleotide[_data.Length];
            Array.Copy(_data, tmp, tmp.Length);
            var rnd = new Random(seed);
            foreach (var i in rnd.GetShuffleFlow(Math.Max(cnt, 1)))
            {
                var last = tmp[i];
                var next = (Nucleotide)rnd.Next(4);
                while (next == last)
                    next = (Nucleotide)rnd.Next(4);
                tmp[i] = next;
            }
            return new Spice(tmp);
        }

        public static Spice Cross(int seed, Spice sp1, Spice sp2)
        {
            // [ToDo] Можно значительно ускорить. Пока только симметричный случай.
            var tmp = new Nucleotide[sp1._data.Length];
            var rnd = new Random(seed);
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = rnd.Next(2) == 0 ? sp1._data[i] : sp2._data[i];
            return new Spice(tmp);
        }

        public bool Equals(Spice other)
        {
            if (Id == other.Id)
                return true;
            return _data.Zip(other._data, (a, b) => new {a, b}).All(p => p.a == p.b);
        }

        
        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return Id + ": " + string.Join("", _data.Select(p => p.ToString())) + (Score == null ? "" : " "+ Score);
        }

        public int CompareTo(Spice other)
        {
            if (Score != null && other.Score != null)
            {
                var ret = Score.CompareTo(other.Score);
                if (ret != 0)
                    return ret;
                return _hash.CompareTo(other._hash);
            }
            throw new NotSupportedException("Нельзя определять порядок у особей, которые пока не оценены.");
        }

        public bool IsFailed { get { return Score != null && Score.IsFailed; } }
    }
}
