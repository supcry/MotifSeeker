using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MotifSeeker.Helpers;
using NUnit.Framework;

namespace MotifSeeker.Test
{
	[TestFixture]
    public class StaticDictionaryTest
    {
		[Test]
		public void _01_Bools()
		{
		    var dic =
		        new StaticDictionary<bool, bool>(new[]
		        {new KeyValuePair<bool, bool>(false, true), new KeyValuePair<bool, bool>(true, false)});
            Assert.AreEqual(2, dic.Count);
			Assert.AreEqual(false, dic[true]);
            Assert.AreEqual(true, dic[false]);
		}

        [Test]
        public void _02_SmallInts()
        {
            var rnd = new Random(1);
            var pairs = new[]
            {
                new KeyValuePair<int, int>(rnd.Next(), rnd.Next()),
                new KeyValuePair<int, int>(rnd.Next(), rnd.Next()),
                new KeyValuePair<int, int>(rnd.Next(), rnd.Next())
            };

            TestDictionary(pairs);
            TestStaticDictionary(pairs);
        }

        [Test]
        public void _03_Ints()
        {
            var rnd = new Random(1);
            const int count = 1000000;
            var hs = new HashSet<int>();
            var pairs = Enumerable.Repeat(1, int.MaxValue)
                                  .Select(_ => new KeyValuePair<int, int>(rnd.Next(), rnd.Next()))
                                  .Where(p => hs.Add(p.Key))
                                  .Take(count)
                                  .ToArray();

            TestDictionary(pairs);
            TestStaticDictionary(pairs);
        }

	    void TestDictionary(ICollection<KeyValuePair<int, int>> data)
	    {
	        var sw = new Stopwatch();
            sw.Start();
	        var dic = new Dictionary<int, int>(data.Count);
	        foreach (var d in data)
	            dic[d.Key] = d.Value;
            sw.Stop();
            var sw2 = new Stopwatch();
            sw2.Start();
	        foreach (var d in data.Reverse())
	            Assert.AreEqual(d.Value, dic[d.Key]);
            sw2.Stop();
            Console.WriteLine("Dictionary: creation=" + sw.Elapsed + ", search=" + sw2.Elapsed);
	    }

        void TestStaticDictionary(ICollection<KeyValuePair<int, int>> data)
        {
            var sw = new Stopwatch();
            sw.Start();
            var dic = new StaticDictionary<int, int>(data);
            sw.Stop();
            Assert.AreEqual(data.Count, dic.Count);
            var sw2 = new Stopwatch();
            sw2.Start();
            foreach (var d in data)
                Assert.AreEqual(d.Value, dic[d.Key]);
            sw2.Stop();
            Console.WriteLine("StaticDictionary: creation=" + sw.Elapsed + ", search=" + sw2.Elapsed);
        }

    }
}
