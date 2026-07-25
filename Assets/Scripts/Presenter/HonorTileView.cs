namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する字牌の種類（Mahjong.Model.Tiles.TileId の字牌部分の鏡写し）
    /// TileView.Suit が Jihai の場合にのみ意味を持つ
    /// </summary>
    public enum HonorTileView
    {
        East,   // 東（とん）
        South,  // 南（なん）
        West,   // 西（しゃー）
        North,  // 北（ぺー）
        Haku,   // 白（はく）
        Hatsu,  // 發（はつ）
        Chun,   // 中（ちゅん）
    }
}
