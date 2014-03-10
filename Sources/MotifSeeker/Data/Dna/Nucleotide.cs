namespace MotifSeeker.Data.Dna
{
	/// <summary>
	/// Двубитный нуклеотид. Всего четыре значения: ATGC
	/// </summary>
	public enum Nucleotide : byte
	{
		A = 0,
		T = 1,
		G = 2,
		C = 3,
        End = 4
	}

	/// <summary>
	/// Четырёхбитный нуклеотид.
	/// </summary>
	public enum NucleotideBis : byte
	{
		A = 0,
		T = 1,
		G = 2,
		C = 3,
		Abis = 4,
		Tbis = 5,
		Gbis = 6,
		Cbis = 7,
		None = 15
	}
}
