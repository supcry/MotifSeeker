using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using MotifSeeker.Data.Dna;

namespace MotifSeeker.Data.DNaseI
{
    /// <summary>
    /// Список данных по экспериментам.
    /// </summary>
    public class FileList
    {
        public readonly Item[] Items;

        public FileList(string path)
        {
            Items = File.ReadAllLines(path).Select(p => new Item(p)).ToArray();
        }

        public class Item
        {
            public string FileName;

            public Dictionary<string, string> Attributes;

            public Item(string stroke)
            {
                var parts = stroke.Split(new[] {"\t", "; "}, StringSplitOptions.RemoveEmptyEntries);
                FileName = parts[0];
                Attributes = new Dictionary<string, string>();
                foreach (var pair in parts.Skip(1))
                {
                    var pp = pair.Split('=');
                    Attributes.Add(pp[0], pp[1]);
                }
            }

            public bool Filter(IEnumerable<KeyValuePair<string, string>> attrs)
            {
                return attrs.All(p => Attributes[p.Key] == p.Value);
            }
        }
    }

	public static class DNaseIManager
	{
		private const string DataRoot = "../../../../Data";

        private const string ExperimentDir = "DNaseI";

        private const string SrcBaseRoot = @"http://hgdownload.cse.ucsc.edu/goldenPath/hg19/encodeDCC/wgEncodeUwDnase/";

	    private static string GetDir()
	    {
            if (!Directory.Exists(DataRoot))
                throw new DirectoryNotFoundException("Не найдена директория для данных. Она уже должна существовать.");
            var chrDir = Path.Combine(DataRoot, ExperimentDir);
            if (!Directory.Exists(chrDir))
                Directory.CreateDirectory(chrDir);
	        return chrDir;
	    }

	    public static string GetFileListPath()
	    {
	        var dir = GetDir();
	        var flist = Path.Combine(dir, "files.txt");
            if (!File.Exists(flist))
            {
                const string url = SrcBaseRoot + "files.txt";
                Console.WriteLine("Загружаем файл со списком данных: " + url);
                try
                {
                    using (var c = new WebClient())
                        c.DownloadFile(url, flist);
                }
                catch (Exception ex)
                {
                    File.Delete(flist);
                    throw new Exception("Не удалось загрузить файл", ex);
                }
                Console.WriteLine("Файл со списком данных локально сохранён по пути " + flist + ", размер: " + new FileInfo(flist).Length + "B");
            }
	        return flist;
	    }

		private static string DownloadFile(FileList.Item item, string dir)
		{
			
			var path = Path.Combine(dir, item.FileName);
			if (!File.Exists(path))
			{
				var url = SrcBaseRoot + item.FileName;
				Console.Write("w");
				try
				{
					using (var c = new WebClient())
						c.DownloadFile(url, path);
				}
				catch (Exception ex)
				{
					File.Delete(path);
					throw new Exception("Не удалось загрузить файл", ex);
				}
			}
			return path;
		}

