using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using NUnit.Framework;

namespace Mahjong.Model.Cpu.Tests
{
    /// <summary>
    /// CpuStrategy と Round を実際に組み合わせ、1局を最後まで進行できることを確認する統合テスト
    /// </summary>
    [TestFixture]
    public class CpuStrategyTests
    {
        private const int MAX_ITERATIONS = 1000;

        [Test]
        public void PlayRoundToCompletion_FourPlayerNormalDifficulty_EndsWithoutException()
        {
            var result = PlayRoundToCompletion(playerCount: 4, seed: 12345);

            Assert.IsNotNull(result);
        }

        [Test]
        public void PlayRoundToCompletion_ThreePlayerNormalDifficulty_EndsWithoutException()
        {
            var result = PlayRoundToCompletion(playerCount: 3, seed: 54321);

            Assert.IsNotNull(result);
        }


        // ========================================
        // テスト用ヘルパー
        // ========================================
        /// <summary>
        /// 全プレイヤーを CpuStrategy（Normal難易度）で動かし、局が終了するまで進行させる
        /// 実際のPresenter層が担う「手番を回すループ」を、テスト目的で簡易に再現したもの
        /// </summary>
        private static RoundResult PlayRoundToCompletion(int playerCount, int seed)
        {
            const CpuDifficulty difficulty = CpuDifficulty.Normal;

            var settings = GameSettings.CreateDefault(playerCount, GameLengthType.EastOnly);
            var players = Enumerable.Range(0, playerCount)
                .Select(i => new PlayerState(i, settings.InitialScore))
                .ToList();

            var random = new Random(seed);
            var round = new Round(settings, players, Wind.East, roundNumber: 1, dealerIndex: 0, honbaCount: 0, riichiStickCount: 0, random);

            for (var iteration = 0; iteration < MAX_ITERATIONS; iteration++)
            {
                if (round.PendingAbortiveDraw != null)
                {
                    return round.FinalizeAbortiveDraw();
                }

                if (round.Phase == TurnPhase.AwaitingDraw)
                {
                    if (round.IsWallExhausted)
                    {
                        return round.DeclareExhaustiveDraw();
                    }

                    round.DrawTile();
                    continue;
                }

                if (round.Phase == TurnPhase.AwaitingDiscard)
                {
                    var playerIndex = round.CurrentPlayerIndex;

                    if (round.CanDeclareTsumoWin())
                    {
                        return round.DeclareTsumoWin();
                    }

                    if (round.CanDeclareKyuushuKyuuhai() && CpuStrategy.ShouldDeclareKyuushuKyuuhai(round, playerIndex, difficulty))
                    {
                        return round.DeclareKyuushuKyuuhai();
                    }

                    if (round.Settings.UseKitaNuki && round.CanDeclareKitaNuki() && CpuStrategy.ShouldDeclareKitaNuki(round, playerIndex, difficulty))
                    {
                        round.DeclareKitaNuki();
                        continue;
                    }

                    if (round.CanDeclareRiichi() && CpuStrategy.ShouldDeclareRiichi(round, playerIndex, difficulty))
                    {
                        round.DeclareRiichi();
                    }

                    var discard = CpuStrategy.ChooseDiscard(round, playerIndex, difficulty, random);
                    round.Discard(discard);
                    continue;
                }

                if (round.Phase == TurnPhase.AwaitingReactions)
                {
                    var discarderIndex = round.CurrentPlayerIndex;
                    var discardedTile = round.Players[discarderIndex].Discards[^1];
                    var options = round.GetAvailableCalls(discardedTile, discarderIndex);

                    var declarations = new List<DeclaredCall>();

                    foreach (var playerIndex in options.Select(o => o.PlayerIndex).Distinct())
                    {
                        var declared = CpuStrategy.ChooseCall(round, playerIndex, options, difficulty);

                        if (declared != null)
                        {
                            declarations.Add(declared);
                        }
                    }

                    var result = round.ResolveCalls(declarations);

                    if (result != null)
                    {
                        return result;
                    }

                    continue;
                }
            }

            Assert.Fail($"局が既定の反復回数（{MAX_ITERATIONS}）内に終了しませんでした");
            return null;
        }
    }
}
