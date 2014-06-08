using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace MotifSeeker.Test
{
	[TestFixture]
    public class ModularityTest
    {
		[Test]
		public void Trivial()
		{
            var objs = new object[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G' };
            var c = objs.Length;
            var w = new int[c][];
            var gr1 = new[] { 'A', 'B', 'C' };
            var gr2 = new[] { 'D', 'E', 'F', 'G' };
            for (int i = 0; i < c; i++)
            {
                w[i] = new int[c];
                for (int j = 0; j < c; j++)
                {
                    if (i == j)
                        w[i][j] = 200;
                    else if (gr1.Contains((char)objs[i]) && gr1.Contains((char)objs[j]))
                        w[i][j] = 100;
                    else if (gr2.Contains((char)objs[i]) && gr2.Contains((char)objs[j]))
                        w[i][j] = 100;
                    else
                        w[i][j] = 0;
                }

            }
            var m = new Modularity(objs, w);
            Console.WriteLine("Start:" + m.CalcTotalModularity());
            int iter = 1;
            while (m.Iterate(true))
                Console.WriteLine("Iter[" + iter++ + "]:" + m.CalcTotalModularity() + ", nodes=" + m.NodesCount);
            Console.WriteLine("Result:" + m.CalcTotalModularity());
            Console.WriteLine("fin");
            Debug.Assert(m.NodesCount == 2);
		}

    }
}
