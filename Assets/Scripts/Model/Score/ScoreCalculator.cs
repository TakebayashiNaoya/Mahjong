using System;
using System.Linq;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 和了時の点数（符・翻数・満貫以上の区分・支払い内訳）を計算する
    /// </summary>
    public static class ScoreCalculator
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 和了時の点数を計算する
        /// </summary>
        /// <param name="hand">和了した手牌</param>
        /// <param name="winningTile">和了牌（ロンの場合は手牌にまだ含まれていない牌）</param>
        /// <param name="yakuResult">YakuEvaluator.Evaluate の結果</param>
        /// <param name="context">状況フラグ</param>
        /// <param name="honbaCount">本場数</param>
        /// <param name="riichiStickCount">供託されているリーチ棒の本数</param>
        /// <param name="indicatorDoraHan">
        /// 表ドラ・裏ドラによる翻数。王牌・リーチ状態と連携するGame進行層が算出して渡す。
        /// 今回のマイルストーンでは未実装のため、呼び出し元は0を渡すことを想定している
        /// </param>
        /// <param name="playerCount">参加人数（3人麻雀は3、4人麻雀は4）</param>
        /// <returns>点数計算の結果</returns>
        /// <exception cref="ArgumentNullException">hand・winningTile・yakuResult が null の場合</exception>
        public static ScoreResult Calculate(
            Hand hand,
            Tile winningTile,
            YakuEvaluationResult yakuResult,
            WinContext context,
            int honbaCount,
            int riichiStickCount,
            int indicatorDoraHan = 0,
            int playerCount = 4)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            if (winningTile == null)
            {
                throw new ArgumentNullException(nameof(winningTile), "winningTile が null です");
            }

            if (yakuResult == null)
            {
                throw new ArgumentNullException(nameof(yakuResult), "yakuResult が null です");
            }

            var akaDoraHan = CountAkaDora(hand, winningTile, context);
            var yakumanMultiplier = yakuResult.IsYakuman
                ? yakuResult.Yaku.Sum(y => y.YakumanMultiplier)
                : 1;
            var effectiveHan = yakuResult.IsYakuman
                ? yakuResult.TotalHan
                : yakuResult.TotalHan + akaDoraHan + indicatorDoraHan;

            var table = PointTable.Calculate(yakuResult.Fu, effectiveHan, yakuResult.IsYakuman, yakumanMultiplier);
            var payment = PaymentCalculator.Calculate(
                table.BasicPoints, context.IsDealer, context.IsTsumo, honbaCount, riichiStickCount, playerCount);

            return new ScoreResult(
                yakuResult.Fu, effectiveHan, table.Band, yakuResult.IsYakuman, yakumanMultiplier,
                table.BasicPoints, honbaCount, riichiStickCount, payment);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 赤ドラの枚数を数える
        /// ロンの場合、和了牌はまだ手牌に含まれていないため明示的にマージする
        /// </summary>
        private static int CountAkaDora(Hand hand, Tile winningTile, WinContext context)
        {
            var allTiles = hand.GetAllTiles();

            if (!context.IsTsumo)
            {
                allTiles.Add(winningTile);
            }

            return allTiles.Count(t => t.IsRed);
        }
    }
}
