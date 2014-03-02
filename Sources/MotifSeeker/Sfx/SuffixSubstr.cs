namespace MotifSeeker.Sfx
{
    public class SuffixSubstr
    {
        public int ChkIdx;                       // Индекс в проверямом тексте
        public int SrcIdx;                       // Индекс в источнике
        public int Length;                       // Длина в словах
        public override string ToString()
        {
            return ChkIdx + " : " + SrcIdx + " : " + Length;
        }
    }
}