using MotifSeeker.Data.Dna;

namespace MotifSeeker.Motiff
{
    public interface IMotiff
    {
        /// <summary>
        /// На скольких элементарных мотивах построен этот вероятностный мотив.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Длина мотива в нуклеотидах.
        /// </summary>
        int Length { get; }

        double CalcMaxScore(Nucleotide[] data, CalcMode? mode = null);

        double[] CalcMaxScore(Nucleotide[][] data, CalcMode? mode = null);

        double CalcMaxScore(Nucleotide[] data, int startPos, int endPos, CalcMode? mode = null);
    }
}