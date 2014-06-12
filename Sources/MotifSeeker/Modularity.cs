using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MotifSeeker
{
    /// <summary>
    /// http://en.wikipedia.org/wiki/Modularity_(networks)
    /// http://perso.uclouvain.be/vincent.blondel/research/louvain.html
    /// https://sites.google.com/site/findcommunities/
    /// </summary>
    public class Modularity
    {
        // оригинальные данные, заданные в контсрукторе и никогда не меняются.
        private readonly object[] _objs; 
        private readonly int[][] _weights;
        private readonly int _sumOfWeights;

        readonly Random _rnd;

        private readonly int[] _clasterIds; // идентификатор кластера у объекта

        private Dictionary<int, Node> _nodes; // поиск, удаление
        private Dictionary<Pair, Edge> _edges; // поиск по паре, удаление
        private int _totalEdgesWeight; // суммарный вес в _edges

        public Modularity(object[] objs, int[][] weights, int seed = 1)
        {
            _objs = objs;
            _weights = weights;
            _rnd = new Random(seed);
            _totalEdgesWeight = 0;
            Debug.Assert(objs.Length == weights.Length);
            weights.ForEach(p => Debug.Assert(p.Length == weights.Length));
            _nodes = objs.Select((p, i) => new Node(i, new[] { i }, 2*weights[i][i], weights[i].Sum() - weights[i][i])).ToDictionary(p => p.Id);
            _clasterIds = Enumerable.Range(0, objs.Length).ToArray();
            _edges = new Dictionary<Pair, Edge>();
            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    var w = weights[i][j];
                    var pair = new Pair(i, j);
                    _edges.Add(pair, new Edge(pair, w));
                    _totalEdgesWeight += w;
                }
                _totalEdgesWeight += weights[i][i];
            }
            _sumOfWeights = _totalEdgesWeight;
#if DEBUG
            CheckConsistency();
#endif
        }

        public bool Iterate(bool smallStep = false)
        {
            bool ret = false;
            // phase 1: бежим по всем случайным образом и пытаемся утянуть один узел в другой. 
            var cnt = _nodes.Count;
            Debug.Assert(_nodes.ContainsKey(0));
            Debug.Assert(_nodes.ContainsKey(cnt-1));
            var a = new HashSet<int>();

            foreach (var id in _rnd.GetShuffleFlow(Enumerable.Range(0, cnt)))
            {
                if (_nodes.Count == 1)
                    break;
                Debug.Assert(id >= 0 && id < cnt);
                Debug.Assert(a.Add(id));
                Debug.Assert(_nodes.ContainsKey(id));
                //var node = _nodes[id];
                double diff;
// ReSharper disable AccessToForEachVariableInClosure
                var dst = _nodes.Where(p => p.Key != id).FirstWhereMax(p => CalcDiff(id, p.Key), out diff);
// ReSharper restore AccessToForEachVariableInClosure
                if (diff <= 0)
                    continue;
                Merge(id, dst.Value.Id);
                ret = true;
                if (smallStep)
                    break;
            }
            // phase 2: перебиваем идентификаторы
            if (ret)
            {
                int cid = 0;
                var dicNextIds = _nodes.Keys.OrderBy(p => p).ToDictionary(id => id, id => cid++);
                var nodes = _nodes.Values.ToArray();
                var edges = _edges.Values.ToArray();

                _nodes = nodes.Select(p => p.Shift(dicNextIds)).ToDictionary(p => p.Id);
                _edges = edges.Select(p => p.Shift(dicNextIds)).ToDictionary(p => p.Id);

                var cid2 = 0;
                foreach (var id in _nodes.Keys.OrderBy(p => p))
                    Debug.Assert(cid2++ == id);

                _totalEdgesWeight = _edges.Values.Sum(p => p.Weight) + _nodes.Values.Sum(p => p.SumWeightsInternal)/2;
                for (int i = 0; i < _clasterIds.Length; i++)
                    _clasterIds[i] = dicNextIds[_clasterIds[i]];
            }
#if DEBUG
            CheckConsistency();
#endif
            return ret;
        }

        public int[] ClasterIds { get { return _clasterIds; } }

        /// <summary>
        /// Вычисляет вклад в общую модулярность от переноса одного узла в другой.
        /// </summary>
        private double CalcDiff(int idSrc, int idDst)
        {
            Debug.Assert(idSrc != idDst);
            var nodeSrc = _nodes[idSrc];
            Debug.Assert(nodeSrc.Id == idSrc);
            var nodeDst = _nodes[idDst];
            Debug.Assert(nodeDst.Id == idDst);
            var sumIn = nodeDst.SumWeightsInternal; // суммарный вес рёбер внутри кластера-приёмника
            var sumExt = nodeDst.SumWeightsExternal; // суммарный вес рёбер, которые смотрят на кластер-приёмник
            var ki = nodeSrc.SumWeightsExternal; // суммарный вес рёбер, которые смотрят на сливаемый кластер
            var kiin = _edges[new Pair(idSrc, idDst)].Weight; // суммарный вес рёбер между кластерами
            var m2 = _totalEdgesWeight*2.0; // суммарный вес всех рёбер

            var part1 = (sumIn + kiin)/m2 - Math.Pow((sumExt + ki)/m2, 2);
            var part2 = sumIn / m2 - Math.Pow(sumExt / m2, 2) - Math.Pow(ki / m2, 2);
            var ret = part1 - part2;
            return ret;
        }

        /// <summary>
        /// Переносит один узел в другой, причём изменяет веса и узлы (удалив веса и узел у источника)
        /// </summary>
        private void Merge(int idSrc, int idDst)
        {
            Debug.Assert(idSrc != idDst);
            var nodeSrc = _nodes[idSrc];
            Debug.Assert(nodeSrc.Id == idSrc);
            var nodeDst = _nodes[idDst];
            Debug.Assert(nodeDst.Id == idDst);
            var pairDst = new Pair(idSrc, idDst);
            var interconnectedWeight = _edges[pairDst].Weight;
            _edges.Remove(pairDst);
            var ids = nodeDst.Merge(nodeSrc, interconnectedWeight);
            foreach (var id in ids)
                _clasterIds[id] = nodeDst.Id;
            foreach (var id in _nodes.Keys.ToArray().Where(p => p != idDst && p != idSrc && _nodes.ContainsKey(p)))
            {
                var pairSrc = new Pair(idSrc, id);
                var pair = new Pair(idDst, id);
                _edges[pair].ChangeWeight(_edges[pairSrc].Weight);
                _edges.Remove(pairSrc);
            }
            _nodes.Remove(nodeSrc.Id);
        }

        /// <summary>
        /// Вычисляет полную модулярность.
        /// Результат должен находиться в диапазоне от -1 до 1.
        /// </summary>
        public double CalcTotalModularity()
        {
            var val = 0.0;
            var m2 = _sumOfWeights*2.0;
            for(int i=0;i<_objs.Length;i++)
                for (int j = 0; j < _objs.Length; j++)
                {
                    if(_clasterIds[i] != _clasterIds[j])
                        continue;
                    var aij = _weights[i][j];
                    var ki = _weights[i].Sum();
                    var kj = _weights[j].Sum();
                    var tmp = aij - ki*kj/m2;
                    val += tmp;
                }
            var ret = val/m2;
            return ret;
        }

        public int NodesCount { get { return _nodes.Count; } }

        /// <summary>
        /// Проверка на вшивость
        /// </summary>
        public void CheckConsistency()
        {
            // корректность суммы весов
            var w = _nodes.Values.Sum(p => p.SumWeightsInternal)/2 + _edges.Values.Sum(p => p.Weight);
            Debug.Assert(w == _sumOfWeights);
            Debug.Assert(w == _totalEdgesWeight);

            // корректность заполнения классов кластеров
            foreach (var n in _nodes)
            {
                Debug.Assert(n.Key == n.Value.Id);
                foreach(var id in n.Value.ObjectIds)
                    Debug.Assert(_clasterIds[id] == n.Value.Id);
            }
        }


        public class Node
        {
            public readonly int Id;

            public readonly List<int> ObjectIds;

            public int SumWeightsInternal;

            public int SumWeightsExternal;

            public Node(int id, IEnumerable<int> objIds, int sumInt, int sumExt)
            {
                Id = id;
                ObjectIds = objIds.ToList();
                SumWeightsInternal = sumInt;
                SumWeightsExternal = sumExt;
            }

            public override int GetHashCode()
            {
                return Id;
            }

            public Node Shift(Dictionary<int, int> dic)
            {
                return new Node(dic[Id], ObjectIds, SumWeightsInternal, SumWeightsExternal);
            }

            public List<int> Merge(Node node, int interconnectedWeights)
            {
                var ret = node.ObjectIds;
                ObjectIds.AddRange(ret);
                SumWeightsInternal += node.SumWeightsInternal + 2*interconnectedWeights;
                SumWeightsExternal += node.SumWeightsExternal - 2*interconnectedWeights;
                return ret;
            }

            public override string ToString()
            {
                return "Id:" + Id + ", Cnt=" + ObjectIds.Count +
                    ", W1=" + SumWeightsInternal + ", W2=" + SumWeightsExternal;
            }
        }

        public class Edge
        {
            public readonly Pair Id;

            public int Weight;

            public Edge(Pair id, int weight)
            {
                Id = id;
                Weight = weight;
            }

            public Edge(int id1, int id2, int weight)
            {
                Id = new Pair(id1, id2);
                Weight = weight;
            }

            public void ChangeWeight(int change)
            {
                Weight += change;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public Edge Shift(Dictionary<int, int> dic)
            {
                return new Edge(new Pair(dic[Id.Id1], dic[Id.Id2]), Weight);
            }
        }

        /// <summary>
        /// Id1 всегда не меньше Id2
        /// </summary>
        public struct Pair : IEquatable<Pair>
        {
            public readonly short Id1;

            public readonly short Id2;

            public Pair(int id1, int id2)
            {
                Debug.Assert(id1 <= short.MaxValue);
                Debug.Assert(id2 <= short.MaxValue);

                if (id1 < id2)
                {
                    var tmp = id1;
                    id1 = id2;
                    id2 = tmp;
                }

                Id1 = (short)id1;
                Id2 = (short)id2;
            }
            public override int GetHashCode()
            {
                return (Id1 << 16) + Id2;
            }

            public bool Equals(Pair other)
            {
                return Id1 == other.Id1 && Id2 == other.Id2;
            }

            public override bool Equals(object obj)
            {
                if (obj is Pair)
                    return Equals((Pair) obj);
                return false;
            }
        }
    }

}
