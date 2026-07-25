using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Game.Tests
{
    [TestFixture]
    public class RoundTests
    {
        // ========================================
        // 配牌・初期状態
        // ========================================
        [Test]
        public void Constructor_FourPlayerGame_DealsThirteenTilesAndDrawsForDealer()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 1);

            Assert.AreEqual(TurnPhase.AwaitingDiscard, round.Phase);
            Assert.AreEqual(0, round.CurrentPlayerIndex);
            Assert.AreEqual(14, players[0].Hand.TileCount);

            for (var i = 1; i < 4; i++)
            {
                Assert.AreEqual(13, players[i].Hand.TileCount);
            }
        }

        [Test]
        public void Constructor_ThreePlayerGame_AssignsEastSouthWestOnlyStartingFromDealer()
        {
            var round = CreateRound(out var players, playerCount: 3, dealerIndex: 1, seed: 2);

            Assert.AreEqual(Wind.East, players[1].SeatWind);
            Assert.AreEqual(Wind.South, players[2].SeatWind);
            Assert.AreEqual(Wind.West, players[0].SeatWind);
        }


        // ========================================
        // ツモ和了
        // ========================================
        [Test]
        public void DeclareTsumoWin_DealerYakuhaiHand_EndsRoundWithRenchan()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 3);
            var dealer = players[0];

            dealer.Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), P(4), P(5), P(6), S(6), S(7), S(8),
                Z(TileId.Haku), Z(TileId.Haku), Z(TileId.Haku), M(9), M(9),
            });
            dealer.Hand.Draw(M(1));

            Assert.IsTrue(round.CanDeclareTsumoWin());

            var result = round.DeclareTsumoWin();

            Assert.AreEqual(RoundEndReason.Tsumo, result.Reason);
            Assert.IsTrue(result.DealerContinues);
            Assert.AreEqual(1, result.Wins.Count);
            Assert.AreEqual(0, result.Wins[0].WinnerIndex);
            Assert.AreEqual(0, result.ScoreDeltas.Sum());
            Assert.Greater(result.ScoreDeltas[0], 0);
        }


        // ========================================
        // ロン
        // ========================================
        [Test]
        public void GetAvailableCalls_YakulessShanponWait_DoesNotOfferRon()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 4);

            // 234m 456p 678s 99s 22p（シャンポン待ち、役なし）
            players[1].Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), M(4), P(4), P(5), P(6), S(6), S(7), S(8), S(9), S(9), P(2), P(2),
            });

            players[0].Hand.SetInitialTiles(new List<Tile>
            {
                S(9), M(1), M(1), M(1), P(1), P(1), P(1), S(1), S(1), S(1),
                Z(TileId.East), Z(TileId.East), Z(TileId.East),
            });

            round.Discard(S(9));
            var options = round.GetAvailableCalls(S(9), 0);

            Assert.IsFalse(options.Any(o => o.PlayerIndex == 1 && o.Type == CallType.Ron));
        }

        [Test]
        public void ResolveCalls_RonWithTanyaoAndPinfu_EndsRoundAndConservesPoints()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 5);

            // 234m 234p 22s 45s 678p（両面待ち3-6索、タンヤオ・ピンフ確定）
            players[1].Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), M(4), P(2), P(3), P(4), S(2), S(2), S(4), S(5), P(6), P(7), P(8),
            });

            players[0].Hand.SetInitialTiles(new List<Tile>
            {
                S(6), M(1), M(1), M(1), P(1), P(1), P(1), S(1), S(1), S(1),
                Z(TileId.East), Z(TileId.East), Z(TileId.East),
            });

            round.Discard(S(6));
            var options = round.GetAvailableCalls(S(6), 0);
            Assert.IsTrue(options.Any(o => o.PlayerIndex == 1 && o.Type == CallType.Ron));

            var result = round.ResolveCalls(new List<DeclaredCall> { new DeclaredCall(1, CallType.Ron, Array.Empty<Tile>()) });

            Assert.IsNotNull(result);
            Assert.AreEqual(RoundEndReason.Ron, result.Reason);
            Assert.AreEqual(1, result.Wins.Count);
            Assert.AreEqual(1, result.Wins[0].WinnerIndex);
            Assert.AreEqual(0, result.Wins[0].DiscarderIndex);
            Assert.AreEqual(0, result.ScoreDeltas.Sum());
            Assert.Less(result.ScoreDeltas[0], 0);
            Assert.Greater(result.ScoreDeltas[1], 0);
        }


        // ========================================
        // ポン
        // ========================================
        [Test]
        public void ResolveCalls_Pon_MovesTurnToCallerAndAppliesMeld()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 6);

            players[2].Hand.SetInitialTiles(new List<Tile>
            {
                Z(TileId.Haku), Z(TileId.Haku), M(1), M(2), M(3), P(1), P(2), P(3), S(1), S(2), S(3), M(9), M(9),
            });

            players[0].Hand.SetInitialTiles(new List<Tile>
            {
                Z(TileId.Haku), M(4), M(5), M(6), P(4), P(5), P(6), S(4), S(5), S(6), P(9), P(9), P(9),
            });

            round.Discard(Z(TileId.Haku));
            var options = round.GetAvailableCalls(Z(TileId.Haku), 0);
            Assert.IsTrue(options.Any(o => o.PlayerIndex == 2 && o.Type == CallType.Pon));

            var declarations = new List<DeclaredCall>
            {
                new DeclaredCall(2, CallType.Pon, new List<Tile> { Z(TileId.Haku), Z(TileId.Haku) }),
            };

            var result = round.ResolveCalls(declarations);

            Assert.IsNull(result);
            Assert.AreEqual(2, round.CurrentPlayerIndex);
            Assert.AreEqual(TurnPhase.AwaitingDiscard, round.Phase);
            Assert.AreEqual(1, players[2].Hand.Melds.Count);
            Assert.AreEqual(MeldType.Pon, players[2].Hand.Melds[0].Type);
        }


        // ========================================
        // リーチ
        // ========================================
        [Test]
        public void DeclareRiichi_TenpaiHand_DeductsScoreAndKeepsTenpaiAfterDiscard()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 7);
            var dealer = players[0];

            dealer.Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), M(4), P(2), P(3), P(4), S(2), S(2), S(4), S(5), P(6), P(7), P(8),
            });
            dealer.Hand.Draw(M(9));

            Assert.IsTrue(round.CanDeclareRiichi());
            var scoreBefore = dealer.Score;

            round.DeclareRiichi();

            Assert.IsTrue(dealer.HandState.IsRiichi);
            Assert.AreEqual(scoreBefore - 1000, dealer.Score);
            Assert.AreEqual(1, round.RiichiStickCount);

            round.Discard(M(9));
            Assert.AreEqual(TurnPhase.AwaitingReactions, round.Phase);
        }


        // ========================================
        // 途中流局
        // ========================================
        [Test]
        public void Discard_AllPlayersDiscardSameWindFirst_TriggersSuufuurenda()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 8);

            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, round.CurrentPlayerIndex);

                if (i > 0)
                {
                    round.DrawTile();
                }

                players[i].Hand.SetInitialTiles(new List<Tile>
                {
                    Z(TileId.East), M(2), M(3), M(4), P(2), P(3), P(4), S(2), S(3), S(4), M(6), M(7), M(8),
                });

                round.Discard(Z(TileId.East));

                if (i < 3)
                {
                    Assert.IsNull(round.PendingAbortiveDraw);
                    round.ResolveCalls(Array.Empty<DeclaredCall>());
                }
            }

            Assert.AreEqual(AbortiveDrawReason.SuufuRenda, round.PendingAbortiveDraw);

            var result = round.FinalizeAbortiveDraw();

            Assert.AreEqual(RoundEndReason.AbortiveDraw, result.Reason);
            Assert.AreEqual(AbortiveDrawReason.SuufuRenda, result.AbortiveReason);
            Assert.IsTrue(result.DealerContinues);
            Assert.IsTrue(result.ScoreDeltas.All(d => d == 0));
        }

        [Test]
        public void DeclareKyuushuKyuuhai_NineOrMoreYaochuKinds_EndsRoundAsAbortiveDraw()
        {
            var round = CreateRound(out var players, playerCount: 4, dealerIndex: 0, seed: 9);

            players[0].Hand.SetInitialTiles(new List<Tile>
            {
                M(1), M(9), P(1), P(9), S(1), S(9),
                Z(TileId.East), Z(TileId.South), Z(TileId.West), Z(TileId.North), Z(TileId.Haku), Z(TileId.Hatsu),
                M(5),
            });
            players[0].Hand.Draw(Z(TileId.Chun));

            Assert.IsTrue(round.CanDeclareKyuushuKyuuhai());

            var result = round.DeclareKyuushuKyuuhai();

            Assert.AreEqual(RoundEndReason.AbortiveDraw, result.Reason);
            Assert.AreEqual(AbortiveDrawReason.KyuushuKyuuhai, result.AbortiveReason);
            Assert.IsTrue(result.DealerContinues);
        }

        [Test]
        public void DeclareExhaustiveDraw_WallNotEmpty_Throws()
        {
            var round = CreateRound(out _, playerCount: 4, dealerIndex: 0, seed: 10);

            Assert.Throws<InvalidOperationException>(() => round.DeclareExhaustiveDraw());
        }


        // ========================================
        // テストヘルパー
        // ========================================
        private static Round CreateRound(out List<PlayerState> players, int playerCount, int dealerIndex, int seed)
        {
            var settings = GameSettings.CreateDefault(playerCount, GameLengthType.HalfGame);
            players = Enumerable.Range(0, playerCount).Select(i => new PlayerState(i, settings.InitialScore)).ToList();

            return new Round(settings, players, Wind.East, roundNumber: 1, dealerIndex, honbaCount: 0, riichiStickCount: 0, new Random(seed));
        }
    }
}