		private static string UnpackFile(string pathGz)
		{
			var fpath = pathGz.Substring(0, pathGz.Length - 3);
			if (File.Exists(fpath))
				return fpath;
			Console.Write("z");
			using (var gf = File.OpenRead(pathGz))
			{
				using (var gz = new GZipStream(gf, CompressionMode.Decompress))
				{
					var tmp = new byte[1024 * 1024 * 10];
					using (var f = File.Create(fpath))
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
			return fpath;
		}

		public static IEnumerable<string> GetFiles(Dictionary<string, string> filterAttrs, int maxCount = int.MaxValue)
		{
			string type;
			if (!filterAttrs.TryGetValue("type", out type) || !new[] { "narrowPeak", "broadPeak" }.Contains(type))
				throw new ArgumentException("Тип данных эксперимента должен быть narrowPeak либо broadPeak.");
			var flistPath = GetFileListPath();
			var flist = new FileList(flistPath);
			var dir = GetDir();
			int cnt = 0;
			foreach (var item in flist.Items.Where(p => p.Filter(filterAttrs)))
			{
				Console.Write("[" + ++cnt + "], " + item.FileName + ":");
				var path = DownloadFile(item, dir);
				if (path.EndsWith(".gz"))
					path = UnpackFile(path);
				Console.WriteLine();
				yield return path;
				if(cnt == maxCount)
					yield break;
			}
		}

	    public static SensitivityResults GetSensitivityResults(Dictionary<string, string> filterAttrs, ChromosomeEnum? chr)
	    {
	        string type;
            if (!filterAttrs.TryGetValue("type", out type) || !new[] { "narrowPeak", "broadPeak" }.Contains(type))
                throw new ArgumentException("Тип данных эксперимента должен быть narrowPeak либо broadPeak.");
	        var flistPath = GetFileListPath();
	        var flist = new FileList(flistPath);
	        var items = flist.Items.Where(p => p.Filter(filterAttrs)).ToArray();
            if(items.Length == 0)
                throw new Exception("Не найдены данные эксперимента, удовлетворяющих заданному фильтру.");
            if(items.Length > 1)
                throw new Exception("Заданному фильтру удовлетворяет " + items.Length + " элементов.");
	        var item = items[0];
	        var fpathGz = Path.Combine(GetDir(), item.FileName);
	        var fpath = fpathGz.Substring(0, fpathGz.Length - 3);
	        if (!File.Exists(fpath))
	        {
	            if (!File.Exists(fpathGz))
	            {
                    var url = SrcBaseRoot + item.FileName;
                    Console.WriteLine("Запускаем загрузку из инета по адресу: " + url);
                    try
                    {
                        using (var c = new WebClient())
                            c.DownloadFile(url, fpathGz);
                    }
                    catch (Exception ex)
                    {
                        File.Delete(fpathGz);
                        throw new Exception("Не удалось загрузить файл", ex);
                    }
                    Console.WriteLine("Файл локально сохранён по пути " + fpathGz);
	            }
                try
                {
                    Console.WriteLine("Запускаем разархивацию файла по пути " + fpathGz);
                    using (var gf = File.OpenRead(fpathGz))
                    {
                        using (var gz = new GZipStream(gf, CompressionMode.Decompress))
                        {
                            var tmp = new byte[1024 * 1024 * 10];
                            using (var f = File.Create(fpath))
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
                    Console.WriteLine("Разархивация завершена по пути " + fpath);
                }
                catch (Exception ex)
                {
                    File.Delete(fpath);
                    throw new Exception("Не удалось распаковать файл", ex);
                }
	        }
            return new SensitivityResults(fpath, filterAttrs.ToArray(), chr);
	    }

	    public static Dictionary<ClassifiedRegion.MotifContainsStatus, List<ClassifiedRegion>> GetClassifiedRegions(
	        ChromosomeEnum chr, Dictionary<string, string> filterAttrs, bool keepUnknownRegions)
	    {
	        var sense = GetSensitivityResults(filterAttrs, chr);
	        var max = sense.Items.Max(p => p.Value2);
	        var presList = new List<ClassifiedRegion>();
            var npresList = new List<ClassifiedRegion>();
            var unkList = new List<ClassifiedRegion>();
	        var thresholdMin = max*0.1;
            var thresholdMax = max*0.9;

	        foreach (var item in sense.Items)
	        {
	            if(item.Value2 > thresholdMax)
                    presList.Add(new ClassifiedRegion(true, item.StartPos, item.EndPos, item.Value1, item.Value2));
                else if(item.Value2 < thresholdMin)
                    npresList.Add(new ClassifiedRegion(false, item.StartPos, item.EndPos, item.Value1, item.Value2));
                else if(keepUnknownRegions)
                    unkList.Add(new ClassifiedRegion(null, item.StartPos, item.EndPos, item.Value1, item.Value2));
	        }
	        var ret = new Dictionary<ClassifiedRegion.MotifContainsStatus, List<ClassifiedRegion>>();
            ret.Add(ClassifiedRegion.MotifContainsStatus.Present, presList);
            ret.Add(ClassifiedRegion.MotifContainsStatus.NotPresent, npresList);
            if(keepUnknownRegions)
                ret.Add(ClassifiedRegion.MotifContainsStatus.Unknown, unkList);
	        return ret;
	    } 
	}
}
