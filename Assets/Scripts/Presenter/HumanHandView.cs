using System.Collections.Generic;

namespace Mahjong.Presenter
{
    /// <summary>
    /// 人間プレイヤーの手牌を、View層が1回で受け取れるようひとまとめにしたスナップショット
    /// 牌の並びとツモ牌の位置を別々のReactivePropertyに分けると、片方だけ更新された瞬間に
    /// View層が食い違った組み合わせで描画してしまうため、常に一体で通知する
    /// </summary>
    public sealed class HumanHandView
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// ツモ牌を持っていないことを表す DrawnTileIndex の値
        /// </summary>
        public const int NO_DRAWN_TILE_INDEX = -1;
        /// <summary>
        /// 空の手牌（対局開始前の初期値）
        /// </summary>
        public static readonly HumanHandView Empty = new(System.Array.Empty<TileView>(), NO_DRAWN_TILE_INDEX);


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 手牌の牌（門前牌 + ツモ牌、この順）
        /// </summary>
        public IReadOnlyList<TileView> Tiles { get; }
        /// <summary>
        /// Tiles の中でツモ牌が何番目かを表すインデックス
        /// ツモ牌を持っていない場合は NO_DRAWN_TILE_INDEX
        /// </summary>
        public int DrawnTileIndex { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 手牌のスナップショットを生成する
        /// </summary>
        /// <param name="tiles">手牌の牌（門前牌 + ツモ牌、この順）</param>
        /// <param name="drawnTileIndex">ツモ牌のインデックス（持っていない場合は NO_DRAWN_TILE_INDEX）</param>
        internal HumanHandView(IReadOnlyList<TileView> tiles, int drawnTileIndex)
        {
            Tiles = tiles;
            DrawnTileIndex = drawnTileIndex;
        }
    }
}
