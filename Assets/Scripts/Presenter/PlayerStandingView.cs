namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開するゲーム終了時の順位1件分の表示用データ
    /// 返し点を用いた正式な順位計算は未実装のため、持ち点降順の簡易順位を表す
    /// </summary>
    public sealed class PlayerStandingView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 順位（1始まり）
        /// </summary>
        public int Rank { get; }
        /// <summary>
        /// 固定席順（0始まり）
        /// </summary>
        public int PlayerIndex { get; }
        /// <summary>
        /// 人間プレイヤーかどうか
        /// </summary>
        public bool IsHuman { get; }
        /// <summary>
        /// 最終持ち点
        /// </summary>
        public int Score { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 順位1件分の表示用データを生成する
        /// </summary>
        internal PlayerStandingView(int rank, int playerIndex, bool isHuman, int score)
        {
            Rank = rank;
            PlayerIndex = playerIndex;
            IsHuman = isHuman;
            Score = score;
        }
    }
}
