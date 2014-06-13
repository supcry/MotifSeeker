using System;
using System.Collections.Generic;
using System.Linq;

namespace MotifSeeker
{
    public static class RandomExt
    {
        public static IEnumerable<int> GetShuffleFlow(this Random rnd, IEnumerable<int> ids)
        {
            var tmp = ids.ToArray();
            int cnt = tmp.Length;
            if (cnt == 0)
            {
                yield return tmp[0];
                yield break;
            }
            while (cnt > 0)
            {
                var r = rnd.Next(cnt);
                yield return tmp[r];
                if(r != cnt-1)
                    tmp[r] = tmp[cnt - 1];
                cnt--;
                if (cnt == 1)
                {
                    yield return tmp[0];
                    yield break;
                }
            }
        }

        public static IEnumerable<int> GetShuffleFlow(this Random rnd, int cnt)
        {
            var ids = Enumerable.Range(0, cnt);
            return GetShuffleFlow(rnd, ids);
        } 
    }
}
