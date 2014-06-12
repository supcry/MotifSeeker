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
        private AlignmentResult[][] _edges;

        public Clustering(IEnumerable<ElementGroup> elements)
        {
            _nodes = elements.OrderByDescending(p => p.Count).ToArray();
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

        public List<Cluster> Work3(int cut = 3, int seed = 1, bool setCoreWeights = false)
        {
            InitEdges();
            Console.WriteLine("Запущена кластеризация второго типа для " + _nodes.Length + " узлов");
            var sw = Stopwatch.StartNew();
            var weights = new int[_nodes.Length][];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = new int[_nodes.Length];
                for (int j = 0; j < _nodes.Length; j++)
                    weights[i][j] = i != j ? GetEdge(i, j).Weight - cut : (setCoreWeights ? _nodes[i].Count : 0);//_nodes[i].Chain.Length;
            }
            var m = new Modularity(_nodes, weights, seed);
            Console.WriteLine("Start with:" + m.CalcTotalModularity());
            var iter = 1;
            while (m.Iterate())
                Console.WriteLine("Iter[" + iter++ + "]:" + m.CalcTotalModularity());
            Console.WriteLine("Result:" + m.CalcTotalModularity());
            sw.Stop();
            Console.WriteLine("Кластеризация завершена за " + sw.Elapsed);
            var clusterIds = m.ClasterIds;
            var ret = new List<Cluster>();
            for (int i = 0; i < m.NodesCount; i++)
            {
                // переобозначим элементы каждого кластера
                int id = 0;
                var dic = new Dictionary<int, int>();
                for (int j = 0; j < clusterIds.Length; j++)
                {
                    if(clusterIds[j] != i)
                        continue;
                    dic.Add(id++,j);
                }
                var nodes = new ElementGroup[dic.Count];
                var edges = new AlignmentResult[dic.Count][];
                for (int j = 0; j < dic.Count; j++)
                {
                    nodes[j] = _nodes[dic[j]];
                    edges[j] = new AlignmentResult[j];
                    for (int k = 0; k < j; k++)
                    {
                        edges[j][k] = _edges[dic[j]][dic[k]];
                        if (k == 0)
                        {
                            var tt = Alignment.Align(nodes[j].NucleoChain, nodes[k].NucleoChain, 0, 0);
                            Debug.Assert(tt.Mask == edges[j][k].Mask);
                        }
                    }
                }
                ret.Add(new Cluster(ret.Count, nodes, edges));
            }
            return ret;
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


    public class Cluster
    {
        public readonly int ClusterId;
        public readonly ElementGroup[] Nodes;
        public AlignmentResult[][] Edges;

        public Cluster(int clusterId, ElementGroup[] nodes, AlignmentResult[][] edges)
        {
            ClusterId = clusterId;
            Nodes = nodes;
            Edges = edges;
        }

        public int TotalCount { get { return Nodes.Sum(p => p.Count); } }


        public override string ToString()
        {
            return "Cnt:" + Nodes.Sum(p => p.Count);
        }

        public MultiAlignmentResult Align()
        {
            var parent = Nodes[0];
            
            var directions = new Direction[Nodes.Length];
            var shifts = new int[Nodes.Length];

            var w = Alignment.GetWeightMatrix(Nodes.Select(p => p.NucleoChain).ToArray());
            var baseId = w.FirstIndexWhereMax(a => a.Sum());

            directions[baseId] = Direction.Straight;
            shifts[baseId] = 0;
            
            for (int i = 0; i < Nodes.Length; i++)
            {
                if(i == baseId)
                    continue;
                var a = Alignment.Align(parent.NucleoChain, Nodes[i].NucleoChain);
                directions[i] = a.Direction;
                shifts[i] = a.Shift1 - a.Shift2;
            }
            var minShift = shifts.Min();
            Debug.Assert(minShift <= 0);
            var map = new Nucleotide[Nodes.Length][];
            for (int i = 0; i < Nodes.Length; i++)
            {
                var s = shifts[i] -= minShift;
                var chain = Nodes[i].NucleoChain.GetChain(directions[i]);
                if (s > 0)
                    chain = Enumerable.Repeat(Nucleotide.All, s).Concat(chain).ToArray();
                map[i] = chain;
            }
            return new MultiAlignmentResult(shifts, directions, map);
        }
    }
}
