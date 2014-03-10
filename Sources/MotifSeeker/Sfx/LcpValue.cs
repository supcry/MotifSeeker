namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Значения для хештаблицы. Хранит интервал и минимальную длину общего префикса суфиксов (lcp)
    /// </summary>
    public struct LcpValue
    {
        public readonly int Lcp;
        public readonly int Left;
        public readonly int Right;

        public LcpValue(int lcp, int left, int right)
        {
            Lcp = lcp;
            Left = left;
            Right = right;
        }

        public LcpValue(SfxArray.LcpTree interval)
        {
            Lcp = interval.Lcp;
            Left = interval.LeftBound;
            Right = interval.RightBound;
        }

        public override string ToString()
        {
            return (Lcp + " ; " + Left + " - " + Right);
        }
    }
}