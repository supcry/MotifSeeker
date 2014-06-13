using MotifSeeker.Data.Dna;

namespace MotifSeeker.Motiff
{
    public interface IMotiff
    {
        /// <summary>
        /// �� �������� ������������ ������� �������� ���� ������������� �����.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// ����� ������ � �����������.
        /// </summary>
        int Length { get; }

        double CalcMaxScore(Nucleotide[] data, CalcMode? mode = null);

        double[] CalcMaxScore(Nucleotide[][] data, CalcMode? mode = null);

        double CalcMaxScore(Nucleotide[] data, int startPos, int endPos, CalcMode? mode = null);
    }
}