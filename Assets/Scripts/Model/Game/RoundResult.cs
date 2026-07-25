using System;
using System.Collections.Generic;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 局の結果
    /// </summary>
    public class RoundResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 局の終了理由
        /// </summary>
        public RoundEndReason Reason { get; }
        /// <summary>
        /// 和了の内訳（Tsumo は1件、Ron はダブロン対応で複数件になり得る。それ以外は空）
        /// </summary>
        public IReadOnlyList<WinOutcome> Wins { get; }
        /// <summary>
        /// プレイヤーごとの点数増減（席順）
        /// 供託・本場・ノーテン精算まですべて反映済み
        /// </summary>
        public IReadOnlyList<int> ScoreDeltas { get; }
        /// <summary>
        /// 親が連荘するかどうか
        /// </summary>
        public bool DealerContinues { get; }
        /// <summary>
        /// 荒牌平局時、各プレイヤーがテンパイだったかどうか（席順）。それ以外の終了理由では空
        /// </summary>
        public IReadOnlyList<bool> TenpaiStates { get; }
        /// <summary>
        /// 途中流局の理由（Reason が AbortiveDraw の場合のみ値を持つ）
        /// </summary>
        public AbortiveDrawReason? AbortiveReason { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 局の結果を生成する
        /// </summary>
        /// <exception cref="ArgumentNullException">scoreDeltas が null の場合</exception>
        public RoundResult(
            RoundEndReason reason,
            IReadOnlyList<WinOutcome> wins,
            IReadOnlyList<int> scoreDeltas,
            bool dealerContinues,
            IReadOnlyList<bool> tenpaiStates = null,
            AbortiveDrawReason? abortiveReason = null)
        {
            if (scoreDeltas == null)
            {
                throw new ArgumentNullException(nameof(scoreDeltas), "scoreDeltas が null です");
            }

            Reason = reason;
            Wins = wins ?? Array.Empty<WinOutcome>();
            ScoreDeltas = scoreDeltas;
            DealerContinues = dealerContinues;
            TenpaiStates = tenpaiStates ?? Array.Empty<bool>();
            AbortiveReason = abortiveReason;
        }
    }
}
