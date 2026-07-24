using System;
using Mahjong.Model.Evaluation.Internal;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 手牌のシャンテン数を計算する
    /// 標準形・七対子形・国士無双形のうち最小値を返す
    /// </summary>
    public static class ShantenCalculator
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 面子の最大数
        /// </summary>
        private const int MAX_MELDS = 4;
        /// <summary>
        /// 七対子に必要な対子数
        /// </summary>
        private const int CHIITOITSU_REQUIRED_PAIRS = 7;
        /// <summary>
        /// 国士無双に必要な么九牌の種類数（1・9・字牌で13種）
        /// </summary>
        private const int KOKUSHI_REQUIRED_KINDS = 13;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 手牌のシャンテン数を計算する
        /// -1 は和了形であることを表す
        /// </summary>
        /// <param name="hand">計算対象の手牌</param>
        /// <returns>標準形・七対子形・国士無双形のうち最小のシャンテン数</returns>
        /// <exception cref="ArgumentNullException">hand が null の場合</exception>
        public static int Calculate(Mahjong.Model.Hands.Hand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            var counts = BuildCounts(hand);
            var shanten = CalculateStandard(counts, hand.Melds.Count);

            // 七対子・国士無双は門前限定
            if (hand.Melds.Count == 0)
            {
                shanten = Math.Min(shanten, CalculateChiitoitsu(counts));
                shanten = Math.Min(shanten, CalculateKokushi(counts));
            }

            return shanten;
        }


        // ========================================
        // 内部メソッド（AgariChecker・WaitingTileFinder等から共有利用）
        // ========================================
        /// <summary>
        /// 手牌の門前牌（手牌+ツモ牌）を34種のカウント配列に変換する
        /// </summary>
        /// <param name="hand">対象の手牌</param>
        /// <returns>34種のカウント配列</returns>
        internal static int[] BuildCounts(Mahjong.Model.Hands.Hand hand)
        {
            var counts = new int[TileKind.KIND_COUNT];

            foreach (var tile in hand.GetClosedTiles())
            {
                counts[TileKind.IndexOf(tile)]++;
            }

            return counts;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 標準形のシャンテン数を計算する
        /// 対子を雀頭として使う解釈・搭子ブロックとして使う解釈の両方を数式評価時に比較し、
        /// 「頭を確保しそこねる」端数バグを構造的に避ける
        /// </summary>
        private static int CalculateStandard(int[] counts, int existingMeldCount)
        {
            var shapes = HandDecomposer.EnumerateShapes(counts);
            var best = int.MaxValue;

            foreach (var shape in shapes)
            {
                var effectiveMelds = shape.Melds + existingMeldCount;
                var blocksCap = MAX_MELDS - effectiveMelds;

                // 雀頭を確保せず、対子もすべて搭子ブロックとして扱う解釈
                var withoutHead = 8 - 2 * effectiveMelds - Math.Min(shape.Blocks, blocksCap);
                best = Math.Min(best, withoutHead);

                // 対子のうち1つを雀頭として確保する解釈
                if (shape.HasPair)
                {
                    var withHead = 8 - 2 * effectiveMelds - Math.Min(shape.Blocks - 1, blocksCap) - 1;
                    best = Math.Min(best, withHead);
                }
            }

            return best;
        }
        /// <summary>
        /// 七対子形のシャンテン数を計算する
        /// 同一種類4枚を2対子とは数えない（distinctKinds/pairKindsともに種類単位でカウントするため）
        /// </summary>
        private static int CalculateChiitoitsu(int[] counts)
        {
            var pairKinds = 0;
            var distinctKinds = 0;

            foreach (var count in counts)
            {
                if (count > 0)
                {
                    distinctKinds++;
                }

                if (count >= 2)
                {
                    pairKinds++;
                }
            }

            return (CHIITOITSU_REQUIRED_PAIRS - 1) - pairKinds + Math.Max(0, CHIITOITSU_REQUIRED_PAIRS - distinctKinds);
        }
        /// <summary>
        /// 国士無双形のシャンテン数を計算する
        /// </summary>
        private static int CalculateKokushi(int[] counts)
        {
            var kinds = 0;
            var hasPair = false;

            for (var kindIndex = 0; kindIndex < TileKind.KIND_COUNT; kindIndex++)
            {
                if (!TileKind.IsYaochu(kindIndex) || counts[kindIndex] == 0)
                {
                    continue;
                }

                kinds++;

                if (counts[kindIndex] >= 2)
                {
                    hasPair = true;
                }
            }

            return KOKUSHI_REQUIRED_KINDS - kinds - (hasPair ? 1 : 0);
        }
    }
}
