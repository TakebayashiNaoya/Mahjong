using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Cpu
{
    /// <summary>
    /// CPUの打牌選択ロジック（仕様書10.2）
    /// </summary>
    public static class CpuDiscardSelector
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 打牌する牌を選ぶ
        /// </summary>
        /// <param name="hand">現在の手牌（ツモ牌を持つ状態）</param>
        /// <param name="isThreePlayer">三人麻雀かどうか</param>
        /// <param name="mustKeepTenpai">true の場合、打牌後もテンパイを維持できる牌のみを候補にする（リーチ宣言時に使用）</param>
        /// <param name="safeTiles">リーチ中の他家に対する安全牌（現物）の集合</param>
        /// <param name="difficulty">CPU強度</param>
        /// <param name="random">牌選択に使う乱数生成器</param>
        /// <returns>打牌する牌</returns>
        /// <exception cref="ArgumentNullException">hand・safeTiles・random が null の場合</exception>
        public static Tile ChooseDiscard(
            Hand hand,
            bool isThreePlayer,
            bool mustKeepTenpai,
            IReadOnlyCollection<Tile> safeTiles,
            CpuDifficulty difficulty,
            Random random)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            if (safeTiles == null)
            {
                throw new ArgumentNullException(nameof(safeTiles), "safeTiles が null です");
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random), "random が null です");
            }

            var candidates = DistinctByKind(hand.GetClosedTiles()).ToList();

            if (mustKeepTenpai)
            {
                var tenpaiKeeping = candidates.Where(t => CalculateShantenAfterDiscard(hand, t) == 0).ToList();

                if (tenpaiKeeping.Count > 0)
                {
                    candidates = tenpaiKeeping;
                }
            }

            if (difficulty == CpuDifficulty.Easy)
            {
                return candidates[random.Next(candidates.Count)];
            }

            // 安全牌（現物）が候補にあれば、牌効率よりも守備を優先する
            var safeCandidates = candidates.Where(t => safeTiles.Any(s => s.IsSameType(t))).ToList();
            var pool = safeCandidates.Count > 0 ? safeCandidates : candidates;

            var scored = pool
                .Select(t => (
                    Tile: t,
                    Shanten: CalculateShantenAfterDiscard(hand, t),
                    Ukeire: CalculateUkeireAfterDiscard(hand, t, isThreePlayer)))
                .OrderBy(x => x.Shanten)
                .ThenByDescending(x => x.Ukeire)
                .ToList();

            var bestShanten = scored[0].Shanten;
            var bestUkeire = scored[0].Ukeire;
            var topTier = scored.Where(x => x.Shanten == bestShanten && x.Ukeire == bestUkeire).ToList();

            return topTier[random.Next(topTier.Count)].Tile;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 牌の種類ごとに代表牌を1枚だけ残す（同種を何度も試算しないための重複排除）
        /// </summary>
        private static IEnumerable<Tile> DistinctByKind(IReadOnlyList<Tile> tiles)
        {
            return tiles
                .GroupBy(t => t.IsJihai ? (object)t.Id : (t.Suit, t.Number))
                .Select(g => g.First());
        }
        /// <summary>
        /// 指定した牌を打牌した場合のシャンテン数を試算する
        /// 門前手は一時的な Hand を組み立てて七対子・国士無双まで含めて正確に計算し、
        /// 副露済みの手はカウント配列から標準形のみで簡易計算する
        /// </summary>
        private static int CalculateShantenAfterDiscard(Hand hand, Tile discard)
        {
            if (hand.Melds.Count == 0)
            {
                var remaining = new List<Tile>(hand.GetClosedTiles());
                remaining.Remove(discard);

                var tempHand = new Hand();
                tempHand.SetInitialTiles(remaining);
                return ShantenCalculator.Calculate(tempHand);
            }

            var counts = ShantenCalculator.BuildCounts(hand);
            counts[TileKind.IndexOf(discard)]--;
            return ShantenCalculator.CalculateStandardFromCounts(counts, hand.Melds.Count);
        }
        /// <summary>
        /// 指定した牌を打牌した場合の有効牌種類数を試算する
        /// </summary>
        private static int CalculateUkeireAfterDiscard(Hand hand, Tile discard, bool isThreePlayer)
        {
            var counts = ShantenCalculator.BuildCounts(hand);
            counts[TileKind.IndexOf(discard)]--;
            return CpuUkeireCalculator.CountUkeireKinds(counts, hand.Melds.Count, isThreePlayer);
        }
    }
}
