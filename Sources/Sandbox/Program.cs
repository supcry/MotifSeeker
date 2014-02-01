using System;
using System.Diagnostics;
using MotifSeeker.Data;

namespace Sandbox
{
	class Program
	{
		static void Main(string[] args)
		{
			var chr = Manager.GetChromosome(ChromosomeEnum.Chr1);
			Debug.Assert(chr != null);
			Console.WriteLine("Ok\nPress any key to exit");
			Console.ReadKey();

		}
	}
}
