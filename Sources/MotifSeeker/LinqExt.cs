using System;
using System.Collections.Generic;

namespace MotifSeeker
{
    public static class LinqExt
    {
        public static Tuple<float, float, float> MinMeanMax(this IEnumerable<float> flow)
        {
            float min = float.MaxValue;
            double mean = 0;
            float max = float.MinValue;
            int count = 0;
            foreach (var f in flow)
            {
                if (f > max)
                    max = f;
                if (f < min)
                    min = f;
                mean += f;
                count++;
            }
            if(count == 0)
                return new Tuple<float, float, float>(0,float.NaN,0);
            return new Tuple<float, float, float>(min, (float)(mean/count), max);
        }
    }
}
