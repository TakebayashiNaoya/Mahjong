namespace Mahjong.Model.Game
{
    /// <summary>
    /// Round 内部のターン進行フェーズ
    /// </summary>
    public enum TurnPhase
    {
        AwaitingDraw,       // 現在のプレイヤーのツモ待ち
        AwaitingDiscard,    // 現在のプレイヤーの打牌待ち
        AwaitingReactions,  // 他家の反応（ロン・ポン・カン・チー）待ち
        Ended,              // 局が終了した
    }
}
