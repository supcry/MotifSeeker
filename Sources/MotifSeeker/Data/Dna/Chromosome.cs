using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;

namespace MotifSeeker.Data.Dna
{
	/// <summary>
	/// Хромосома в компактном представлении. 2 бита на нуклеотид.
	/// </summary>
	[DataContract]
	public class Chromosome
	{
		[DataMember(Order = 1)]
		public int Count { get; private set; }

		[DataMember(Order = 2)]
		public ChromosomeEnum ChromosomeId { get; private set; }

		[DataMember(Order = 3)]
		private byte[] _data = new byte[0];

		private Chromosome() { }

		public Chromosome(ChromosomeEnum id) : this()
		{
			ChromosomeId = id;
		}

		public IEnumerator<Nucleotide> GetEnumerator()
		{
			var cnt = 0;
			foreach (var b in _data)
			{
				var bb = b;
				for (int i = 0; i < 4; i++)
				{
					yield return (Nucleotide) (bb & 0x03);
					if (++cnt == Count)
						yield break;
					bb = (byte)(bb >> 2);
				}
			}
		}

		public Nucleotide[] GetPack(int start, int count)
		{
			Debug.Assert(start + count <= Count);
			var ret = new Nucleotide[count];
			if (count == 0)
				return ret;
			var byteId = start/4;
			var lastByteId = (start + count)/4;
			var packId = start%4;
			var b = _data[byteId];
			var cnt = 0;
			for (int i = packId; i < 4; i++)
			{
				ret[cnt] = (Nucleotide)((b >> (2*(i))) & 0x03);
				if (++cnt == count)
					break;
			}
			byteId++;

			for (; byteId <= lastByteId && cnt != count; byteId++)
			{
				b = _data[byteId];
				for (int i = 0; i < 4; i++)
				{
					ret[cnt] = (Nucleotide)(b & 0x03);
					if (++cnt == count)
						break;
					b = (byte) (b >> 2);
				}
			}
			return ret;
		}

		public void AddRange(ICollection<Nucleotide> items)
		{
			var nbc = (Count + items.Count + 3)/4;
			if (nbc > _data.Length)
				Array.Resize(ref _data, Math.Max(nbc, _data.Length*2));
			var byteId = Count/4;
			var packId = Count%4;
			var curByte = _data[byteId];
			int skipHeader;
			if (packId > 0)
			{
				
				var itemHeader = items.Take(4 - packId).ToArray();
				skipHeader = itemHeader.Length;
				for (int i = packId; i < 4 && i < itemHeader.Length+packId; i++)
				{
					var shift = 2*i;
					curByte = (byte) ((curByte & ~(0x03 << shift)) | ((int) itemHeader[i - packId] << shift));
				}
				_data[byteId] = curByte;
				Count += skipHeader;
				byteId++;
				packId = 0;
			}
			else
				skipHeader = 0;

			byte tmp = 0;
			foreach (var item in items.Skip(skipHeader))
			{
				var shift = 2*packId;
				tmp =  (byte)(tmp | ((int)item << shift));
				Count++;
				packId = (packId + 1)%4;
				if (packId == 0)
					_data[byteId++] = tmp;
			}
			if (packId != 0)
				_data[byteId] = tmp;
		}

		/// <summary>
		/// Удаляет лишние байты в хвосте.
		/// </summary>
		public void Compact()
		{
			var bcnt = (Count + 3)/4;
			Debug.Assert(bcnt <= _data.Length);
			if(bcnt != _data.Length)
				Array.Resize(ref _data, bcnt);
		}

		public long Serialize(string path)
		{
			Compact();
			using (var s = File.Create(path, 1024*1024*10))
			{
				s.SetLength(0);
				Serializer.Serialize(s, this);
				s.Flush();
				return s.Length;
			}
		}

		public static Chromosome Deserialize(string path)
		{
			using (var f = File.OpenRead(path))
				return Serializer.Deserialize<Chromosome>(f);
		}
	}
}
