using System;
using Mahjong.Model.Common;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation.Internal
{
    /// <summary>
    /// 牌が三元牌・風牌・役牌かどうかを判定する共有ヘルパー
    /// YakuEvaluator（役牌の役判定）と FuCalculator（雀頭の符判定）の両方から使うため、
    /// 判定ロジックの重複によるズレを防ぐ目的でここに集約する
    /// </summary>
    internal static class TileClassification
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 三元牌（白發中）かどうかを判定する
        /// </summary>
        public static bool IsDragon(Tile tile)
        {
            return tile.IsJihai && tile.Id is TileId.Haku or TileId.Hatsu or TileId.Chun;
        }
        /// <summary>
        /// 風牌（東南西北）かどうかを判定する
        /// </summary>
        public static bool IsWind(Tile tile)
        {
            return tile.IsJihai && tile.Id is TileId.East or TileId.South or TileId.West or TileId.North;
        }
        /// <summary>
        /// 役牌（三元牌・自風・場風）かどうかを判定する
        /// 連風牌（自風=場風）でも特別扱いはしない
        /// </summary>
        public static bool IsYakuhaiTile(Tile tile, WinContext context)
        {
            if (!tile.IsJihai)
            {
                return false;
            }

            if (IsDragon(tile))
            {
                return true;
            }

            return tile.Id == WindToTileId(context.SeatWind) || tile.Id == WindToTileId(context.RoundWind);
        }
        /// <summary>
        /// Wind を対応する TileId に変換する
        /// </summary>
        public static TileId WindToTileId(Wind wind)
        {
            return wind switch
            {
                Wind.East => TileId.East,
                Wind.South => TileId.South,
                Wind.West => TileId.West,
                Wind.North => TileId.North,
                _ => throw new ArgumentException($"未対応の Wind です: {wind}", nameof(wind)),
            };
        }
    }
}
