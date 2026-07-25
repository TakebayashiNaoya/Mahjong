namespace Mahjong.Model.Game
{
    /// <summary>
    /// 他家の捨て牌に対する宣言の種類
    /// 暗槓・加槓・北抜きは自分のツモ番に行う別APIで扱うため、ここには含めない
    /// </summary>
    public enum CallType
    {
        Chi,    // チー
        Pon,    // ポン
        Kan,    // 大明槓
        Ron,    // ロン
    }
}
