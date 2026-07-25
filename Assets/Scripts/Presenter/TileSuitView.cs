namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する牌の種類（Mahjong.Model.Tiles.TileSuit の鏡写し）
    /// View層がModel型を直接参照せずに済むよう、Presenter層で独自に定義する
    /// </summary>
    public enum TileSuitView
    {
        Manzu,  // 萬子（1〜9）
        Pinzu,  // 筒子（1〜9）
        Souzu,  // 索子（1〜9）
        Jihai,  // 字牌（東南西北白發中）
    }
}
