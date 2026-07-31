using System.Collections.Generic;

namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する局の結果（和了・流局）の表示用データ
    /// GamePresenter が Round の結果から組み立てる（Model型は一切含まず、文字列に整形済み）
    /// </summary>
    public sealed class RoundResultView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 局を表す見出し（例: "東1局 0本場"）
        /// </summary>
        public string RoundLabel { get; }
        /// <summary>
        /// 終了理由の表示文字列（例: "ツモ和了", "途中流局: 九種九牌"）
        /// </summary>
        public string ReasonLabel { get; }
        /// <summary>
        /// 和了の内訳（Tsumo は1件、Ron はダブロン対応で複数件になり得る。それ以外は空）
        /// </summary>
        public IReadOnlyList<WinResultView> Wins { get; }
        /// <summary>
        /// プレイヤーごとの点数増減（席順）
        /// </summary>
        public IReadOnlyList<int> ScoreDeltas { get; }
        /// <summary>
        /// 荒牌平局時、各プレイヤーがテンパイだったかどうか（席順）。それ以外の終了理由では空
        /// </summary>
        public IReadOnlyList<bool> TenpaiStates { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 局の結果の表示用データを生成する
        /// </summary>
        internal RoundResultView(
            string roundLabel,
            string reasonLabel,
            IReadOnlyList<WinResultView> wins,
            IReadOnlyList<int> scoreDeltas,
            IReadOnlyList<bool> tenpaiStates)
        {
            RoundLabel = roundLabel;
            ReasonLabel = reasonLabel;
            Wins = wins;
            ScoreDeltas = scoreDeltas;
            TenpaiStates = tenpaiStates;
        }
    }
}
