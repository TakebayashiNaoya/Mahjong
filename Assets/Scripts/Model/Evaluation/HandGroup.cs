using System.Collections.Generic;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 和了形を構成する1グループ（面子または雀頭）
    /// </summary>
    public sealed class HandGroup
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// グループ種別
        /// </summary>
        public GroupType Type { get; }
        /// <summary>
        /// グループを構成する牌
        /// </summary>
        public IReadOnlyList<Tile> Tiles { get; }
        /// <summary>
        /// 副露せずに完成したグループかどうか
        /// ロンで完成した刻子は false になる（大明槓・ポン等の既存副露は Meld.IsOpen から判定）
        /// </summary>
        public bool IsConcealed { get; }
        /// <summary>
        /// 和了牌を含むグループかどうか
        /// </summary>
        public bool ContainsWinningTile { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public HandGroup(GroupType type, IReadOnlyList<Tile> tiles, bool isConcealed, bool containsWinningTile)
        {
            Type = type;
            Tiles = tiles;
            IsConcealed = isConcealed;
            ContainsWinningTile = containsWinningTile;
        }
    }
}
