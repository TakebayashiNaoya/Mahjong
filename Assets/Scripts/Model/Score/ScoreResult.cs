using System.Collections.Generic;
using Mahjong.Model.Evaluation;

namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 点数計算の最終結果
    /// </summary>
    public sealed class ScoreResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 符（役満の場合は意味を持たない）
        /// </summary>
        public int Fu { get; }
        /// <summary>
        /// 翻数（役満以外の役の合計翻数＋ドラ。役満の場合は倍率の合計）
        /// </summary>
        public int Han { get; }
        /// <summary>
        /// 満貫以上の区分
        /// </summary>
        public LimitBand Band { get; }
        /// <summary>
        /// 役満が成立しているかどうか
        /// </summary>
        public bool IsYakuman { get; }
        /// <summary>
        /// 役満の倍率（今回は常に1。ダブル役満は未対応）
        /// </summary>
        public int YakumanMultiplier { get; }
        /// <summary>
        /// 基本点
        /// </summary>
        public int BasicPoints { get; }
        /// <summary>
        /// 本場数
        /// </summary>
        public int HonbaCount { get; }
        /// <summary>
        /// 供託されていたリーチ棒の本数
        /// </summary>
        public int RiichiStickCount { get; }
        /// <summary>
        /// 支払い内訳
        /// </summary>
        public PaymentBreakdown Payment { get; }
        /// <summary>
        /// 成立した役の内訳（ドラを除く）
        /// </summary>
        public IReadOnlyList<YakuResult> Yaku { get; }
        /// <summary>
        /// 表ドラ・裏ドラ・北抜きによる翻数の合計。役満成立時は0
        /// </summary>
        public int DoraHan { get; }
        /// <summary>
        /// 赤ドラによる翻数。役満成立時は0
        /// </summary>
        public int AkaDoraHan { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public ScoreResult(
            int fu, int han, LimitBand band, bool isYakuman, int yakumanMultiplier,
            int basicPoints, int honbaCount, int riichiStickCount, PaymentBreakdown payment,
            IReadOnlyList<YakuResult> yaku, int doraHan, int akaDoraHan)
        {
            Fu = fu;
            Han = han;
            Band = band;
            IsYakuman = isYakuman;
            YakumanMultiplier = yakumanMultiplier;
            BasicPoints = basicPoints;
            HonbaCount = honbaCount;
            RiichiStickCount = riichiStickCount;
            Payment = payment;
            Yaku = yaku;
            DoraHan = doraHan;
            AkaDoraHan = akaDoraHan;
        }
    }
}
