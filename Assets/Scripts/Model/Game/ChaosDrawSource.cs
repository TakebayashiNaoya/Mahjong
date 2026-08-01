namespace Mahjong.Model.Game
{
    /// <summary>
    /// カオス麻雀ルールで、自分のツモ番に牌を取れる場所（仕様書16.3）
    /// 1つのツモ番ではこのうち1つだけを選べる
    /// </summary>
    public enum ChaosDrawSource
    {
        Wall,           // 山（通常のツモ）
        DiscardPile,    // 誰かの河
        OpponentHand,   // 他家の手牌（伏せたまま位置で指定する）
        OpponentMeld,   // 他家の副露
        OwnMeld,        // 自分の副露を手牌に戻す
    }
}
