/// <summary>
/// 副露の種類
/// </summary>
public enum MeldType
{
    Chi,        // チー（上家の捨て牌で順子）
    Pon,        // ポン（誰かの捨て牌で刻子）
    DaiMinKan,  // 大明槓（他家の捨て牌で槓子）
    KaKan,      // 加槓（ポンした刻子に手牌の同牌を追加）
    AnKan,      // 暗槓（手牌の中だけで4枚揃えて宣言）
}
