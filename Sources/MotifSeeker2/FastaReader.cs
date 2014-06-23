using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MotifSeeker2
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

        public IEnumerable<FastaItem> ReadFlow()
        {
            foreach (var line in File.ReadAllLines(_path))
            {
                if (line.StartsWith(">"))
                    continue;
                foreach (var c in line)
                {
                    switch (c)
                    {
                        case 'N':
                            yield return FastaItem.Any;
                            break;
                        case 'A':
                            yield return FastaItem.A;
                            break;
                        case 'T':
                            yield return FastaItem.T;
                            break;
                        case 'G':
                            yield return FastaItem.G;
                            break;
                        case 'C':
                            yield return FastaItem.C;
                            break;
                        case 'a':
                            yield return FastaItem.Alow;
                            break;
                        case 't':
                            yield return FastaItem.Tlow;
                            break;
                        case 'g':
                            yield return FastaItem.Glow;
                            break;
                        case 'c':
                            yield return FastaItem.Clow;
                            break;
                        default:
                            throw new NotSupportedException("Символ '" + c + "' не определён.");
                    }
                }
            }
        }
    }
}
