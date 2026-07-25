using System;
using Mahjong.Model.Scoring;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 和了1件分の内訳
    /// ダブロン（複数人が同じ捨て牌にロン）に対応するため、RoundResult は複数件保持できる
    /// </summary>
    public class WinOutcome
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 和了したプレイヤーの席順
        /// </summary>
        public int WinnerIndex { get; }
        /// <summary>
        /// ロンの場合の放銃者の席順。ツモの場合は null
        /// </summary>
        public int? DiscarderIndex { get; }
        /// <summary>
        /// 点数計算の結果
        /// </summary>
        public ScoreResult Score { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 和了の内訳を生成する
        /// </summary>
        /// <exception cref="ArgumentNullException">score が null の場合</exception>
        public WinOutcome(int winnerIndex, int? discarderIndex, ScoreResult score)
        {
            if (score == null)
            {
                throw new ArgumentNullException(nameof(score), "score が null です");
            }

            WinnerIndex = winnerIndex;
            DiscarderIndex = discarderIndex;
            Score = score;
        }
    }
}
