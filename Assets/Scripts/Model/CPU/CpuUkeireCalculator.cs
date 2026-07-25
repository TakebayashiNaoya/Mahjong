using System;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Hands;

namespace Mahjong.Model.Cpu
{
    /// <summary>
    /// 手牌の有効牌（シャンテン数を減らせる牌）の種類数を数える
    /// 標準形のみを基準にした簡略版の牌効率計算（七対子・国士無双は考慮しない）
    /// </summary>
    public static class CpuUkeireCalculator
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 現在の手牌に対する有効牌の種類数を数える
        /// </summary>
        /// <param name="hand">判定対象の手牌（打牌後、ツモ牌を持たない状態を想定）</param>
        /// <param name="isThreePlayer">三人麻雀かどうか（萬子2〜8を候補から除外する）</param>
        /// <returns>シャンテン数を1つ以上減らせる牌の種類数</returns>
        /// <exception cref="ArgumentNullException">hand が null の場合</exception>
        public static int CountUkeireKinds(Hand hand, bool isThreePlayer)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            return CountUkeireKinds(ShantenCalculator.BuildCounts(hand), hand.Melds.Count, isThreePlayer);
        }
        /// <summary>
        /// 34種のカウント配列に対する有効牌の種類数を数える
        /// Hand/Meld を実際に組み立てずに「この牌を切ったら」を試算したい CpuDiscardSelector から、
        /// 副露済みの手牌でも使えるようにするための経路
        /// </summary>
        /// <param name="counts">34種のカウント配列（このメソッドは変更しない）</param>
        /// <param name="meldCount">既存の副露数</param>
        /// <param name="isThreePlayer">三人麻雀かどうか（萬子2〜8を候補から除外する）</param>
        public static int CountUkeireKinds(int[] counts, int meldCount, bool isThreePlayer)
        {
            if (counts == null)
            {
                throw new ArgumentNullException(nameof(counts), "counts が null です");
            }

            var working = (int[])counts.Clone();
            var baseShanten = ShantenCalculator.CalculateStandardFromCounts(working, meldCount);
            var ukeireKinds = 0;

            for (var kindIndex = 0; kindIndex < TileKind.KIND_COUNT; kindIndex++)
            {
                if (isThreePlayer && IsUnavailableInThreePlayer(kindIndex))
                {
                    continue;
                }

                working[kindIndex]++;
                var shantenAfterDraw = ShantenCalculator.CalculateStandardFromCounts(working, meldCount);
                working[kindIndex]--;

                if (shantenAfterDraw < baseShanten)
                {
                    ukeireKinds++;
                }
            }

            return ukeireKinds;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 三人麻雀に存在しない種類（萬子2〜8）かどうかを判定する
        /// </summary>
        private static bool IsUnavailableInThreePlayer(int kindIndex)
        {
            return kindIndex >= TileKind.MANZU_OFFSET + 1 && kindIndex <= TileKind.MANZU_OFFSET + 7;
        }
    }
}
