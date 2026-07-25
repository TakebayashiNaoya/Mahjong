using System;
using System.Collections.Generic;
using Mahjong.Model.Common;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Game.Tests
{
    [TestFixture]
    public class MahjongGameTests
    {
        [Test]
        public void Constructor_FourPlayerDefault_CreatesPlayersWithInitialScore()
        {
            var settings = GameSettings.CreateDefault(4, GameLengthType.HalfGame);
            var game = new MahjongGame(settings, new Random(1));

            Assert.AreEqual(4, game.Players.Count);

            foreach (var player in game.Players)
            {
                Assert.AreEqual(25000, player.Score);
            }

            Assert.AreEqual(Wind.East, game.CurrentRoundWind);
            Assert.AreEqual(1, game.CurrentRoundNumber);
            Assert.IsFalse(game.IsGameOver);
        }

        [Test]
        public void ApplyRoundResult_DealerTsumoWin_IncrementsHonbaAndKeepsDealer()
        {
            var settings = GameSettings.CreateDefault(4, GameLengthType.HalfGame);
            var game = new MahjongGame(settings, new Random(2));
            var dealerBefore = game.DealerIndex;

            var result = ForceDealerTsumoAndApply(game, new Random(3));

            Assert.IsTrue(result.DealerContinues);
            Assert.AreEqual(dealerBefore, game.DealerIndex);
            Assert.AreEqual(1, game.HonbaCount);
            Assert.AreEqual(1, game.CurrentRoundNumber);
        }

        [Test]
        public void ApplyRoundResult_NonDealerRonWin_RotatesDealerAndResetsHonba()
        {
            var settings = GameSettings.CreateDefault(4, GameLengthType.HalfGame);
            var game = new MahjongGame(settings, new Random(4));
            var dealerBefore = game.DealerIndex;

            var result = ForceNonDealerRonAndApply(game, new Random(5));

            Assert.IsFalse(result.DealerContinues);
            Assert.AreEqual((dealerBefore + 1) % 4, game.DealerIndex);
            Assert.AreEqual(0, game.HonbaCount);
            Assert.AreEqual(2, game.CurrentRoundNumber);
        }

        [Test]
        public void ApplyRoundResult_EastOnlyGameFinishesFinalRound_EndsGame()
        {
            var settings = new GameSettings(4, GameLengthType.EastOnly, 25000, 30000);
            var game = new MahjongGame(settings, new Random(6));

            for (var round = 1; round <= 4; round++)
            {
                ForceNonDealerRonAndApply(game, new Random(10 + round));
            }

            Assert.IsTrue(game.IsGameOver);
        }

        [Test]
        public void ApplyRoundResult_HalfGameFinishesEastRounds_AdvancesToSouthWind()
        {
            var settings = new GameSettings(4, GameLengthType.HalfGame, 25000, 30000);
            var game = new MahjongGame(settings, new Random(7));

            for (var round = 1; round <= 4; round++)
            {
                ForceNonDealerRonAndApply(game, new Random(30 + round));
            }

            Assert.IsFalse(game.IsGameOver);
            Assert.AreEqual(Wind.South, game.CurrentRoundWind);
            Assert.AreEqual(1, game.CurrentRoundNumber);
        }

        [Test]
        public void StartNextRound_AfterGameOver_Throws()
        {
            var settings = new GameSettings(4, GameLengthType.EastOnly, 25000, 30000);
            var game = new MahjongGame(settings, new Random(8));

            for (var round = 1; round <= 4; round++)
            {
                ForceNonDealerRonAndApply(game, new Random(40 + round));
            }

            Assert.Throws<InvalidOperationException>(() => game.StartNextRound());
        }

        [Test]
        public void ApplyRoundResult_TobiEnabledAndScoreBelowZero_EndsGame()
        {
            var settings = new GameSettings(4, GameLengthType.HalfGame, 25000, 30000, enableTobi: true);
            var game = new MahjongGame(settings, new Random(9));

            // Round は放銃者の持ち点をマイナスにはしない通常の点数移動しか起こさないため、
            // 飛びの判定自体はここで直接持ち点を操作して検証する
            game.StartNextRound(new Random(11));
            game.Players[1].AddScore(-30000);

            ForceNonDealerRonAndApply(game, new Random(12), useExistingRound: true);

            Assert.IsTrue(game.IsGameOver);
        }


        // ========================================
        // テストヘルパー
        // ========================================
        /// <summary>
        /// 親に和了役（役牌）付きのツモを成立させ、結果を MahjongGame に反映する
        /// </summary>
        private static RoundResult ForceDealerTsumoAndApply(MahjongGame game, Random random)
        {
            var round = game.StartNextRound(random);
            var dealer = round.Players[round.DealerIndex];

            dealer.Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), P(4), P(5), P(6), S(6), S(7), S(8),
                Z(TileId.Haku), Z(TileId.Haku), Z(TileId.Haku), M(9), M(9),
            });
            dealer.Hand.Draw(M(1));

            var result = round.DeclareTsumoWin();
            game.ApplyRoundResult(result);
            return result;
        }
        /// <summary>
        /// 親の隣家（子）にタンヤオ・ピンフ確定のロンを成立させ、結果を MahjongGame に反映する
        /// </summary>
        private static RoundResult ForceNonDealerRonAndApply(MahjongGame game, Random random, bool useExistingRound = false)
        {
            var round = useExistingRound ? game.CurrentRound : game.StartNextRound(random);
            var discarderIndex = round.DealerIndex;
            var winnerIndex = (discarderIndex + 1) % game.Players.Count;

            round.Players[winnerIndex].Hand.SetInitialTiles(new List<Tile>
            {
                M(2), M(3), M(4), P(2), P(3), P(4), S(2), S(2), S(4), S(5), P(6), P(7), P(8),
            });

            round.Players[discarderIndex].Hand.SetInitialTiles(new List<Tile>
            {
                S(6), M(1), M(1), M(1), P(1), P(1), P(1), S(1), S(1), S(1),
                Z(TileId.East), Z(TileId.East), Z(TileId.East),
            });

            round.Discard(S(6));
            var result = round.ResolveCalls(new List<DeclaredCall> { new DeclaredCall(winnerIndex, CallType.Ron, Array.Empty<Tile>()) });
            game.ApplyRoundResult(result);
            return result;
        }
    }
}
