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
    }
}
