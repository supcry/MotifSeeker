using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MotifSeeker.Data.Dna;
using MotifSeeker.Sfx;

namespace MotifSeeker
{
    public class Clustering
    {
        private readonly ElementGroup[] _nodes;
        private readonly int?[] _clusterIds;

        private AlignmentResult[][] _edges;

        public Clustering(ElementGroup[] elements)
        {
            _nodes = elements;
            _clusterIds = new int?[_nodes.Length];
        }

        public void InitEdges()
        {
            if (_edges != null)
                return;
            Console.WriteLine("Запущен рассчёт рёбер для " + _nodes.Length + " узлов");
            var sw = Stopwatch.StartNew();
            var edges = new AlignmentResult[_nodes.Length][];
            Parallel.For(0, edges.Length, new ParallelOptions {MaxDegreeOfParallelism = 4},
                i =>
                {
                    edges[i] = new AlignmentResult[i];
                    for (var j = 0; j < i; j++)
                        edges[i][j] = Alignment.Align(_nodes[i].NucleoChain, _nodes[j].NucleoChain, i, j);
                });
            //for (var i = 0; i < edges.Length; i++)
            //{
            //    edges[i] = new AlignmentResult[i];
            //    for (var j = 0; j < i; j++)
            //        edges[i][j] = Alignment.Align(_nodes[i].NucleoChain, _nodes[j].NucleoChain, i, j);
            //}
            _edges = edges;
            sw.Stop();
            Console.WriteLine("Рёбра рассчитаны за " + sw.Elapsed);
        }

        public void WriteToFileForGephi(string path, int minWeight = 5)
        {
            InitEdges();
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var pathNodes = path + ".nodes.csv";
            var pathEdges = path + ".edges.csv";
            // запишем узлы
            File.Delete(path);
            using (var f = File.CreateText(pathNodes))
            {
                f.WriteLine("id\tlabel\tsize");
                int id = 1;
                foreach (var el in _nodes)
                    f.WriteLine(id++ + "\t" + el.ChainAsString() + "\t" + el.Count);
                f.Flush();
                f.Close();
            }
            // запишем связи
            File.Delete(path);
            using (var f = File.CreateText(pathEdges))
            {
                f.WriteLine("source\ttarget\tlabel\tweight\tType\tDirection");
                for (int i = 0; i < _edges.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        var ares = _edges[i][j];// Alignment.Align(els[i].NucleoChain, els[j].NucleoChain);
                        if (ares.Weight < minWeight)
                            continue;
                        f.WriteLine((i + 1) + "\t" + (j + 1) + "\t" + ares.Mask + "\t" + (ares.Weight - minWeight + 1) + "\tUndirected\t" + ares.Direction);
                    }
                }
                f.Flush();
                f.Close();
            }
        }

