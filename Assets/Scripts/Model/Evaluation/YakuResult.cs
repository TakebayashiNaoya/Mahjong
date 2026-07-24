namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 成立した役1つ分の情報
    /// </summary>
    public readonly struct YakuResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 役の識別子
        /// </summary>
        public YakuId Id { get; }
        /// <summary>
        /// 翻数（役満の場合は無視され、YakumanMultiplier が使われる）
        /// </summary>
        public int Han { get; }
        /// <summary>
        /// 役満かどうか
        /// </summary>
        public bool IsYakuman { get; }
        /// <summary>
        /// 役満倍率（ダブル役満等の将来拡張用。通常の役満は1）
        /// </summary>
        public int YakumanMultiplier { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public YakuResult(YakuId id, int han, bool isYakuman = false, int yakumanMultiplier = 1)
        {
            Id = id;
            Han = han;
            IsYakuman = isYakuman;
            YakumanMultiplier = yakumanMultiplier;
        }
    }
}
