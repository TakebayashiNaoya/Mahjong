using System;
using System.Collections.Generic;
using Mahjong.Model.Evaluation.Internal;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// テンパイ時の待ち牌を列挙する
    /// HandState.UpdateFuriten が要求する waitingTiles を供給するために使用する
    /// </summary>
    public static class WaitingTileFinder
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 待ち牌判定が可能な門前牌の枚数（ツモ牌を含まない13枚）
        /// </summary>
        private const int TENPAI_CLOSED_TILE_COUNT = 13;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 手牌（13枚・ツモ牌なし）の待ち牌をすべて列挙する
        /// 34種すべてを仮の和了牌として AgariChecker に問い合わせる総当たりで求める
        /// </summary>
        /// <param name="hand">判定対象の手牌（GetClosedTiles() が13枚である必要がある）</param>
        /// <returns>和了になる牌の一覧（種類ごとに代表牌を1枚ずつ）</returns>
        /// <exception cref="ArgumentNullException">hand が null の場合</exception>
        /// <exception cref="InvalidOperationException">手牌の門前牌が13枚（ツモ牌なし）でない場合</exception>
        public static IReadOnlyList<Mahjong.Model.Tiles.Tile> FindWaits(Mahjong.Model.Hands.Hand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            var closedCount = hand.GetClosedTiles().Count;

            if (closedCount != TENPAI_CLOSED_TILE_COUNT)
            {
                throw new InvalidOperationException(
                    $"待ち牌の判定は門前牌が{TENPAI_CLOSED_TILE_COUNT}枚（ツモ牌なし）の場合のみ可能です: {closedCount}枚");
            }

            var waits = new List<Mahjong.Model.Tiles.Tile>();

            for (var kindIndex = 0; kindIndex < TileKind.KIND_COUNT; kindIndex++)
            {
                var candidate = TileKind.CreateRepresentativeTile(kindIndex);
                var result = AgariChecker.CheckWin(hand, candidate, isTsumo: false);

                if (result.IsWin)
                {
                    waits.Add(candidate);
                }
            }

            return waits;
        }
    }
}
