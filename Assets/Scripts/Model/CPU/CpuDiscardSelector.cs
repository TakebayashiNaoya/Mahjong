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

            // 手牌の種類ごとに代表牌を1枚だけ残す（同種を何度も試算しないための重複排除）
            var candidates = DistinctByKind(hand.GetClosedTiles()).ToList();
            // 打牌後もテンパイを維持できる牌のみを候補にする場合は、シャンテン数が0になる牌のみに絞り込む
            if (mustKeepTenpai)
            {
                // 打牌後もテンパイを維持できる牌の一覧を取得する
                var tenpaiKeeping = candidates.Where(t => CalculateShantenAfterDiscard(hand, t) == 0).ToList();
                // 打牌後もテンパイを維持できる牌が1種類以上あれば、候補をその牌のみに絞り込む
                if (tenpaiKeeping.Count > 0)
                {
                    candidates = tenpaiKeeping;
                }
            }
            // CPU強度が Easy の場合は、候補の中からランダムに1枚選ぶ
            if (difficulty == CpuDifficulty.Easy)
            {
                return candidates[random.Next(candidates.Count)];
            }

            // 安全牌（現物）が候補にあれば、牌効率よりも守備を優先する
            var safeCandidates = candidates.Where(t => safeTiles.Any(s => s.IsSameType(t))).ToList();
            var pool = safeCandidates.Count > 0 ? safeCandidates : candidates;
            // 候補の牌を、打牌後のシャンテン数が少ない順、同じ場合は有効牌種類数が多い順に並べる
            var scored = pool
                .Select(t => (
                    Tile: t,
                    Shanten: CalculateShantenAfterDiscard(hand, t),
                    Ukeire: CalculateUkeireAfterDiscard(hand, t, isThreePlayer)))
                .OrderBy(x => x.Shanten)
                .ThenByDescending(x => x.Ukeire)
                .ToList();
            // 最もシャンテン数が少なく、有効牌種類数が多い牌の中からランダムに1枚選ぶ
            var bestShanten = scored[0].Shanten;
            var bestUkeire = scored[0].Ukeire;
            var topTier = scored.Where(x => x.Shanten == bestShanten && x.Ukeire == bestUkeire).ToList();

            return topTier[random.Next(topTier.Count)].Tile;
        }
        /// <summary>
        /// 打牌後もテンパイを維持できる牌をすべて列挙する
        /// リーチ宣言時、人間プレイヤーに提示する打牌候補を絞り込むために使用する
        /// </summary>
        /// <param name="hand">現在の手牌（ツモ牌を持つ状態）</param>
        /// <returns>打牌後もシャンテン数が0になる牌の一覧（1種類につき代表牌1枚）</returns>
        /// <exception cref="ArgumentNullException">hand が null の場合</exception>
        public static IReadOnlyList<Tile> FindTenpaiKeepingDiscards(Hand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            // 手牌の種類ごとに代表牌を1枚だけ残す（同種を何度も試算しないための重複排除）
            return DistinctByKind(hand.GetClosedTiles())
                .Where(t => CalculateShantenAfterDiscard(hand, t) == 0)
                .ToList();
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 牌の種類ごとに代表牌を1枚だけ残す（同種を何度も試算しないための重複排除）
        /// </summary>
        private static IEnumerable<Tile> DistinctByKind(IReadOnlyList<Tile> tiles)
        {
            // 字牌は ID で、数牌は (Suit, Number) の組み合わせでグループ化して代表牌を1枚だけ残す
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
            // 副露済みの手の場合は、標準形のみで簡易計算する
            if (hand.Melds.Count == 0)
            {
                // 副露済みの手ではない場合は、打牌後の手牌を一時的に組み立てて七対子・国士無双まで含めて正確に計算する
                var remaining = new List<Tile>(hand.GetClosedTiles());
                remaining.Remove(discard);
                // 副露済みの手ではない場合は、七対子・国士無双まで含めて正確に計算する
                var tempHand = new Hand();
                tempHand.SetInitialTiles(remaining);
                return ShantenCalculator.Calculate(tempHand);
            }
            // 副露済みの手の場合は、標準形のみで簡易計算する
            var counts = ShantenCalculator.BuildCounts(hand);
            counts[TileKind.IndexOf(discard)]--;
            return ShantenCalculator.CalculateStandardFromCounts(counts, hand.Melds.Count);
        }
        /// <summary>
        /// 指定した牌を打牌した場合の有効牌種類数を試算する
        /// </summary>
        private static int CalculateUkeireAfterDiscard(Hand hand, Tile discard, bool isThreePlayer)
        {
            // 副露済みの手ではない場合は、打牌後の手牌を一時的に組み立てて有効牌種類数を正確に計算する
            var counts = ShantenCalculator.BuildCounts(hand);
            counts[TileKind.IndexOf(discard)]--;
            return CpuUkeireCalculator.CountUkeireKinds(counts, hand.Melds.Count, isThreePlayer);
        }
    }
}
