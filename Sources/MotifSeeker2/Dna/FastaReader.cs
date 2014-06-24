using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MotifSeeker2.Dna
{
    /// <summary>
    /// Читатель формата FASTA.
    /// http://en.wikipedia.org/wiki/FASTA_format
    /// http://www.ncbi.nlm.nih.gov/BLAST/blastcgihelp.shtml
    /// </summary>
    public sealed class FastaReader
    {
        public enum FastaItem
        {
            A = 0,
            T = 1,
            G = 2,
            C = 3,
            Alow = 4,
            Tlow = 5,
            Glow = 6,
            Clow = 7,
            Any = 8
        };

        private readonly string _path;
        public string Description { get; private set; }

        public FastaReader(string path)
        {
            _path = path;
            Description = File.ReadAllLines(_path).First().Substring(1);
        }

        public IEnumerable<FastaItem[]> ReadFlow()
        {
            return from line in File.ReadAllLines(_path) where !line.StartsWith(">") select line.Select(ToFastaItem).ToArray();
        }

        private static FastaItem ToFastaItem(char c)
        {
            switch (c)
            {
                case 'N':
                    return FastaItem.Any;
                case 'A':
                    return FastaItem.A;
                case 'T':
                    return FastaItem.T;
                case 'G':
                    return FastaItem.G;
                case 'C':
                    return FastaItem.C;
                case 'a':
                    return FastaItem.Alow;
                case 't':
                    return FastaItem.Tlow;
                case 'g':
                    return FastaItem.Glow;
                case 'c':
                    return FastaItem.Clow;
                default:
                    throw new NotSupportedException("Символ '" + c + "' не определён.");
            }
        }
    }
}
