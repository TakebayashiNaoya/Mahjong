using Mahjong.Model.Tiles;

namespace Mahjong.Presenter
{
    /// <summary>
    /// 人間プレイヤーに提示する打牌候補
    /// View層はこのラッパー越しにしか牌を扱わないため、Model型（Tile）を直接参照せずに済む
    /// </summary>
    public sealed class DiscardChoice
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// ボタンに表示するラベル
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// 牌の表示用データ（アイコン表示に使う）
        /// </summary>
        public TileView TileView { get; }
        /// <summary>
        /// この候補がツモ牌かどうか（手牌の表示でツモ牌だけ離して並べるために使う）
        /// </summary>
        public bool IsDrawnTile { get; }
        /// <summary>
        /// 実際に打牌する牌（Presenter層内でのみ使用する）
        /// </summary>
        internal Tile Tile { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 打牌候補を生成する
        /// </summary>
        /// <param name="tile">打牌の候補になる牌</param>
        /// <param name="isDrawnTile">その牌がツモ牌かどうか</param>
        internal DiscardChoice(Tile tile, bool isDrawnTile)
        {
            Tile = tile;
            Label = tile.ToString();
            TileView = TileView.FromModel(tile);
            IsDrawnTile = isDrawnTile;
        }
    }
}
