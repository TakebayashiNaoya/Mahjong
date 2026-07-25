using Mahjong.Model.Tiles;

namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する牌1枚の表示用データ
    /// View層はこのDTO越しにしか牌を扱わないため、Model型（Tile）を直接参照せずに済む
    /// </summary>
    public sealed class TileView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 牌の種類
        /// </summary>
        public TileSuitView Suit { get; }
        /// <summary>
        /// 牌の数字（1〜9）
        /// 字牌の場合は 0
        /// </summary>
        public int Number { get; }
        /// <summary>
        /// 字牌の種類
        /// Suit が Jihai 以外の場合は null
        /// </summary>
        public HonorTileView? Honor { get; }
        /// <summary>
        /// 赤ドラかどうか
        /// </summary>
        public bool IsRed { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 牌の表示用データを生成する
        /// </summary>
        /// <param name="suit">牌の種類</param>
        /// <param name="number">牌の数字（字牌は0）</param>
        /// <param name="honor">字牌の種類（数牌はnull）</param>
        /// <param name="isRed">赤ドラかどうか</param>
        internal TileView(TileSuitView suit, int number, HonorTileView? honor, bool isRed)
        {
            Suit = suit;
            Number = number;
            Honor = honor;
            IsRed = isRed;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// Model層の牌をView層に安全なTileViewへ変換する
        /// </summary>
        /// <exception cref="System.InvalidOperationException">未対応のSuit/Idの場合</exception>
        internal static TileView FromModel(Tile tile)
        {
            if (tile.Suit != TileSuit.Jihai)
            {
                var suit = tile.Suit switch
                {
                    TileSuit.Manzu => TileSuitView.Manzu,
                    TileSuit.Pinzu => TileSuitView.Pinzu,
                    TileSuit.Souzu => TileSuitView.Souzu,
                    _ => throw new System.InvalidOperationException($"未対応のTileSuitです: {tile.Suit}"),
                };

                return new TileView(suit, tile.Number, null, tile.IsRed);
            }

            var honor = tile.Id switch
            {
                TileId.East => HonorTileView.East,
                TileId.South => HonorTileView.South,
                TileId.West => HonorTileView.West,
                TileId.North => HonorTileView.North,
                TileId.Haku => HonorTileView.Haku,
                TileId.Hatsu => HonorTileView.Hatsu,
                TileId.Chun => HonorTileView.Chun,
                _ => throw new System.InvalidOperationException($"未対応の字牌IDです: {tile.Id}"),
            };

            return new TileView(TileSuitView.Jihai, 0, honor, false);
        }
    }
}
