using System;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation.Internal
{
    /// <summary>
    /// 牌を0〜33の種類インデックスに変換するヘルパー
    /// 赤ドラは通常の5と同じインデックスになる（Tile.Suit / Number のみを見るため）
    /// シャンテン数計算・和了判定などのアルゴリズムはすべてこのインデックスを基盤にする
    /// </summary>
    internal static class TileKind
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 牌の種類数（萬子9 + 筒子9 + 索子9 + 字牌7）
        /// </summary>
        public const int KIND_COUNT = 34;
        /// <summary>
        /// 萬子の先頭インデックス
        /// </summary>
        public const int MANZU_OFFSET = 0;
        /// <summary>
        /// 筒子の先頭インデックス
        /// </summary>
        public const int PINZU_OFFSET = 9;
        /// <summary>
        /// 索子の先頭インデックス
        /// </summary>
        public const int SOUZU_OFFSET = 18;
        /// <summary>
        /// 字牌の先頭インデックス
        /// </summary>
        public const int JIHAI_OFFSET = 27;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 牌を0〜33の種類インデックスに変換する
        /// </summary>
        /// <param name="tile">変換する牌</param>
        /// <returns>0〜33の種類インデックス</returns>
        /// <exception cref="ArgumentNullException">tile が null の場合</exception>
        public static int IndexOf(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile), "tile が null です");
            }

            return tile.Suit switch
            {
                TileSuit.Manzu => MANZU_OFFSET + (tile.Number - 1),
                TileSuit.Pinzu => PINZU_OFFSET + (tile.Number - 1),
                TileSuit.Souzu => SOUZU_OFFSET + (tile.Number - 1),
                TileSuit.Jihai => JIHAI_OFFSET + JihaiOrder(tile.Id),
                _ => throw new ArgumentException($"未対応の TileSuit です: {tile.Suit}", nameof(tile)),
            };
        }
        /// <summary>
        /// 種類インデックスが么九牌（1・9・字牌）かどうかを判定する
        /// </summary>
        /// <param name="kindIndex">0〜33の種類インデックス</param>
        /// <returns>么九牌なら true</returns>
        public static bool IsYaochu(int kindIndex)
        {
            if (kindIndex >= JIHAI_OFFSET)
            {
                return true;
            }

            var numberInSuit = kindIndex % 9;
            return numberInSuit == 0 || numberInSuit == 8;
        }
        /// <summary>
        /// 種類インデックスから、その種類を代表する牌（赤ドラではない通常牌）を生成する
        /// 待ち牌列挙など、実際の牌インスタンスが必要な場面で使用する
        /// </summary>
        /// <param name="kindIndex">0〜33の種類インデックス</param>
        /// <returns>その種類を表す通常牌</returns>
        public static Tile CreateRepresentativeTile(int kindIndex)
        {
            if (kindIndex < JIHAI_OFFSET)
            {
                var suit = kindIndex switch
                {
                    < PINZU_OFFSET => TileSuit.Manzu,
                    < SOUZU_OFFSET => TileSuit.Pinzu,
                    _ => TileSuit.Souzu,
                };

                var suitOffset = suit switch
                {
                    TileSuit.Manzu => MANZU_OFFSET,
                    TileSuit.Pinzu => PINZU_OFFSET,
                    _ => SOUZU_OFFSET,
                };

                var number = kindIndex - suitOffset + 1;
                return new Tile(SuitedTileId(suit, number), suit, number);
            }

            var jihaiId = JihaiIdFromOrder(kindIndex - JIHAI_OFFSET);
            return new Tile(jihaiId, TileSuit.Jihai, 0);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 字牌の並び順（東南西北白發中）を0〜6のインデックスに変換する
        /// </summary>
        /// <param name="id">字牌のTileId</param>
        /// <returns>0〜6の並び順インデックス</returns>
        private static int JihaiOrder(TileId id)
        {
            return id switch
            {
                TileId.East => 0,
                TileId.South => 1,
                TileId.West => 2,
                TileId.North => 3,
                TileId.Haku => 4,
                TileId.Hatsu => 5,
                TileId.Chun => 6,
                _ => throw new ArgumentException($"字牌ではない TileId です: {id}", nameof(id)),
            };
        }
        /// <summary>
        /// 字牌の並び順（0〜6）を TileId に変換する（JihaiOrder の逆変換）
        /// </summary>
        /// <param name="order">0〜6の並び順インデックス</param>
        /// <returns>対応する字牌の TileId</returns>
        private static TileId JihaiIdFromOrder(int order)
        {
            return order switch
            {
                0 => TileId.East,
                1 => TileId.South,
                2 => TileId.West,
                3 => TileId.North,
                4 => TileId.Haku,
                5 => TileId.Hatsu,
                6 => TileId.Chun,
                _ => throw new ArgumentException($"字牌の並び順として不正な値です: {order}", nameof(order)),
            };
        }
        /// <summary>
        /// スート内の数字（1〜9）を、赤ドラではない通常牌の TileId に変換する
        /// </summary>
        /// <param name="suit">牌の種類</param>
        /// <param name="number">1〜9の数字</param>
        /// <returns>対応する通常牌の TileId</returns>
        private static TileId SuitedTileId(TileSuit suit, int number)
        {
            return suit switch
            {
                TileSuit.Manzu => number switch
                {
                    1 => TileId.Manzu1,
                    2 => TileId.Manzu2,
                    3 => TileId.Manzu3,
                    4 => TileId.Manzu4,
                    5 => TileId.Manzu5,
                    6 => TileId.Manzu6,
                    7 => TileId.Manzu7,
                    8 => TileId.Manzu8,
                    9 => TileId.Manzu9,
                    _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
                },
                TileSuit.Pinzu => number switch
                {
                    1 => TileId.Pinzu1,
                    2 => TileId.Pinzu2,
                    3 => TileId.Pinzu3,
                    4 => TileId.Pinzu4,
                    5 => TileId.Pinzu5,
                    6 => TileId.Pinzu6,
                    7 => TileId.Pinzu7,
                    8 => TileId.Pinzu8,
                    9 => TileId.Pinzu9,
                    _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
                },
                TileSuit.Souzu => number switch
                {
                    1 => TileId.Souzu1,
                    2 => TileId.Souzu2,
                    3 => TileId.Souzu3,
                    4 => TileId.Souzu4,
                    5 => TileId.Souzu5,
                    6 => TileId.Souzu6,
                    7 => TileId.Souzu7,
                    8 => TileId.Souzu8,
                    9 => TileId.Souzu9,
                    _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
                },
                _ => throw new ArgumentException($"数牌ではない TileSuit です: {suit}", nameof(suit)),
            };
        }
    }
}
