namespace Mahjong.Model.Game
{
    /// <summary>
    /// 局の終了理由
    /// </summary>
    public enum RoundEndReason
    {
        Tsumo,          // ツモ和了
        Ron,            // ロン和了
        ExhaustiveDraw, // 荒牌平局
        AbortiveDraw,   // 途中流局
    }
}
