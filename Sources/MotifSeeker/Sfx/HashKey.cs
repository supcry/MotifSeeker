using System;

namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Класс является ключем для хештаблицы.
    /// Содержит интервал суффиксного массива, на котором встречается Хеш.
    /// </summary>
    public struct HashKey : IEquatable<HashKey>
    {
        public readonly int Left;
        public readonly int Right;
	    private readonly int _hashCode;
		public readonly byte Hash;

        public HashKey(byte hash, int left, int right)
        {
            Hash = hash;
            Left = left;
            Right = right;
	        _hashCode = (int) ((Left + Right << 16) | Hash);
        }

        public HashKey(byte hash, SfxArray.LcpTree interval)
        {
            Hash = hash;
            Left = interval.LeftBound;
            Right = interval.RightBound;
			_hashCode = (int)((Left + Right << 16) | Hash);
        }

        /// <summary>
        /// ToDo заменить на что-то более достойное.
        /// </summary>
        public override int GetHashCode()
        {
            return _hashCode;
        }

		//public override bool Equals(object obj)
		//{
		//	var hk = obj as HashKey;
		//	return hk != null &&
		//		   hk.Hash == Hash &&
		//		   hk.Left == Left &&
		//		   hk.Right == Right;
		//}

	    public bool Equals(HashKey hk)
	    {
		    return hk.Hash == Hash &&
		           hk.Left == Left &&
		           hk.Right == Right;
	    }

	    public override string ToString()
        {
            return (Hash + " ; " + Left + " - " + Right);
        }
    }
}