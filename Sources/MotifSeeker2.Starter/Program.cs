using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MotifSeeker2.Dna;

namespace MotifSeeker2.Starter
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            Sandbox();

            sw.Stop();

            Console.WriteLine("fin: " + sw.Elapsed + "\npress any key to exit...");
            Console.ReadKey();

        }

        static void Sandbox()
        {
            var chr1 = Chromosome.GetChromosome(ChromosomeEnum.Chr1);
            var chrX = Chromosome.GetChromosome(ChromosomeEnum.Chr13);
        }
    }
}
