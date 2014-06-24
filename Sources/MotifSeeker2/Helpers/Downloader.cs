using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace MotifSeeker2.Helpers
{
    /// <summary>
    /// Загружает и распаковывает файл из инета. Если тот уже загружен и распакован, то возвращает просто его локальный путь.
    /// Если файл не заканчивается на .gz, то не распаковывает.
    /// </summary>
    public class Downloader
    {
        private readonly string _remotePath;

        private readonly string _localDir;

        public Downloader(string remotePath, string localDir)
        {
            _remotePath = remotePath;
            _localDir = localDir;
        }

        /// <summary>
        /// Возвращает загруженный и распакованный файл. Если надо, то загружает и распаковывает.
        /// </summary>
        public string Get()
        {
            // создадим директорию, если её ещё нет
            if (!Directory.Exists(_localDir))
                Directory.CreateDirectory(_localDir);
            // определим, нужно ли вообще что-либо делать
            var fn = Path.GetFileName(_remotePath);
            if(fn == null)
                throw new ArgumentException("Не удалось определить имя файла.");
            var isGz = fn.ToLower().EndsWith(".gz");
            var ret = Path.Combine(_localDir, isGz ? (fn.Substring(0, fn.Length - 3)) : fn);
            if (File.Exists(ret))
                return ret;
            // загрузим из инета
            byte[] tmp;
            Logs.Instance.Info(_remotePath + " downloading...");
            var sw = Stopwatch.StartNew();
            using (var c = new WebClient())
                tmp = c.DownloadData(_remotePath);
            Logs.Instance.Info(_remotePath + " downloaded, dt=" + sw.Elapsed + ", len=" + tmp.Length);
            var tmpPath = Path.Combine(_localDir, fn + ".tmp");
            // подчистим временный файл, если тот остался с прошлой попытки
            File.Delete(tmpPath);
            if (isGz)
            {
                sw.Restart();
                // распакуем во временный файл
                using (var gz = new GZipStream(new MemoryStream(tmp), CompressionMode.Decompress))
                {
                    using (var f = File.Create(tmpPath))
                    {
                        var buf = new byte[1024*1024*10];
                        int len;
                        while ((len = gz.Read(buf, 0, buf.Length)) > 0)
                        {
                            f.Write(buf, 0, len);
                            if (buf.Length != len)
                                break;
                        }
                        Logs.Instance.Info("Data unzipped, dt=" + sw.Elapsed + ", zipLen=" + tmp.Length + ", unzipLen=" + f.Length);
                        f.Flush();
                    }
                }
                
            }
            else // запишем во временный файл
                File.WriteAllBytes(tmpPath, tmp);
            // быстро заменим на нужный путь
            File.Move(tmpPath, ret);
            return ret;
        }
    }
}
