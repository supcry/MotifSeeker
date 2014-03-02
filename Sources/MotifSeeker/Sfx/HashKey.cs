namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Класс является ключем для хештаблицы.
    /// Содержит интервал суффиксного массива, на котором встречается Хеш.
    /// </summary>
    public class HashKey
    {
        public readonly uint Hash;
        public readonly int Left;
        public readonly int Right;

        public HashKey(uint hash, int left, int right)
        {
            Hash = hash;
            Left = left;
            Right = right;
        }

        public HashKey(uint hash, SfxArray.LcpTree interval)
        {
            Hash = hash;
            Left = interval.LeftBound;
            Right = interval.RightBound;
        }

        /// <summary>
        /// ToDo заменить на что-то более достойное.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)((Left + Right << 16) | Hash);
        }

        public override bool Equals(object obj)
        {
            var hk = obj as HashKey;
            return hk != null &&
                   hk.Hash == Hash &&
                   hk.Left == Left &&
                   hk.Right == Right;
        }

        public override string ToString()
        {
            return (Hash + " ; " + Left + " - " + Right);
        }
    }
}