namespace Mahjong.Model.Game
{
    /// <summary>
    /// 途中流局の理由（仕様書4.8）
    /// </summary>
    public enum AbortiveDrawReason
    {
        KyuushuKyuuhai, // 九種九牌
        SuufuRenda,     // 四風連打
        SuuKaiSanra,    // 四槓散了
        Sanchaho,       // 三家和
    }
}
