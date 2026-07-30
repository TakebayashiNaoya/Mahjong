using System.Collections.Generic;

namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する副露1組の表示用データ
    /// 副露は麻雀では公開情報のため、牌の中身をそのまま渡す
    /// 鳴いた相手は「どの牌を横向きに置くか」というレイアウト上の情報に変換して渡す
    /// （View層に風・席順の知識を持ち込まないため）
    /// </summary>
    public sealed class MeldView
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 横向きに置く牌が無いことを表す RotatedTileIndex の値（暗槓）
        /// </summary>
        public const int NO_ROTATED_TILE_INDEX = -1;


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 副露を構成する牌（左から並べる順）
        /// </summary>
        public IReadOnlyList<TileView> Tiles { get; }
        /// <summary>
        /// 横向きに置く牌のインデックス
        /// 上家から鳴いた場合は左端、対面なら左から2枚目、下家なら右端になる
        /// 暗槓の場合は NO_ROTATED_TILE_INDEX
        /// </summary>
        public int RotatedTileIndex { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 副露の表示用データを生成する
        /// </summary>
        /// <param name="tiles">副露を構成する牌（左から並べる順）</param>
        /// <param name="rotatedTileIndex">横向きに置く牌のインデックス（暗槓は NO_ROTATED_TILE_INDEX）</param>
        internal MeldView(IReadOnlyList<TileView> tiles, int rotatedTileIndex)
        {
            Tiles = tiles;
            RotatedTileIndex = rotatedTileIndex;
        }
    }
}
