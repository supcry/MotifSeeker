using System.Collections.Generic;
using System.IO;
using System.Linq;
using MotifSeeker.Data;
using NUnit.Framework;

namespace MotifSeeker.Test
{
	[TestFixture]
	public class ChromosomeTest
    {
		[Test]
		public void Trivial()
		{
			var c = new Chromosome(ChromosomeEnum.Unknown);
			Assert.AreEqual(ChromosomeEnum.Unknown, c.ChromosomeId);
			Assert.AreEqual(0, c.Count);
		}

		[Test]
		public void ByElements()
		{
			for (var n = Nucleotide.A; n <= Nucleotide.C; n++)
			{
				var c = new Chromosome(ChromosomeEnum.Unknown);
				c.AddRange(new []{n});
				Assert.AreEqual(1, c.Count);
				var pack = c.GetPack(0, 1);
				Assert.AreEqual(1, pack.Length);
				Assert.AreEqual(n, pack[0]);
			}
		}

		[Test]
		public void ByRepeats()
		{
			for (var n = Nucleotide.A; n <= Nucleotide.C; n++)
			{
				var c = new Chromosome(ChromosomeEnum.Unknown);
				for (int i = 1; i <= 10; i++)
				{
					c.AddRange(new[] {n});
					Assert.AreEqual(i, c.Count);
					var pack = c.GetPack(0, i);
					Assert.AreEqual(i, pack.Length);
					Assert.IsTrue(pack.All(p => p == n));
				}
			}

			for (var n = Nucleotide.A; n <= Nucleotide.C; n++)
			{
				for (int i = 1; i <= 10; i++)
				{
					var c = new Chromosome(ChromosomeEnum.Unknown);
					c.AddRange(Enumerable.Repeat(n, i).ToArray());
					Assert.AreEqual(i, c.Count);
					var pack = c.GetPack(0, i);
					Assert.AreEqual(i, pack.Length);
					Assert.IsTrue(pack.All(p => p == n));
				}
			}
			var tmp = new List<Nucleotide>();
			var cc = new Chromosome(ChromosomeEnum.Unknown);
			for (int i = 0; i <= 10; i++)
			for (var n = Nucleotide.A; n <= Nucleotide.C; n++)
			{
				cc.AddRange(new[] {n});
				tmp.Add(n);
				Assert.IsTrue(cc.Count >= i*4);
				var pack = cc.GetPack(cc.Count-1, 1);
				Assert.AreEqual(1, pack.Length);
				Assert.AreEqual(n, pack[0]);
				var pack2 = cc.GetPack(0, cc.Count);
				CollectionAssert.AreEqual(tmp, pack2);
			}
		}


		[Test]
		public void Test1()
		{
			var c = new Chromosome(ChromosomeEnum.Unknown);
			Assert.AreEqual(ChromosomeEnum.Unknown, c.ChromosomeId);
			Assert.AreEqual(0, c.Count);
			var pack = new[] {Nucleotide.A, Nucleotide.G, Nucleotide.T, Nucleotide.C, Nucleotide.G};
			c.AddRange(pack);
			Assert.AreEqual(pack.Length, c.Count);
			c.AddRange(pack);
			Assert.AreEqual(pack.Length*2, c.Count);
			var pack1 = c.GetPack(0, pack.Length);
			var pack2 = c.GetPack(pack.Length, pack.Length);
			var pack3 = c.GetPack(0, pack.Length*2);
			CollectionAssert.AreEqual(pack, pack1);
			CollectionAssert.AreEqual(pack1, pack2);
			var tmpPath = "qwe.tmp";
			File.Delete(tmpPath);
			c.Serialize(tmpPath);
			var c2 = Chromosome.Deserialize(tmpPath);
			Assert.AreEqual(c.ChromosomeId, c2.ChromosomeId);
			Assert.AreEqual(c.Count, c2.Count);
			var tmp = new List<Nucleotide>();
			var e = c.GetEnumerator();
			while(e.MoveNext())
				tmp.Add(e.Current);
			var tmp2 = c2.GetPack(0, pack.Length*2).ToList();
			CollectionAssert.AreEqual(tmp, tmp2);
		}
    }
}
