using System;
using System.Collections.Generic;
using System.Linq;

namespace MotifSeeker2.Helpers
{
    /// <summary>
    /// Коллекция расширений над коллекциями.
    /// </summary>
    public static class LinqExt
    {
        #region firstWhere
        public static T FirstWhereMax<T>(this IEnumerable<T> lst, Func<T, int> f)
        {
            var data = default(T);
            var val = 0;
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v <= val && get) continue;
                get = true;
                val = v;
                data = item;
            }
            if(!get)
                throw new Exception("Коллекция пуста");
            return data;
        }

        public static T FirstWhereMaxOrDefault<T>(this IEnumerable<T> lst, Func<T, int> f)
        {
            var data = default(T);
            if (lst == null)
                return data;
            var val = 0;
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v <= val && get) continue;
                get = true;
                val = v;
                data = item;
            }
            return data;
        }

        public static T FirstWhereMax<T,TV>(this IEnumerable<T> lst, Func<T, TV> f) where TV: IComparable
        {
            var data = default(T);
            var val = default(TV);
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v.CompareTo(val) <= 0 && get) continue;
                get = true;
                val = v;
                data = item;
            }
            if (!get)
                throw new Exception("Коллекция пуста");
            return data;
        }

        public static T FirstWhereMaxOrDefault<T, TV>(this IEnumerable<T> lst, Func<T, TV> f) where TV : IComparable
        {
            var data = default(T);
            if (lst == null)
                return data;
            var val = default(TV);
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v.CompareTo(val) <= 0 && get) continue;
                get = true;
                val = v;
                data = item;
            }
            return data;
        }

        public static T FirstWhereMin<T>(this IEnumerable<T> lst, Func<T, int> f)
        {
            var data = default(T);
            var val = 0;
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v >= val && get) continue;
                get = true;
                val = v;
                data = item;
            }
            if (!get)
                throw new Exception("Коллекция пуста");
            return data;
        }

        public static T FirstWhereMinOrDefault<T>(this IEnumerable<T> lst, Func<T, int> f)
        {
            var data = default(T);
            if (lst == null)
                return data;
            var val = 0;
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v >= val && get) continue;
                get = true;
                val = v;
                data = item;
            }
            return data;
        }

        public static T FirstWhereMin<T,TV>(this IEnumerable<T> lst, Func<T, TV> f) where TV: IComparable
        {
            var data = default(T);
            var val = default(TV);
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v.CompareTo(val) < 0 || !get)
                {
                    get = true;
                    val = v;
                    data = item;
                }
            }
            if (!get)
                throw new Exception("Коллекция пуста");
            return data;
        }

        public static T FirstWhereMinOrDefault<T, TV>(this IEnumerable<T> lst, Func<T, TV> f) where TV : IComparable
        {
            var data = default(T);
            if (lst == null)
                return data;
            var val = default(TV);
            var get = false;
            foreach (var item in lst)
            {
                var v = f(item);
                if (v.CompareTo(val) < 0 || !get)
                {
                    get = true;
                    val = v;
                    data = item;
                }
            }
            return data;
        }
        #endregion firstWhere

        public static int FirstIndexWhereMin<T, TV>(this T[] lst, Func<T, TV> f) where TV : IComparable
        {
            TV val = default(TV);
            bool get = false;
            int id = 0;
            for (int i = 0; i < lst.Length; i++)
            {
                var item = lst[i];
                var v = f(item);
                if (v.CompareTo(val) < 0 || !get)
                {
                    get = true;
                    val = v;
                    id = i;
                }
            }
            if (!get)
                throw new Exception("Коллекция пуста");
            return id;
        }

        #region enumerate
        public static IEnumerable<List<T>> EnumerateByPacks<T>(this IEnumerable<T> data, int packSize)
        {
            var pack = new List<T>(packSize);
            foreach(var t in data)
            {
                pack.Add(t);
                if(pack.Count == packSize)
                {
                    yield return pack;
                    pack = new List<T>(packSize);
                }
            }
            if (pack.Count != 0)
                yield return pack;
        }

        /// <summary>
        /// Разбивает последовательность на пачки. Чётные содержат packSizeEven элементов, нечётные - packSizeOdd.
        /// Нулевая пачка - чётная. Размер последней пачки неопределён.
        /// </summary>
        public static IEnumerable<List<T>> EnumerateByPacks<T>(this IEnumerable<T> data, int packSizeEven, int packSizeOdd)
        {
            bool even = true;
            int curPackSize = packSizeEven;
            var pack = new List<T>(curPackSize);
            foreach (var t in data)
            {
                pack.Add(t);
                if (pack.Count == curPackSize)
                {
                    yield return pack;
                    even = !even;
                    curPackSize = even ? packSizeEven : packSizeOdd;
                    pack = new List<T>(curPackSize);
                }
            }
            if (pack.Count != 0)
                yield return pack;
        }

        public static IEnumerable<T[]> EnumerateGroups<T>(this IEnumerable<IEnumerable<T>> flows)
        {
            var data = flows.Select(p => p.GetEnumerator()).ToList();
            while(true)
            {
                var ret = new T[data.Count];
                bool rem = false;
                for(int i=0;i<ret.Length;i++)
                {
                    if(data[i] == null)
                        continue;
                    if (data[i].MoveNext())
                    {
                        ret[i] = data[i].Current;
                    }
                    else
                    {
                        data[i] = null;
                        rem = true;
                    }
                }
                if (rem)
                    data.RemoveAll(p => p == null);
                if(data.Count == 0)
                    yield break;
                yield return ret;
            }
        }
        #endregion enumerate

        /// <summary>
        /// Расщепляет поток на два списка.
        /// Увы, ленивым его не сделать.
        /// [ToDo] реализовать ленивый вариант. :)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flow">Конечный поток данных.</param>
        /// <param name="pred">Условие. Если верно, то в первый список. Если нет - то во второй.</param>
        public static Tuple<List<T>, List<T>> Split<T>(this IEnumerable<T> flow, Predicate<T> pred)
        {
            var tmp1 = new List<T>();
            var tmp2 = new List<T>();
            foreach(var f in flow)
                if(pred(f))
                    tmp1.Add(f);
                else
                    tmp2.Add(f);

            return new Tuple<List<T>, List<T>>(tmp1, tmp2);
        }

        #region deNullable
        public static IEnumerable<T> DeNullabe<T>(this IEnumerable<T?> flow) where T: struct
        {
            if (flow == null)
                yield break;
            foreach (var t in flow)
                if (t.HasValue)
                    yield return t.Value;
        }

        public static IEnumerable<TV> DeNullabe<T,TV>(this IEnumerable<T> flow, Func<T, TV?> f) where TV : struct
        {
            if (flow == null)
                yield break;
            foreach (var t in flow)
            {
                var v = f(t);
                if (v.HasValue)
                    yield return v.Value;
            }
        }
        #endregion deNullable

        public static void ForEach<T>(this IEnumerable<T> c, Action<T> action)
        {
            if (c == null || action == null)
                return;

            foreach (T obj in c)
                action(obj);
        }

        public static IEnumerable<T> Prepare<T>(this IEnumerable<T> c, Action<T> action)
        {
            if (c == null)
                yield break;

            foreach (T obj in c)
            {
                if (action != null)
                    action(obj);

                yield return obj;
            }
        }

        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }

        public static Queue<T> ToQueue<T>(this IEnumerable<T> flow)
        {
            var ret = new Queue<T>();
            flow.ForEach(ret.Enqueue);
            return ret;
        }

        public static Stack<T> ToStack<T>(this IEnumerable<T> flow)
        {
            return new Stack<T>(flow);
        }

        //public static SortedQueue<TK,TV> ToSortedQueue<TK,TV>(this IEnumerable<TV> flow, Func<TV,TK> f)
        //{
        //    var ret = new SortedQueue<TK, TV>(f);
        //    flow.ForEach(ret.EnQueue);
        //    return ret;
        //} 

        public static double Prod(this IEnumerable<double> flow)
        {
            return flow.Aggregate(1.0, (current, f) => current * f);
        }
    }
}
