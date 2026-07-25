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
        /// 実際に打牌する牌（Presenter層内でのみ使用する）
        /// </summary>
        internal Tile Tile { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        internal DiscardChoice(Tile tile)
        {
            Tile = tile;
            Label = tile.ToString();
        }
    }
}
