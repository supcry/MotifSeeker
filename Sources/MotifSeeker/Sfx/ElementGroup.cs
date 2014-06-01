using System;
using System.CodeDom;
using System.Linq;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Sfx
{
	public class ElementGroup
	{
		public readonly byte[] Chain;

		public readonly uint[] Positions;

		private readonly int _hash;

		public int Count { get { return Positions.Length; } }

		public ElementGroup(byte[] chain, uint[] positions)
		{
			Chain = chain;
			Positions = positions;
			Array.Sort(Positions);
			_hash = Chain.Aggregate(0, (a, b) => a*13 + b);
		}

		public override int GetHashCode()
		{
			return _hash;
		}

		public override string ToString()
		{
			return "Sz:" + Chain.Length + ",Cnt:" + Positions.Length + ",Pos0:" + Positions[0];
		}

	    public string ChainAsString()
	    {
	        return string.Join("", Chain.Select(p => p.ToNucleotide()));
	    }

        public Nucleotide[] NucleoChain { get { return Chain.Select(p => p.ToNucleotide()).ToArray(); } }
	}
}
