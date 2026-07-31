using System.Collections.Generic;

namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開するゲーム終了時の表示用データ
    /// </summary>
    public sealed class GameOverSummaryView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 最終順位（1位から順）
        /// </summary>
        public IReadOnlyList<PlayerStandingView> Standings { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// ゲーム終了時の表示用データを生成する
        /// </summary>
        internal GameOverSummaryView(IReadOnlyList<PlayerStandingView> standings)
        {
            Standings = standings;
        }
    }
}
