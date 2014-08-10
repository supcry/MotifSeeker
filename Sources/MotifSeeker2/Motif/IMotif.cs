using MotifSeeker2.Dna;

namespace MotifSeeker2.Motif
{
    /// <summary>
    /// Интерфейс мотива.
    /// </summary>
    public interface IMotif
    {
        /// <summary>
        /// Длина мотива. Минимальное количество нуклеотид, необходимых для работы мотива.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Получение оценки комплиментарности мотива к заданной последовательности.
        /// Длина последовательности должна быть равна Length мотива.
        /// </summary>
        float GetScore(Nucleotide[] nucs);
    }
}
