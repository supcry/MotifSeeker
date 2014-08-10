using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MotifSeeker2.Helpers
{
    /// <summary>
    /// Коллекция функций по конвертации человеческого представления данных в машинное и обрабтно.
    /// </summary>
    public static class HumanReadability
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string GetHumanSize(long size, string item = "B")
        {
            Debug.Assert(item != null);
            if (size < 1024)
                return size + item;
            size >>= 10;
            if (size < 1024)
                return size + "k" + item;
            size >>= 10;
            if (size < 1024)
                return size + "M" + item;
            size >>= 10;
            if (size < 1024)
                return size + "G" + item;
            size >>= 10;
            if (size < 1024)
                return size + "T" + item;
            size >>= 10;
            if (size < 1024)
                return size + "E" + item;
            return "тьма_тьмущая" + item;
        }

        public static string GetHumanSizeStrict(long size, string item = "B")
        {
            Debug.Assert(item != null);
            if (size < 1024 || size%1024 != 0)
                return size + item;
            size >>= 10;
            if (size < 1024 || size % 1024 != 0)
                return size + "k" + item;
            size >>= 10;
            if (size < 1024 || size % 1024 != 0)
                return size + "M" + item;
            size >>= 10;
            if (size < 1024 || size % 1024 != 0)
                return size + "G" + item;
            size >>= 10;
            if (size < 1024 || size % 1024 != 0)
                return size + "T" + item;
            size >>= 10;
            return size + "E" + item;
        }

        public static long GetSizeFromString(string str, string item = "B")
        {
            Debug.Assert(item != null);
            if(str == null || str.Length < item.Length + 1 || !str.EndsWith(item))
                throw new Exception("Невозможно определить количество " + item + " в строке '" + str + "'.1");
            var sfx = str[str.Length - 2];
            var power = 0;
            string toInt;
            if (Char.IsDigit(sfx))
                toInt = str.Substring(0, str.Length - 1);
            else
            {
                toInt = str.Substring(0, str.Length - 2);
                switch (sfx)
                {
                    case 'k':
                        power = 10;
                        break;
                    case 'M':
                        power = 20;
                        break;
                    case 'G':
                        power = 30;
                        break;
                    case 'T':
                        power = 40;
                        break;
                    case 'E':
                        power = 50;
                        break;
                    default:
                        throw new Exception("Невозможно определить количество " + item + " в строке '" + str + "'.2");
                }
            }
            long factor;
            if(!long.TryParse(toInt, out factor))
                throw new Exception("Невозможно определить количество " + item + " в строке '" + str + "'.3");
            return factor << power;
        }

        public static long TryGetSizeFromString(string str, long defaultFalue, string nameToWarning = null, string item = "B")
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                    return defaultFalue;
                if(item != "" && !str.EndsWith(item))
                    throw new Exception();
                return GetSizeFromString(str, item);
            }catch(Exception)
            {
                if(nameToWarning != null)
                    Logger.Warn("Ошибка при парсинге параметра " + nameToWarning + ":" +
                                       " Значение должно быть как 1235MB (маска XXX{,k,M,G,T}" + item + "), а было " + str +
                                       " .Задаётся значение по умолчанию: " + defaultFalue);
                return defaultFalue;
            }
        }

        public static long? TryGetSizeFromStringZ(string str, string nameToWarning = null, string item = "B")
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                    return null;
                if (item != "" && !str.EndsWith(item))
                    throw new Exception();
                return GetSizeFromString(str, item);
            }
            catch (Exception)
            {
                if (nameToWarning != null)
                    Logger.Warn("Ошибка при парсинге параметра " + nameToWarning + ":" +
                                       " Значение должно быть как 1235MB (маска XXX{,k,M,G,T}" + item + "), а было " + str);
                return null;
            }
        }

        public static string GetHumanDeltaTime(TimeSpan dt)
        {
            if (dt.TotalSeconds < 1)
                return (int)dt.TotalMilliseconds + "msec";
            if (dt.TotalMinutes < 1)
                return Math.Round(dt.TotalSeconds, 2) + "sec";
            if(dt.TotalHours < 1)
                return Math.Round(dt.TotalMinutes, 1) + "min";
            if(dt.TotalDays < 1)
                return Math.Round(dt.TotalHours, 1) + "hrs";
            if (dt.TotalDays < 7)
                return Math.Round(dt.TotalDays, 1) + "days";
            return dt.ToString();
        }

        public static string GetHumanDeltaTime(DateTime t)
        {
            return GetHumanDeltaTime(DateTime.Now - t);
        }

        public static string GetHumanSpeed(long len, DateTime t, string items = "B")
        {
            return GetHumanSpeed(len, DateTime.Now - t, items);
        }

        public static string GetHumanSpeed(long len, TimeSpan t, string items = "B")
        {
            var tmp = len/(t.TotalSeconds);
            if (tmp < 0.1)
                return Math.Round(tmp * 1024.0, 2).ToString("F2") + "m" + items + "/s";
            if (tmp < 1024)
                return Math.Round(tmp, 2).ToString("F2") + items + "/s";
            var tmp2 = tmp/1024.0;
            if (tmp2 < 1024)
                return Math.Round(tmp2, 2).ToString("F2") + "k" + items + "/s";
            var tmp3 = tmp2/1024.0;
            if (tmp3 < 1024)
                return Math.Round(tmp3, 2).ToString("F2") + "M" + items + "/s";
            var tmp4 = tmp3 / 1024.0;
            if (tmp4 < 1024)
                return Math.Round(tmp4, 2).ToString("F2") + "G" + items + "/s";
            return Math.Round(tmp4/1024.0, 2).ToString("F2") + "T" + items + "/s";
        }

        public static string GetDateForName(DateTime d, string sep = "")
        {
            var sb = new StringBuilder();
            sb.Append(d.Year);
            sb.Append(sep);
            sb.Append(d.Month.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
            sb.Append(sep);
            sb.Append(d.Day.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
            return sb.ToString();
        }

        public static string GetTimeForName(DateTime d, string sep = "")
        {
            var sb = new StringBuilder();
            sb.Append(d.Hour.ToString(CultureInfo.InvariantCulture).PadLeft(2,'0'));
            sb.Append(sep);
            sb.Append(d.Minute.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
            sb.Append(sep);
            sb.Append(d.Second.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'));
            sb.Append(sep);
            sb.Append(d.Millisecond.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'));
            return sb.ToString();
        }

        public static string ToHuman(this TimeSpan dt)
        {
            return GetHumanDeltaTime(dt);
        }

        public static string ToHuman(this int val, string items = "B")
        {
            return GetHumanSize(val, items);
        }

        public static string ToHuman(this long val, string items = "B")
        {
            return GetHumanSize(val, items);
        }
    }
}
