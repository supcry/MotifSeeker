using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MotifSeeker.Data.DNaseI;
using ZedGraph;

namespace MotifSeeker.Graph
{
    public static class MergedBarGraph
    {
        public static void DrawPeaks(IEnumerable<MergedNarrowPeak> peaks, int maxPeaksPerBar, int minPos, int maxPos, int width = 800, int height = 600)
        {
            Console.Write("Draw graph...");
            // какие-то настройки
            Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            // подготовка формы и компонента
            var form = new Form {Width = width, Height = height};
            var f = new ZedGraphControl
	        {
	            Anchor = AnchorStyles.Top,
	            AutoSizeMode = AutoSizeMode.GrowAndShrink,
	            Dock = DockStyle.Fill,
                Name = "qwe"
	        };
            f.GraphPane = new GraphPane(RectangleF.Empty, "Сгруппированные пики активности DNaseI в chr1", "p", "q");
            // преобразуем данные
            int maxValue;
            var data = PrepareData(peaks, maxPeaksPerBar, ref minPos, ref maxPos, out maxValue);

            // подготовим оси
            // Y
	        var yaxis = f.GraphPane.YAxis;
            yaxis.Type = AxisType.Log;
	        yaxis.Scale.MaxAuto = false;
            yaxis.Scale.MinAuto = false;
            yaxis.Scale.Max = maxValue;
            yaxis.Scale.Min = 0;
            yaxis.Title = new AxisLabel("peakValue", null, 16, Color.Black, false, false, false);
            // X
            var xaxis = f.GraphPane.XAxis;
            xaxis.Scale.MaxAuto = false;
            xaxis.Scale.MinAuto = false;
	        xaxis.Scale.Max = maxPos;
            xaxis.Scale.Min = minPos;
            xaxis.Type = AxisType.LinearAsOrdinal;
            xaxis.Title = new AxisLabel("chr1 position", null, 16, Color.Black, true, false, false);

            // Установим формат столбцов
            f.GraphPane.BarSettings.Type = BarType.SortedOverlay;

            var cs = new Color[data.Length];
            for (int i = 0; i < data.Length; i++)
                cs[i] = Color.FromArgb((int) Math.Round(255.0*(i + 0.5)/data.Length), 0, 0, 0);

            // Добавим столбцы
            for (int i = 0; i < data.Length; i++)
            {
                var bar = f.GraphPane.AddBar("Совпад.№" + i, data[i], cs[i]);
                bar.Bar.Fill.Type = FillType.Solid;
                bar.Bar.Border.IsVisible = false;
                bar.Bar.Border.Width = 1500;
            }

            // Финальный штрих
            form.Controls.Add(f);
            f.AxisChange();
            f.Invalidate();
            Console.WriteLine("ok");
            Application.Run(form);
        }

        /// <summary>
        /// Подготавливает данные для их отображения на графике.
        /// </summary>
        private static IPointList[] PrepareData(IEnumerable<MergedNarrowPeak> peaks, int maxPeaksPerBar,
                                         ref int minPos, ref int maxPos, out int maxValue)
        {
            maxValue = 0;
            var minPos2 = int.MaxValue;
            var maxPos2 = int.MinValue;
            var data = new List<KeyValuePair<int, int>>[maxPeaksPerBar];
            for (int i = 0; i < data.Length; i++)
                data[i] = new List<KeyValuePair<int, int>>();
            foreach (var peak in peaks)
            {
                if(peak.StartPos < minPos)
                    continue;
                if (peak.EndPos > maxPos)
                    break;

                if (peak.StartPosMin < minPos2)
                    minPos2 = peak.StartPosMin;
                if (peak.EndPosMax > maxPos2)
                    maxPos2 = peak.EndPosMax;

                int id = 0;
                foreach (var p in peak.Values1.OrderByDescending(p => p).Take(maxPeaksPerBar))
                {
                    if (p > maxValue)
                        maxValue = (int)p;
                    data[id++].Add(new KeyValuePair<int, int>((peak.EndPos + peak.StartPos)/2, (int) Math.Round(p)));
                }
            }
            int cnt = data.Count(p => p.Count > 0);
            var ret = new IPointList[cnt];
            for (int i = 0; i < cnt; i++)
            {
                ret[i] = new PointPairList(data[i].Select(p => (double)p.Key).ToArray(),
                                           data[i].Select(p => (double)p.Value).ToArray());
            }
            minPos = minPos2;
            maxPos = maxPos2;
            return ret;
        }
    }
}
