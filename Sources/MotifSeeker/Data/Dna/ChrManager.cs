using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace MotifSeeker.Data.Dna
{
	public static class ChrManager
	{
		private const string DataRoot = "../../../../Data";

		private const string ChromosomeFolder = "Chromosomes";

		private const string SrcBaseRoot = @"http://hgdownload.soe.ucsc.edu/goldenPath/hg19/chromosomes/";

	    public static string GetChromosomeFaPath(ChromosomeEnum id)
	    {
            if (!Directory.Exists(DataRoot))
                throw new DirectoryNotFoundException("Не найдена директория для данных. Она уже должна существовать.");
            var chrDir = Path.Combine(DataRoot, ChromosomeFolder);
            if (!Directory.Exists(chrDir))
                Directory.CreateDirectory(chrDir);
            var chrName = id.ToString().ToLower() + ".fa";
            var chrPath = Path.Combine(chrDir, chrName);
	        if (File.Exists(chrPath))
	            return chrPath;

            var chrPathGz = chrPath + ".gz";

            // если нет ни гзипованной, ни разгзипованной версии, то загрузим из инета гзипованную
            if (!File.Exists(chrPathGz) && !File.Exists(chrPath))
            {
                var url = SrcBaseRoot + chrName + ".gz";
                Console.WriteLine("Файлов с хромосомой локально не обнаружен. Запускаем загрузку из инета по адресу: " + url);
                try
                {
                    using (var c = new WebClient())
                        c.DownloadFile(url, chrPathGz);
                }
                catch (Exception ex)
                {
                    File.Delete(chrPathGz);
                    throw new Exception("Не удалось загрузить файл", ex);
                }
                Console.WriteLine("Архив с хромосомой локально сохранён по пути " + chrPathGz + ", размер: " + new FileInfo(chrPathGz).Length + "B");
            }
            // если нет разгзипованной версии, то разгзипуем то, что уже скачали
            if (!File.Exists(chrPath))
                try
                {
                    Console.WriteLine("Запускаем разархивацию хромосомы по пути " + chrPathGz + ", размер: " + new FileInfo(chrPathGz).Length + "B");
                    using (var gf = File.OpenRead(chrPathGz))
                    {
                        using (var gz = new GZipStream(gf, CompressionMode.Decompress))
                        {
                            var tmp = new byte[1024 * 1024 * 10];
                            using (var f = File.Create(chrPath))
                            {
                                int l;
                                while ((l = gz.Read(tmp, 0, tmp.Length)) > 0)
                                {
                                    f.Write(tmp, 0, l);
                                    if (l != tmp.Length)
                                        break;
                                }
                                f.Flush();
                            }
                        }
                    }
                    Console.WriteLine("Разархивация завершена по пути " + chrPath + ", размер: " + new FileInfo(chrPath).Length + "B");
                }
                catch (Exception ex)
                {
                    File.Delete(chrPath);
                    throw new Exception("Не удалось распаковать файл", ex);
                }
            return chrPath;
	    }

		public static Chromosome GetChromosome(ChromosomeEnum id)
		{
			if (!Directory.Exists(DataRoot))
				throw new DirectoryNotFoundException("Не найдена директория для данных. Она уже должна существовать.");
			var chrDir = Path.Combine(DataRoot, ChromosomeFolder);
			if (!Directory.Exists(chrDir))
				Directory.CreateDirectory(chrDir);

			var chrName = id.ToString().ToLower() + ".fa";
			var chrPath = Path.Combine(chrDir, chrName);

			if (File.Exists(chrPath + ".proto"))// файл есть
				return Chromosome.Deserialize(chrPath + ".proto");

		    var chrPath2 = GetChromosomeFaPath(id);
            Debug.Assert(chrPath2 == chrPath);

			Console.WriteLine("Запускаем конвертацию во внутреннее представление файла " + chrPath + ", размер: " + new FileInfo(chrPath).Length + "B");
			// конвертация файла в наш формат
			var chr = new Chromosome(id);
			bool isFirst = true;
			foreach (var line in File.ReadLines(chrPath))
			{
				if (isFirst)
				{
					if(line != ">" + id.ToString().ToLower())
						throw new Exception("Заголовок файла не верен: " + line);
					isFirst = false;
					continue;
				}
				chr.AddRange(line.Select(CharToNucleotide).ToArray());
			}
			Console.WriteLine("Конвертация завершена. Осталось только сериализовать для последующего испольования.");
			// сериализация его
			var serL = chr.Serialize(chrPath + ".proto");
			Console.WriteLine("Хромосома в новом представлении сериализована. Размер: " + serL + "B");
			return chr;
		}

		private static Nucleotide CharToNucleotide(char c)
		{
			switch (c)
			{
				case 'a': case 'A':
					return Nucleotide.A;
				case 't':
				case 'T':
					return Nucleotide.T;
				case 'g':
				case 'G':
					return Nucleotide.G;
				case 'c':
				case 'C':
					return Nucleotide.C;
				case 'N':
					return Nucleotide.A;
				default:
					throw new NotSupportedException("Неизвестный идентификатор '" + c + "'");
			}
		}
	}
}
