using System;
using System.Collections.Generic;
using System.Linq;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 荒牌平局時のテンパイ・ノーテン精算（仕様書4.9・5.2・6.2）を計算する
    /// 符・翻数・和了の知識は持たず、テンパイ状態の配列のみを受け取る純粋な計算
    /// </summary>
    public static class NotenPaymentCalculator
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 四人麻雀での精算総額
        /// </summary>
        private const int FOUR_PLAYER_POT = 3000;
        /// <summary>
        /// 三人麻雀での精算総額
        /// </summary>
        private const int THREE_PLAYER_POT = 2000;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// テンパイ・ノーテン精算の点数増減を計算する
        /// 全員テンパイ・全員ノーテンの場合は増減なし
        /// </summary>
        /// <param name="isTenpai">プレイヤーごとのテンパイ状態（席順）</param>
        /// <param name="playerCount">参加人数（三人麻雀は3、四人麻雀は4）</param>
        /// <returns>プレイヤーごとの点数増減（席順）</returns>
        /// <exception cref="ArgumentNullException">isTenpai が null の場合</exception>
        /// <exception cref="ArgumentException">isTenpai の要素数が playerCount と一致しない場合</exception>
        public static IReadOnlyList<int> Calculate(IReadOnlyList<bool> isTenpai, int playerCount)
        {
            if (isTenpai == null)
            {
                throw new ArgumentNullException(nameof(isTenpai), "isTenpai が null です");
            }

            if (isTenpai.Count != playerCount)
            {
                throw new ArgumentException(
                    $"isTenpai の要素数は playerCount と一致する必要があります: {isTenpai.Count} / {playerCount}", nameof(isTenpai));
            }

            var deltas = new int[playerCount];
            var tenpaiCount = isTenpai.Count(t => t);
            var notenCount = playerCount - tenpaiCount;

            // 全員テンパイ・全員ノーテンは動きなし
            if (tenpaiCount == 0 || notenCount == 0)
            {
                return deltas;
            }

            var pot = playerCount == 3 ? THREE_PLAYER_POT : FOUR_PLAYER_POT;
            var gainPerTenpai = pot / tenpaiCount;
            var lossPerNoten = pot / notenCount;

            for (var i = 0; i < playerCount; i++)
            {
                deltas[i] = isTenpai[i] ? gainPerTenpai : -lossPerNoten;
            }

            return deltas;
        }
    }
}
