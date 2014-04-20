using System.Collections.Generic;

namespace MotifSeeker.Sfx
{
    public class SuffixSubstr
    {
        public int ChkIdx;                       // Индекс в проверямом тексте
        public int SrcIdx;                       // Индекс в источнике
        public int Length;                       // Длина в словах

        public SuffixSubstr()
        { }

        public SuffixSubstr(int chkId, int srcId, int length)
        {
            ChkIdx = chkId;
            SrcIdx = srcId;
            Length = length;
        }

        public SuffixSubstr(uint chkId, int srcId, int length)
        {
            ChkIdx = (int)chkId;
            SrcIdx = srcId;
            Length = length;
        }

        public SuffixSubstr(int chkId, uint srcId, int length)
        {
            ChkIdx = chkId;
            SrcIdx = (int)srcId;
            Length = length;
        }

        public override string ToString()
        {
            return ChkIdx + " : " + SrcIdx + " : " + Length;
        }
    }

    public class SuffixSubstrComparеrByChkIdx : IComparer<SuffixSubstr>
    {
        public int Compare(SuffixSubstr x, SuffixSubstr y)
        {
            if (x.ChkIdx > y.ChkIdx) return 1;
            if (x.ChkIdx < y.ChkIdx) return -1;
            //Длина от большего к меньшему
            if (x.Length < y.Length) return 1;
            if (x.Length > y.Length) return -1;
            //SrcIdx от меньшего к большему
            if (x.SrcIdx > y.SrcIdx) return 1;
            if (x.SrcIdx < y.SrcIdx) return -1;

            return 0;
        }
    }
}