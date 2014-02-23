using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MotifSeeker.Helpers
{
    /// <summary>
    /// Реализация быстрого словаря без возможности вставки/удаления, но с минимальным расходом по памяти (не более 2х int'ов на пару key-value).
    /// </summary>
    /// <typeparam name="TKey">Ключ пары. Хэш ключа должен быть достаточно хорош.</typeparam>
    /// <typeparam name="TValue">Значение пары.</typeparam>
    public sealed class StaticDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly int _ofsBitsCount;
        private readonly int[] _ofs;       // таблица смещений в массиве хэшей
        private readonly uint[] _hashes;    // хэши ключей (отсортированы по возрастанию)
        private readonly TKey[] _keys;     // ключи (сортировка соответствует _hashes)
        private readonly TValue[] _values; // значения (сортировка соответствует _keys)

        public StaticDictionary(ICollection<KeyValuePair<TKey, TValue>> pairs)
        {
            // Пока предполагаем, что дубликатов нет.
            _hashes = new uint[pairs.Count];
            _keys = new TKey[pairs.Count];
            _values = new TValue[pairs.Count];
            int i = 0;
            _ofsBitsCount = GetOfsBitsCount(pairs.Count);
            _ofs = new int[(int)Math.Round(Math.Pow(2, _ofsBitsCount))];
            int lastOfs = 0;

            //foreach (var t in pairs.Select(p => new Tuple<uint, TKey, TValue>((uint)p.Key.GetHashCode(), p.Key, p.Value)).OrderBy(p => p.Item1))
            //{
            //    _hashes[i] = t.Item1;
            //    _keys[i] = t.Item2;
            //    _values[i] = t.Item3;
                
            //    var ofs = (int)(t.Item1 >> (sizeof(uint) * 8 - _ofsBitsCount));
            //    if (lastOfs != ofs)
            //    {
            //        for (int j = lastOfs + 1; j <= ofs; j++)
            //            _ofs[j] = i;
            //        lastOfs = ofs;
            //    }
            //    i++;
            //}

            foreach (var t in pairs.Select(p => new KeyValuePair<uint, KeyValuePair<TKey, TValue>>((uint)p.Key.GetHashCode(), p)).OrderBy(p => p.Key))
            {
                _hashes[i] = t.Key;
                _keys[i] = t.Value.Key;
                _values[i] = t.Value.Value;

                var ofs = (int)(t.Key >> (sizeof(uint) * 8 - _ofsBitsCount));
                if (lastOfs != ofs)
                {
                    for (int j = lastOfs + 1; j <= ofs; j++)
                        _ofs[j] = i;
                    lastOfs = ofs;
                }
                i++;
            }

            for (int j = lastOfs + 1; j < _ofs.Length; j++)
                _ofs[j] = -1;
        }

        private int GetKeyId(TKey key)
        {
            var hash = (uint)key.GetHashCode();
            var ofs = (int)(hash >> (sizeof(uint) * 8 - _ofsBitsCount));
            int id = _ofs[ofs];
            if (id == -1)
                return -1;
            while (_hashes[id] < hash)
                id++;
            while (_hashes[id] == hash)
            {
                if (_keys[id].Equals(key))
                    return id;
                id++;
            }
            return -1;
        }

        /// <summary>
        /// Сколько бит будет задействовано при работе с таблицей _ofs
        /// </summary>
        /// <param name="dicSize"></param>
        /// <returns></returns>
        private static int GetOfsBitsCount(int dicSize)
        {
            var part = dicSize;
            int bitsCount = 0;
            while ((1 << bitsCount) < part)
                bitsCount++;
            Debug.Assert(bitsCount > 0);
            return bitsCount;
        }



        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerable()
        {
            return _keys.Select((t, i) => new KeyValuePair<TKey, TValue>(t, _values[i]));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue ret;
            if (TryGetValue(item.Key, out ret))
                return item.Equals(ret);
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public int Count { get { return _keys.Length; } }
        public bool IsReadOnly { get { return true; } }
        public bool ContainsKey(TKey key)
        {
            return GetKeyId(key) != -1;
        }

        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotSupportedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var id = GetKeyId(key);
            if (id == -1)
            {
                value = default(TValue);
                return false;
            }
            value = _values[id];
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                var hash = (uint)key.GetHashCode();
                var ofs = (int)(hash >> (sizeof(uint) * 8 - _ofsBitsCount));
                int id = _ofs[ofs];
                if (id != -1)
                {
                    while (_hashes[id] < hash)
                        id++;
                    while (_hashes[id] == hash)
                    {
                        if (_keys[id].Equals(key))
                            return _values[id];
                        id++;
                    }
                }
                throw new KeyNotFoundException();;
            }
            set { throw new NotSupportedException(); }
        }

        public ICollection<TKey> Keys { get { return _keys; } }
        public ICollection<TValue> Values { get { return _values; } }
    }
}