        public void WriteElementsLog(string path, string comment)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Delete(path);
            using (var f = File.CreateText(path))
            {
                f.WriteLine(comment);
                f.WriteLine("id   grpSz chainLen\toriginalChain\tinversed\treversed\trevinversed");
                int id = 1;
                foreach (var el in _nodes)
                {
                    var cnt = el.Count;
                    var chain1 = el.Chain.Select(p => p.ToNucleotide()).ToArray();
                    var chain2 = chain1.Select(p => p.Inverse()).ToArray();
                    var chain3 = chain1.Reverse().ToArray();
                    var chain4 = chain2.Reverse().ToArray();
                    f.WriteLine(id++ + "\t" + cnt + "\t" + chain1.Length + "\t" +
                                string.Join("", chain1) + "\t" +
                                string.Join("", chain2) + "\t" +
                                string.Join("", chain3) + "\t" +
                                string.Join("", chain4) + "\t");
                }
                f.Flush();
                f.Close();
            }
        }

        public List<ClusterData> Work(int minWeight0)
        {
            InitEdges();
            Console.WriteLine("Запущена кластеризация " + _nodes.Length + " узлов");
            var sw = Stopwatch.StartNew();
            var thresholdQueue = new Queue<int>(_edges.SelectMany(p => p).Select(p => p.Weight).Distinct().OrderByDescending(p => p));

            
            int freeCnt = _nodes.Length;
            var clusters = new List<ClusterData>();// последовательность кластеров здесь только растёт
            while (thresholdQueue.Count > 0 && thresholdQueue.Peek() >= minWeight0 && freeCnt > 0) // продолжаем алгоритм, пока есть что добавлять
            {
                var thresholdWeight = thresholdQueue.Dequeue();
                foreach (var edge in GetUnclosedEdges(thresholdWeight).OrderByDescending(p => p.Weight)) // бежим по всем рёбрам, которые пока не находятся целиком в кластерах
                {
                    var cl1 = _clusterIds[edge.Id1];
                    var cl2 = _clusterIds[edge.Id2];

                    if (cl1.HasValue && cl2.HasValue)
                        continue;

                    // Сначала идёт два симметричных случая, когда строго один из концов ребра уже находится в кластере.
                    // Просто пробуем добавить узел в кластер.
                    if (cl1.HasValue)
                    {
                        if (cl2.HasValue && cl1 != cl2)
                            throw new Exception("Найдено два узла со связью из разных кластеров");
                        Debug.Assert(!cl2.HasValue);
                        if (TryToAddNodeToCluster(edge.Id2, clusters[cl1.Value], thresholdWeight - 2))
                            freeCnt--;
                        continue; // edge.Id2 - кандидат во вступление в кластер на следующей итерации.
                    }
                    if (cl2.HasValue)
                    {
                        if (cl1.HasValue && cl1 != cl2)
                            throw new Exception("Найдено два узла со связью из разных кластеров");
                        Debug.Assert(!cl1.HasValue);
                        if (TryToAddNodeToCluster(edge.Id1, clusters[cl2.Value], thresholdWeight - 2))
                            freeCnt--;
                        continue; // edge.Id1 - кандидат во вступление в кластер на следующей итерации.
                    }
                    // Ни один из концов ребра не находится в кластере. Попробуем добавить его в один из уже существующих кластеров.
                    // Если не получится, то создадим свой кластер для этой пары.
                    bool added = false;
// ReSharper disable AccessToForEachVariableInClosure
                    var cluster = clusters.FirstWhereMaxOrDefault(p => GetMaxWeight(edge.Id1, edge.Id2, p).Sum());
// ReSharper restore AccessToForEachVariableInClosure

                    if (cluster != null && TryToAddNodeToCluster(edge.Id1, edge.Id2, cluster, thresholdWeight - 2))
                    {
                        freeCnt -= 2;
                        added = true;
                    }
                    if (added)
                        continue;
                    cluster = new ClusterData(clusters.Count, edge.Id1, edge.Id2);
                    clusters.Add(cluster);
                    _clusterIds[edge.Id1] = cluster.ClusterId;
                    _clusterIds[edge.Id2] = cluster.ClusterId;
                    freeCnt -= 2;
                }
                Console.WriteLine("Порог кластеризации - " + thresholdWeight + ", осталось " + freeCnt + " свободных узлов." +
                                  " Уже затрачено на работу: " + sw.Elapsed + " и выделено " + clusters.Count + " кластеров.");
            }
            sw.Stop();
            Console.WriteLine("Кластеризация завершена за " + sw.Elapsed);
            return clusters;
        }

        public List<ClusterData> Work2(int minWeight0, int minWeight1)
        {
            InitEdges();
            Console.WriteLine("Запущена кластеризация второго типа для " + _nodes.Length + " узлов");
            var sw = Stopwatch.StartNew();
            int freeCnt = _nodes.Length;
            const int fuzzyShift = 0;
            var clusters = new List<ClusterData>();// последовательность кластеров здесь только растёт
            // Сначала ищет разбивает всё на кластеры, состоящие из цепочек не ниже minWeight0.
            for (int weight = minWeight0; weight >= minWeight1 && freeCnt > 0; weight--)
            {
                foreach (var edge in GetUnclosedEdges(weight).OrderByDescending(p => p.Weight))
                    // бежим по всем рёбрам, которые пока не находятся целиком в кластерах
                {
                    var cl1 = _clusterIds[edge.Id1];
                    var cl2 = _clusterIds[edge.Id2];

                    if (cl1.HasValue && cl2.HasValue)
                        continue;

                    // Сначала идёт два симметричных случая, когда строго один из концов ребра уже находится в кластере.
                    // Просто пробуем добавить узел в кластер.
                    if (cl1.HasValue)
                    {
                        if (cl2.HasValue && cl1 != cl2)
                            throw new Exception("Найдено два узла со связью из разных кластеров");
                        Debug.Assert(!cl2.HasValue);
                        if (TryToAddNodeToClusterByChain(edge.Id2, clusters[cl1.Value], weight - fuzzyShift))
                            freeCnt--;
                        continue; // edge.Id2 - кандидат во вступление в кластер на следующей итерации.
                    }
                    if (cl2.HasValue)
                    {
                        if (cl1.HasValue && cl1 != cl2)
                            throw new Exception("Найдено два узла со связью из разных кластеров");
                        Debug.Assert(!cl1.HasValue);
                        if (TryToAddNodeToClusterByChain(edge.Id1, clusters[cl2.Value], weight - fuzzyShift))
                            freeCnt--;
                        continue; // edge.Id1 - кандидат во вступление в кластер на следующей итерации.
                    }
                    // Ни один из концов ребра не находится в кластере. Попробуем добавить его в один из уже существующих кластеров.
                    // Если не получится, то создадим свой кластер для этой пары.

                    // ReSharper disable AccessToForEachVariableInClosure
                    var cluster = clusters.FirstWhereMaxOrDefault(p => GetMaxWeight(edge.Id1, edge.Id2, p).Max());
                    // ReSharper restore AccessToForEachVariableInClosure

                    if (cluster != null)
                    {
                        bool added = false;
                        if (TryToAddNodeToClusterByChain(edge.Id1, cluster, weight - fuzzyShift))
                        {
                            freeCnt--;
                            added = true;
                        }
                        if (TryToAddNodeToClusterByChain(edge.Id2, cluster, weight - fuzzyShift))
                        {
                            freeCnt--;
                            added = true;
                        }
                        if (added)
                            continue;
                    }
                    if (weight == minWeight0) // только на первой итерации можно создавать кластеры
                    {
                        cluster = new ClusterData(clusters.Count, edge.Id1, edge.Id2);
                        clusters.Add(cluster);
                        _clusterIds[edge.Id1] = cluster.ClusterId;
                        _clusterIds[edge.Id2] = cluster.ClusterId;
                        freeCnt -= 2;
                        Console.WriteLine("c");
                    }
                }

                if (weight == minWeight0) // только на первой итерации можно создавать кластеры

                    Console.WriteLine("Первая итерация прошла. Порог кластеризации - " + weight + ", осталось " +
                                      freeCnt + " свободных узлов." +
                                      " Уже затрачено на работу: " + sw.Elapsed + " и выделено " + clusters.Count +
                                      " кластеров.");
                else
                    Console.WriteLine("Порог кластеризации - " + weight + ", осталось " + freeCnt +
                                      " свободных узлов." +
                                      " Уже затрачено на работу: " + sw.Elapsed + " и выделено " + clusters.Count +
                                      " кластеров.");
            }



            // Затем добавляет оставшиеся элементы к имеющимся кластерам по алгоритму k-ближайших соседей с порогом minWeight1
            sw.Stop();
            Console.WriteLine("Кластеризация завершена за " + sw.Elapsed);
            return clusters;
        }

        public List<ClusterData> Work3(int cut = 3)
        {
            InitEdges();
            Console.WriteLine("Запущена кластеризация второго типа для " + _nodes.Length + " узлов");
            var sw = Stopwatch.StartNew();
            var weights = new int[_nodes.Length][];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = new int[_nodes.Length];
                for (int j = 0; j < _nodes.Length; j++)
                    weights[i][j] = i != j ? GetEdge(i, j).Weight - 6 : 0;//_nodes[i].Chain.Length;
            }
            var m = new Modularity(_nodes, weights);
            Console.WriteLine("Start with:" + m.CalcTotalModularity());
            var iter = 1;
            while (m.Iterate())
                Console.WriteLine("Iter[" + iter++ + "]:" + m.CalcTotalModularity());
            Console.WriteLine("Result:" + m.CalcTotalModularity());



            // Затем добавляет оставшиеся элементы к имеющимся кластерам по алгоритму k-ближайших соседей с порогом minWeight1
            sw.Stop();
            Console.WriteLine("Кластеризация завершена за " + sw.Elapsed);
            return null;
        }

        /// <summary>
        /// Возвращает последовательность связей, два конца которых пока не находятся в кластерах.
        /// </summary>
        private IEnumerable<AlignmentResult> GetUnclosedEdges(int minWeight)
        {
// ReSharper disable once LoopCanBeConvertedToQuery
            foreach(var line in _edges)
                foreach (var edge in line)
                {
                    if (edge.Weight < minWeight)
                        continue;
                    if (_clusterIds[edge.Id1].HasValue && _clusterIds[edge.Id2].HasValue)
                        continue;
                    yield return edge;
                }
        }

        private bool TryToAddNodeToCluster(int nodeId, ClusterData cluster, int minWeight)
        {
            int weight = 0;
            foreach (var id in cluster.NodeIds)
            {
                var edge = GetEdge(nodeId, id);
                if(edge.Weight < minWeight)
                    return false;
                weight += edge.Weight;
            }
            Debug.Assert(!_clusterIds[nodeId].HasValue);
            cluster.Put(weight, nodeId);
            _clusterIds[nodeId] = cluster.ClusterId;
            return true;
        }

        private bool TryToAddNodeToClusterByChain(int nodeId, ClusterData cluster, int minWeight)
        {
            int weight = 0;
            bool add = false;
            foreach (var id in cluster.NodeIds)
            {
                var edge = GetEdge(nodeId, id);
                if (edge.Weight >= minWeight)
                    add = true;
                weight += edge.Weight;
            }
            if (!add)
                return false;
            Debug.Assert(!_clusterIds[nodeId].HasValue);
            cluster.Put(weight, nodeId);
            _clusterIds[nodeId] = cluster.ClusterId;
            return true;
        }

        private bool TryToAddNodeToClusterByMean(int nodeId, ClusterData cluster, int minWeight)
        {
            int weight = 0;
            bool add = false;
            foreach (var id in cluster.NodeIds)
            {
                var edge = GetEdge(nodeId, id);
                if (edge.Weight >= minWeight)
                    add = true;
                weight += edge.Weight;
            }
            if (!add)
                return false;
            Debug.Assert(!_clusterIds[nodeId].HasValue);
            cluster.Put(weight, nodeId);
            _clusterIds[nodeId] = cluster.ClusterId;
            return true;
        }

        private bool TryToAddNodeToCluster(int nodeId1, int nodeId2, ClusterData cluster, int minWeight)
        {
            int weight = GetEdge(nodeId1, nodeId2).Weight;
            foreach (var nodeId in new[] { nodeId1, nodeId2})
                foreach (var id in cluster.NodeIds)
                {
                    var edge = GetEdge(nodeId, id);
                    if (edge.Weight < minWeight)
                        return false;
                    weight += edge.Weight;
                }
            cluster.Put(weight, nodeId1, nodeId2);
            Debug.Assert(!_clusterIds[nodeId1].HasValue);
            Debug.Assert(!_clusterIds[nodeId2].HasValue);
            _clusterIds[nodeId1] = cluster.ClusterId;
            _clusterIds[nodeId2] = cluster.ClusterId;
            return true;
        }

        private int[] GetMaxWeight(int nodeId1, int nodeId2, ClusterData cluster)
        {
            var ws = new int[2];
            for (int i = 0; i < new[] {nodeId1, nodeId2}.Length; i++)
            {
                var nodeId = new[] {nodeId1, nodeId2}[i];
                foreach (var id in cluster.NodeIds)
                {
                    var edge = GetEdge(nodeId, id);
                    if (edge.Weight > ws[i])
                        ws[i] = edge.Weight;
                }
            }
            return ws;
        }

        private AlignmentResult GetEdge(int nodeId1, int nodeId2)
        {
            Debug.Assert(nodeId1 != nodeId2);
            if (nodeId1 < nodeId2)
            {
                var tmp = nodeId1;
                nodeId1 = nodeId2;
                nodeId2 = tmp;
            }
            return _edges[nodeId1][nodeId2];
        }
    }


    public class ClusterData
    {
        public readonly int ClusterId;

        private readonly List<int> _ids = new List<int>();

        public int SummaryWeight { get; private set; }

        public int Count { get { return _ids.Count; } }

        public double Ratio { get { return Count > 1 ? SummaryWeight/(Count*(Count-1)/2.0) : 0; } }

        public IEnumerable<int> NodeIds { get { return _ids; } } 

        public ClusterData(int clusterId, int id1, int id2)
        {
            ClusterId = clusterId;
            _ids.Add(id1);
            _ids.Add(id2);
        }

        public void Put(int addWeight, params int[] ids)
        {
            _ids.AddRange(ids);
            SummaryWeight += addWeight;
        }

        public override string ToString()
        {
            return "Cnt:" + Count + ", Ratio:" + Ratio.ToString("F");
        }
    }
}
