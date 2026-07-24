using System;
using System.Collections.Generic;
using Mahjong.Model.Common;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation.Tests
{
    /// <summary>
    /// テストで牌を組み立てるためのヘルパー
    /// </summary>
    internal static class TestTiles
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 13枚の牌から手牌を組み立てる
        /// </summary>
        /// <param name="thirteenTiles">配牌（13枚）</param>
        /// <param name="drawnTile">ツモ牌（ツモ和了のテスト時のみ指定）</param>
        public static Mahjong.Model.Hands.Hand CreateHand(List<Tile> thirteenTiles, Tile drawnTile = null)
        {
            var hand = new Mahjong.Model.Hands.Hand();
            hand.SetInitialTiles(thirteenTiles);

            if (drawnTile != null)
            {
                hand.Draw(drawnTile);
            }

            return hand;
        }
        /// <summary>
        /// テスト用の WinContext を組み立てる（未指定の項目は false / 東 になる）
        /// </summary>
        public static WinContext Context(
            bool isTsumo = false,
            bool isRiichi = false,
            bool isIppatsu = false,
            Wind seatWind = Wind.East,
            Wind roundWind = Wind.East,
            bool isDealer = false)
        {
            return new WinContext(
                isTsumo, isRiichi, isIppatsu,
                isHaitei: false, isHoutei: false, isRinshan: false, isChankan: false,
                isTenhou: false, isChiihou: false,
                seatWind, roundWind, isDealer);
        }
        /// <summary>
        /// 萬子を生成する
        /// </summary>
        public static Tile M(int number, bool isRed = false)
        {
            return Suited(TileSuit.Manzu, number, isRed);
        }
        /// <summary>
        /// 筒子を生成する
        /// </summary>
        public static Tile P(int number, bool isRed = false)
        {
            return Suited(TileSuit.Pinzu, number, isRed);
        }
        /// <summary>
        /// 索子を生成する
        /// </summary>
        public static Tile S(int number, bool isRed = false)
        {
            return Suited(TileSuit.Souzu, number, isRed);
        }
        /// <summary>
        /// 字牌を生成する
        /// </summary>
        public static Tile Z(TileId id)
        {
            return new Tile(id, TileSuit.Jihai, 0);
        }
        /// <summary>
        /// ポンの副露を生成する（手牌側の2枚 + 鳴いた1枚）
        /// </summary>
        public static Meld Pon(Tile own1, Tile own2, Tile stolen, Wind fromWind)
        {
            return new Meld(MeldType.Pon, new List<Tile> { own1, own2, stolen }, stolen, fromWind);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        private static Tile Suited(TileSuit suit, int number, bool isRed)
        {
            var id = suit switch
            {
                TileSuit.Manzu => ManzuId(number, isRed),
                TileSuit.Pinzu => PinzuId(number, isRed),
                TileSuit.Souzu => SouzuId(number, isRed),
                _ => throw new ArgumentException($"数牌ではない TileSuit です: {suit}", nameof(suit)),
            };

            return new Tile(id, suit, number, isRed);
        }

        private static TileId ManzuId(int number, bool isRed)
        {
            if (isRed && number == 5)
            {
                return TileId.Manzu5Red;
            }

            return number switch
            {
                1 => TileId.Manzu1, 2 => TileId.Manzu2, 3 => TileId.Manzu3,
                4 => TileId.Manzu4, 5 => TileId.Manzu5, 6 => TileId.Manzu6,
                7 => TileId.Manzu7, 8 => TileId.Manzu8, 9 => TileId.Manzu9,
                _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
            };
        }

        private static TileId PinzuId(int number, bool isRed)
        {
            if (isRed && number == 5)
            {
                return TileId.Pinzu5Red;
            }

            return number switch
            {
                1 => TileId.Pinzu1, 2 => TileId.Pinzu2, 3 => TileId.Pinzu3,
                4 => TileId.Pinzu4, 5 => TileId.Pinzu5, 6 => TileId.Pinzu6,
                7 => TileId.Pinzu7, 8 => TileId.Pinzu8, 9 => TileId.Pinzu9,
                _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
            };
        }

        private static TileId SouzuId(int number, bool isRed)
        {
            if (isRed && number == 5)
            {
                return TileId.Souzu5Red;
            }

            return number switch
            {
                1 => TileId.Souzu1, 2 => TileId.Souzu2, 3 => TileId.Souzu3,
                4 => TileId.Souzu4, 5 => TileId.Souzu5, 6 => TileId.Souzu6,
                7 => TileId.Souzu7, 8 => TileId.Souzu8, 9 => TileId.Souzu9,
                _ => throw new ArgumentException($"数字として不正な値です: {number}", nameof(number)),
            };
        }
    }
}
