namespace MotifSeeker.Data.DNaseI
{
    /// <summary>
    /// Регион, классифицированный в эксперименте.
    /// </summary>
    public class ClassifiedRegion
    {
        /// <summary>
        /// Статус классификации.
        /// </summary>
        public readonly MotifContainsStatus Status;

        /// <summary>
        /// Начальное положение региона.
        /// </summary>
        public readonly int StartPos;

        /// <summary>
        /// Конечное положение региона.
        /// </summary>
        public readonly int EndPos;

        public float RawValue1;

        public float RawValue2;

        public ClassifiedRegion(bool? present, int start, int end, float raw1, float raw2)
        {
            StartPos = start;
            EndPos = end;
            RawValue1 = raw1;
            RawValue2 = raw2;
            if (!present.HasValue)
                Status = MotifContainsStatus.Unknown;
            else
                Status = present.Value ? MotifContainsStatus.Present : MotifContainsStatus.NotPresent;
        }

        /// <summary>
        /// Статус классификации.
        /// </summary>
        public enum MotifContainsStatus
        {
            /// <summary>
            /// Маркер/мотив присутствует в регионе.
            /// </summary>
            Present,

            /// <summary>
            /// Не присутствует в регионе.
            /// </summary>
            NotPresent,

            /// <summary>
            /// Статус пока не ясен.
            /// </summary>
            Unknown
        }
    }
}
