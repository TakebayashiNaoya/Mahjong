using Mahjong.Model.Evaluation;

namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 符・翻数（または役満）から基本点と満貫以上の区分を求める
    /// </summary>
    public readonly struct PointTableResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 基本点
        /// </summary>
        public int BasicPoints { get; }
        /// <summary>
        /// 満貫以上の区分
        /// </summary>
        public LimitBand Band { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public PointTableResult(int basicPoints, LimitBand band)
        {
            BasicPoints = basicPoints;
            Band = band;
        }
    }

    /// <summary>
    /// 符・翻数（または役満）から基本点と満貫以上の区分を求める
    /// </summary>
    public static class PointTable
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 役満1つ分の基本点
        /// </summary>
        private const int YAKUMAN_BASIC_POINT = 8000;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 符・翻数から基本点と満貫以上の区分を求める
        /// </summary>
        /// <param name="fu">符（役満の場合は無視される）</param>
        /// <param name="han">翻数（役満の場合は無視される）</param>
        /// <param name="isYakuman">役満が成立しているかどうか</param>
        /// <param name="yakumanMultiplier">役満の倍率（通常は1。ダブル役満等は今回未対応のため常に1として扱う）</param>
        /// <returns>基本点と区分</returns>
        public static PointTableResult Calculate(int fu, int han, bool isYakuman, int yakumanMultiplier)
        {
            var basicPoints = isYakuman
                ? YAKUMAN_BASIC_POINT * yakumanMultiplier
                : BasicPointFormula.Calculate(fu, han);

            return new PointTableResult(basicPoints, BandFromBasicPoints(basicPoints));
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 基本点から満貫以上の区分を求める
        /// </summary>
        private static LimitBand BandFromBasicPoints(int basicPoints)
        {
            if (basicPoints >= YAKUMAN_BASIC_POINT)
            {
                return LimitBand.Yakuman;
            }

            return basicPoints switch
            {
                >= 6000 => LimitBand.Sanbaiman,
                >= 4000 => LimitBand.Baiman,
                >= 3000 => LimitBand.Haneman,
                >= 2000 => LimitBand.Mangan,
                _ => LimitBand.None,
            };
        }
    }
}
