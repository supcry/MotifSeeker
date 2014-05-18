using System;
using System.Collections.Generic;

namespace MotifSeeker.Sfx
{
    public interface ITextComparer
    {
        SuffixSubstr[] GetAllCites(byte[] hashes, int minSubstrLength = -1);
    }

    public abstract class TextComparerBase
    {
        protected readonly byte[] _docHashes;

        protected Pointer _pointer;
        protected SuffixArray _suffixArray;

        protected TextComparerBase(byte[] docHashes)
        {
            _docHashes = docHashes;
        }
    }

    public class TextComparer : TextComparerBase, ITextComparer
    {
        private int DocHashesLength { get; set; }

        public TextComparer(byte[] docHashes, SuffixArray suffixArray)
            : this(docHashes, 2, suffixArray)
        {
        }

        public TextComparer(byte[] docHashes, int minSubstrLength, SuffixArray suffixArray = null) : base(docHashes)
        {
            //            _depths = new int[docHashes.Length];
            //            _srcIds = new int[docHashes.Length];

            _suffixArray = suffixArray;

            DocHashesLength = _docHashes.Length;
        }

        public SuffixSubstr[] GetAllCites(byte[] hashes, int minSubstrLength = -1)
        {
			if (minSubstrLength == -1)
				minSubstrLength = hashes.Length;
			Array.Resize(ref hashes, hashes.Length + 1);
            
            hashes[hashes.Length - 1] = byte.MaxValue - 1; // todo перенести в textInfo)
            List<SubPointer> list = FindSubStringsByCheckDoc(hashes, minSubstrLength);
            var result = FindAllSubStrs(list, minSubstrLength);

            result.Sort(new SuffixSubstrComparеrByChkIdx());
            return result.ToArray();
        }

		public List<ElementGroup> GetElementGroups()
	    {
		    return _suffixArray.GetElementGroups();
	    }

		public int StrokeSize { get { return _suffixArray.StrokeSize;  } }


        /// <summary>
        /// С использованием суффиксных ссылок и безопасного нахождения подстрок в проверяемом документе.
        /// </summary>
        private List<SubPointer> FindSubStringsByCheckDoc(byte[] hashes, int minSubstrLength)
        {
            var list = new List<SubPointer>();
            var docLength = DocHashesLength - 1;
            var sa = _suffixArray;

            _pointer = new Pointer(0, 0, docLength);

            var right = 0;
            for (int left = 0; left < hashes.Length; left++)
            {
                while (sa.PointerDown(_pointer, hashes[right], right - left))
                    right++;


                if (right - left >= minSubstrLength)
                    list.Add(new SubPointer(_pointer, left, right - left));

                sa.UseSuffixLink(_pointer);

                while (
                    !((_pointer.Depth + 1 >= right - left) ||
                      !sa.PointerDownByLink(_pointer, hashes[left + _pointer.Depth + 1], right - left)))
                {
                }

                if (left == right)
                    right++;
            }

            return list;
        }


        //todo вынести в отдельный класс
        // НИКАК не избавиться от чтения suftab
        //        private SuffixSubstr[] FindAllSubStrsSafe(List<SubPointer> pointers)
        //        {
        //            foreach (var pointer in pointers)
        //            {
        //                if (pointer.Length > _depths[pointer.Left])
        //                {
        //                    _depths[pointer.Left] = pointer.Length;
        //                    _srcIds[pointer.Left] = pointer.SrcId;
        //                }
        //            }
        //
        //           // var suftab = _suffixArray.Suftab; //todo как избавиться от чтения suftab?
        //           // var lcptab = _suffixArray.Lcptab;
        //            
        //            //слева направо вариант
        //            for (int i = 1; i < _depths.Length; i++)
        //            {
        //                var d = Math.Min(_depths[i - 1], _suffixArray.SrcIdx(i));
        //                if (d >= _minSubstrLength && _depths[i]==0)
        //                {
        //                    _depths[i] = d;
        //                    _srcIds[i] = _srcIds[i - 1];
        //                }
        //            }
        //
        //            //справа налево
        //            for (int i = _depths.Length - 1; i > 0; i--)
        //            {
        //                var d = Math.Min(_depths[i], _suffixArray.SrcIdx(i));
        //                if (d>=_minSubstrLength && d > _depths[i - 1])
        //                {
        //                    _depths[i - 1] = d;
        //                    _srcIds[i - 1] = _srcIds[i];
        //                }
        //            }
        //            var count = _depths.Count(d => d > 0);
        //            var res = new SuffixSubstr[count];
        //            var j = 0;
        //            for (int i = 0; i < _depths.Length; i++)
        //            {
        //                if (_depths[i] >= _minSubstrLength)
        //                {
        //                    var r = new SuffixSubstr
        //                    {
        //                        ChkIdx = suftab[i],  //todo как избавиться от чтения suftab?
        //                        Length = _depths[i],
        //                        SrcIdx = _srcIds[i]
        //                    };
        //                    res[j++] = r;
        //                }
        //            }
        //            Array.Sort(res, new SuffixSubstrComparеrByChkIdx());
        //            return res;
        //        }

        private List<SuffixSubstr> FindAllSubStrs(List<SubPointer> pointers, int minSubstrLength)
        {
            var list = new List<SuffixSubstr>();

            foreach (var p in pointers)
            {
                list.Add(_suffixArray.CreateSuffix(p));

                _suffixArray.AddSuffixesRightToLeft(p, minSubstrLength, list);

                _suffixArray.AddSuffixesLeftToRight(p, minSubstrLength, list);
            }

            return list;
        }

    }
}
