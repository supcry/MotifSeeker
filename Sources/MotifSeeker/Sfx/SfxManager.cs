﻿using System.Collections.Generic;
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
    public class SfxManager
    {
        /// <summary>
        /// Строит и возвращает суф-массив построенный на цепочках класса присутствия.
        /// </summary>
        public static SfxArray GetSfxArray(ChromosomeEnum chrId, Dictionary<string, string> dnaseFilterAttrs)
        {
            var regions = DNaseIManager.GetClassifiedRegions(chrId, dnaseFilterAttrs, false);
            var chr = ChrManager.GetChromosome(chrId);
            var present =
                regions[ClassifiedRegion.MotifContainsStatus.Present].Select(
                    p => chr.GetPack(p.StartPos, p.EndPos - p.StartPos)).ToArray();
            var builder = new SfxBuilder();
            return builder.BuildMany(present);
        }

        /// <summary>
        /// Строит и возвращает суф-массив построенный на всей хромосоме
        /// </summary>
        public static SfxArray GetSfxArray(ChromosomeEnum chrId)
        {
            var chr = ChrManager.GetChromosome(chrId);
            var builder = new SfxBuilder();
            return builder.BuildOne(chr.GetPack(0, chr.Count));
        }
    }
}
