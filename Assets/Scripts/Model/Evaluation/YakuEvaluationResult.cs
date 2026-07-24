using System.Collections.Generic;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 役判定の最終結果
    /// 複数の HandDecomposition のうち、実際の点数（役満優先、次に符×翻数）が最も高いものを
    /// 採用した結果を表す
    /// </summary>
    public sealed class YakuEvaluationResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 成立した役の一覧
        /// 役満が1つでも成立している場合、通常役は含まれない
        /// </summary>
        public IReadOnlyList<YakuResult> Yaku { get; }
        /// <summary>
        /// 合計翻数
        /// 役満成立時は YakumanMultiplier の合計（通常は1、ダブル役満等は2以上）
        /// </summary>
        public int TotalHan { get; }
        /// <summary>
        /// 役満が成立しているかどうか
        /// </summary>
        public bool IsYakuman { get; }
        /// <summary>
        /// 採用された分解パターン
        /// </summary>
        public HandDecomposition BestDecomposition { get; }
        /// <summary>
        /// 採用された分解パターンの符
        /// 役満の場合は符を使わないため 0（FuCalculator のセンチネル値をそのまま反映）
        /// </summary>
        public int Fu { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public YakuEvaluationResult(
            IReadOnlyList<YakuResult> yaku, int totalHan, bool isYakuman, HandDecomposition bestDecomposition, int fu)
        {
            Yaku = yaku;
            TotalHan = totalHan;
            IsYakuman = isYakuman;
            BestDecomposition = bestDecomposition;
            Fu = fu;
        }
    }
}
