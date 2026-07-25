using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Game;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Cpu
{
    /// <summary>
    /// Round と連携してCPUの意思決定を行う窓口
    /// 実際の判断ロジックは CpuDiscardSelector・CpuCallSelector（Hand単位の純粋関数）に委譲し、
    /// ここでは Round・PlayerState から必要な文脈（安全牌・リーチ宣言直後かどうか等）を組み立てる
    /// </summary>
    public static class CpuStrategy
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 打牌する牌を選ぶ
        /// リーチ中（今回の宣言ではない）の場合はツモ切りを強制する
        /// </summary>
        /// <exception cref="ArgumentNullException">round・random が null の場合</exception>
        public static Tile ChooseDiscard(Round round, int playerIndex, CpuDifficulty difficulty, Random random)
        {
            if (round == null)
            {
                throw new ArgumentNullException(nameof(round), "round が null です");
            }

            var player = round.Players[playerIndex];

            // リーチ済みかつ今回の打牌がその宣言直後ではない場合、ツモ切りが強制される
            var isOngoingRiichi = player.HandState.IsRiichi && player.HandState.RiichiTurnIndex != round.TurnIndex;

            if (isOngoingRiichi)
            {
                return player.Hand.DrawnTile;
            }

            var isThreePlayer = round.Settings.PlayerCount == 3;
            var mustKeepTenpai = player.HandState.IsRiichi;
            var safeTiles = FindSafeTiles(round, playerIndex);

            return CpuDiscardSelector.ChooseDiscard(player.Hand, isThreePlayer, mustKeepTenpai, safeTiles, difficulty, random);
        }
        /// <summary>
        /// リーチを宣言するかどうかを判断する
        /// Normal はテンパイ時に基本的にリーチする。Easy はリーチしない
        /// </summary>
        public static bool ShouldDeclareRiichi(Round round, int playerIndex, CpuDifficulty difficulty)
        {
            return difficulty != CpuDifficulty.Easy;
        }
        /// <summary>
        /// 捨て牌に対する宣言を選ぶ
        /// </summary>
        /// <param name="options">GetAvailableCalls の結果（他プレイヤー分を含んでいてもよい。このプレイヤー分のみに絞り込む）</param>
        /// <exception cref="ArgumentNullException">round・options が null の場合</exception>
        public static DeclaredCall ChooseCall(Round round, int playerIndex, IReadOnlyList<CallOption> options, CpuDifficulty difficulty)
        {
            if (round == null)
            {
                throw new ArgumentNullException(nameof(round), "round が null です");
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "options が null です");
            }

            var myOptions = options.Where(o => o.PlayerIndex == playerIndex).ToList();
            return CpuCallSelector.ChooseCall(round.Players[playerIndex].Hand, myOptions, difficulty);
        }
        /// <summary>
        /// 九種九牌を宣言するかどうかを判断する（宣言可能なら常に宣言する）
        /// </summary>
        public static bool ShouldDeclareKyuushuKyuuhai(Round round, int playerIndex, CpuDifficulty difficulty)
        {
            return true;
        }
        /// <summary>
        /// 北抜きを宣言するかどうかを判断する（宣言可能なら常に宣言する）
        /// </summary>
        public static bool ShouldDeclareKitaNuki(Round round, int playerIndex, CpuDifficulty difficulty)
        {
            return true;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// リーチ中の他家全員の河に共通して存在する牌（現物）を安全牌として集める
        /// 筋・壁読みは行わない簡略仕様
        /// </summary>
        private static IReadOnlyCollection<Tile> FindSafeTiles(Round round, int playerIndex)
        {
            var riichiOpponents = round.Players
                .Where(p => p.PlayerIndex != playerIndex && p.HandState.IsRiichi)
                .ToList();

            if (riichiOpponents.Count == 0)
            {
                return Array.Empty<Tile>();
            }

            IEnumerable<Tile> safe = riichiOpponents[0].Discards;

            foreach (var opponent in riichiOpponents.Skip(1))
            {
                safe = safe.Where(t => opponent.Discards.Any(d => d.IsSameType(t)));
            }

            return safe.ToList();
        }
    }
}
