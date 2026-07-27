namespace Mahjong.Presenter
{
    /// <summary>
    /// 他家（自分以外）の手牌のうち、公開情報だけをView層に渡すためのデータ
    /// 中身の牌は伏せたままにするため、伏せ牌として並べるのに必要な枚数と、
    /// ツモ牌を持っているか（＝1枚離して置くか）だけを持つ
    /// 枚数とツモ牌の有無を分けて持つ理由: 合計枚数だけだとツモのたびに列全体の長さが変わり、
    /// 表示側が門前牌の位置を保てなくなるため
    /// </summary>
    public sealed class ConcealedHandView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 門前牌の枚数（ツモ牌・副露牌は含まない）
        /// </summary>
        public int ConcealedTileCount { get; }
        /// <summary>
        /// ツモ牌を持っているかどうか
        /// </summary>
        public bool HasDrawnTile { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 伏せ手牌の表示用データを生成する
        /// </summary>
        /// <param name="concealedTileCount">門前牌の枚数（ツモ牌・副露牌を含まない）</param>
        /// <param name="hasDrawnTile">ツモ牌を持っているかどうか</param>
        internal ConcealedHandView(int concealedTileCount, bool hasDrawnTile)
        {
            ConcealedTileCount = concealedTileCount;
            HasDrawnTile = hasDrawnTile;
        }
    }
}
