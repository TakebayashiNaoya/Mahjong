namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 満貫以上の区分
    /// </summary>
    public enum LimitBand
    {
        None,        // 満貫未満
        Mangan,      // 満貫
        Haneman,     // 跳満
        Baiman,      // 倍満
        Sanbaiman,   // 三倍満
        Yakuman,     // 役満
    }
}
