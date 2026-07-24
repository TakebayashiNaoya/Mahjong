using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Evaluation.Tests
{
    [TestFixture]
    public class AgariCheckerTests
    {
        [Test]
        public void CheckWin_StandardForm_Ron_IsWin()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku),
                });

            var result = AgariChecker.CheckWin(hand, Z(TileId.Haku), isTsumo: false);

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Standard));
        }

        [Test]
        public void CheckWin_StandardForm_Tsumo_IsWin()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku),
                },
                drawnTile: Z(TileId.Haku));

            var result = AgariChecker.CheckWin(hand, Z(TileId.Haku), isTsumo: true);

            Assert.IsTrue(result.IsWin);
        }

        [Test]
        public void CheckWin_Chiitoitsu_IsWin()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(1),
                    P(2), P(2),
                    S(3), S(3),
                    Z(TileId.East), Z(TileId.East),
                    Z(TileId.South), Z(TileId.South),
                    Z(TileId.West), Z(TileId.West),
                    M(5),
                });

            var result = AgariChecker.CheckWin(hand, M(5), isTsumo: false);

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Chiitoitsu));
        }

        [Test]
        public void CheckWin_QuadTileIsNotCountedAsTwoPairs_NotWin()
        {
            // M1が4枚、他は5種類の対子。七対子としても標準形としても和了にならない
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(1), M(1),
                    P(2), P(2),
                    S(3), S(3),
                    Z(TileId.East), Z(TileId.East),
                    Z(TileId.South), Z(TileId.South),
                    Z(TileId.West), Z(TileId.West),
                });

            var result = AgariChecker.CheckWin(hand, M(1), isTsumo: false);

            Assert.IsFalse(result.IsWin);
        }

        [Test]
        public void CheckWin_KokushiThirteenWait_IsWin()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(9), P(1), P(9), S(1), S(9),
                    Z(TileId.East), Z(TileId.South), Z(TileId.West), Z(TileId.North),
                    Z(TileId.Haku), Z(TileId.Hatsu), Z(TileId.Chun),
                });

            var result = AgariChecker.CheckWin(hand, M(1), isTsumo: false);

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Kokushi));
        }

        [Test]
        public void CheckWin_NonTenpaiHand_NotWin()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(3), M(5), M(7), M(9),
                    P(2), P(4), P(6), P(8),
                    S(1), S(3), S(5), S(7),
                });

            var result = AgariChecker.CheckWin(hand, M(2), isTsumo: false);

            Assert.IsFalse(result.IsWin);
        }

        [Test]
        public void CheckWin_RyanmenWait_DetectsRyanmen()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku), Z(TileId.Haku),
                    M(5), M(6),
                });

            var result = AgariChecker.CheckWin(hand, M(7), isTsumo: false);

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Standard && d.WaitType == WaitType.Ryanmen));
        }

        [Test]
        public void CheckWin_TankiWait_DetectsTanki()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku),
                });

            var result = AgariChecker.CheckWin(hand, Z(TileId.Haku), isTsumo: false);

            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Standard && d.WaitType == WaitType.Tanki));
        }

        [Test]
        public void CheckWin_ShanponWait_DetectsShanpon()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    M(9), M(9),
                    Z(TileId.Haku), Z(TileId.Haku),
                });

            var result = AgariChecker.CheckWin(hand, Z(TileId.Haku), isTsumo: false);

            Assert.IsTrue(result.IsWin);
            Assert.IsTrue(result.Decompositions.Any(d => d.Form == WinningForm.Standard && d.WaitType == WaitType.Shanpon));
        }

        [Test]
        public void CheckWin_RonCompletedTriplet_IsNotConcealed()
        {
            // 暗刻3つ（萬2・筒5・索7）+ 白の対子 + 中の対子がロンで刻子化する
            var hand = CreateHand(
                new List<Tile>
                {
                    M(2), M(2), M(2),
                    P(5), P(5), P(5),
                    S(7), S(7), S(7),
                    Z(TileId.Haku), Z(TileId.Haku),
                    Z(TileId.Chun), Z(TileId.Chun),
                });

            var result = AgariChecker.CheckWin(hand, Z(TileId.Chun), isTsumo: false);

            Assert.IsTrue(result.IsWin);

            var standard = result.Decompositions.First(d => d.Form == WinningForm.Standard);
            var chunGroup = standard.Groups.First(g => g.Type == GroupType.Triplet && g.Tiles[0].Id == TileId.Chun);
            var manzuGroup = standard.Groups.First(g => g.Type == GroupType.Triplet && g.Tiles[0].Id == TileId.Manzu2);

            Assert.IsFalse(chunGroup.IsConcealed, "ロンで完成した刻子は暗刻として数えない");
            Assert.IsTrue(manzuGroup.IsConcealed);
        }
    }
}
