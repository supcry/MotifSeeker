using System.Collections.Generic;
using System.Linq;
using MotifSeeker.Data.Dna;
using MotifSeeker.Data.DNaseI;

namespace MotifSeeker.Sfx
{
    /// <summary>
    /// Менеджер суффиксных массивов.
    /// Если результат уже есть в кэше, то берёт готовый. Если нет, то сначала строит и кэширует.
    /// 
    /// [ToDo] Добавить кэгирование, как только всё заработает и будет протестировано.
    /// </summary>
    public class SuffixManager
    {
        /// <summary>
        /// Строит и возвращает суф-массив построенный на цепочках класса присутствия.
        /// </summary>
        public static SuffixArray GetSfxArray(ChromosomeEnum chrId, Dictionary<string, string> dnaseFilterAttrs)
        {
            var regions = DNaseIManager.GetClassifiedRegions(chrId, dnaseFilterAttrs, false);
            var chr = ChrManager.GetChromosome(chrId);
            var present =
                regions[ClassifiedRegion.MotifContainsStatus.Present].Select(
                    p => chr.GetPack(p.StartPos, p.EndPos - p.StartPos)).ToArray();
            return SuffixBuilder.BuildMany(present);
        }

        /// <summary>
        /// Строит и возвращает суф-массив построенный на всей хромосоме
        /// </summary>
        public static SuffixArray GetSfxArray(ChromosomeEnum chrId)
        {
            var chr = ChrManager.GetChromosome(chrId);
            return SuffixBuilder.BuildOne(chr.GetPack(0, chr.Count));
        }
    }
}
