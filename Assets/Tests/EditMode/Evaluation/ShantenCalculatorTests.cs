using System.Collections.Generic;
using Mahjong.Model.Common;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Evaluation.Tests
{
    [TestFixture]
    public class ShantenCalculatorTests
    {
        [Test]
        public void Calculate_CompleteHand_ReturnsMinusOne()
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

            Assert.AreEqual(-1, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_TankiWaitTenpai_ReturnsZero()
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

            Assert.AreEqual(0, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_OneShantenHand_ReturnsOne()
        {
            // 完成メンツ3つ + 搭子1つ + 浮き牌2枚（対子なし）
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    M(4), M(5),
                    Z(TileId.Haku), Z(TileId.Hatsu),
                });

            Assert.AreEqual(1, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_ChiitoitsuShape_BeatsStandardShanten()
        {
            // 6対子 + 浮き牌1枚。標準形では悪い形だが七対子形では0シャンテン
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

            Assert.AreEqual(0, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_KokushiThirteenWait_ReturnsZero()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(9), P(1), P(9), S(1), S(9),
                    Z(TileId.East), Z(TileId.South), Z(TileId.West), Z(TileId.North),
                    Z(TileId.Haku), Z(TileId.Hatsu), Z(TileId.Chun),
                });

            Assert.AreEqual(0, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_KokushiWithPair_ReturnsZero()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(9), P(1), P(9), S(1), S(9),
                    Z(TileId.East), Z(TileId.South), Z(TileId.West), Z(TileId.North),
                    Z(TileId.Haku), Z(TileId.Hatsu), Z(TileId.Hatsu),
                });

            Assert.AreEqual(0, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_WithExistingMeld_ReducesRequiredClosedMelds()
        {
            // 配牌13枚（東東 + 3メンツ + 白白）→ 東をポン → 白を1枚切って単騎テンパイにする
            var hand = CreateHand(
                new List<Tile>
                {
                    Z(TileId.East), Z(TileId.East),
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku), Z(TileId.Haku),
                });

            hand.AddMeld(Pon(Z(TileId.East), Z(TileId.East), Z(TileId.East), Wind.South));
            hand.Discard(Z(TileId.Haku));

            Assert.AreEqual(0, ShantenCalculator.Calculate(hand));
        }

        [Test]
        public void Calculate_RedFiveDoesNotAffectShanten()
        {
            var normalHand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    M(4), M(5),
                    Z(TileId.Haku), Z(TileId.Haku),
                });

            var redHand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    M(4), M(5, isRed: true),
                    Z(TileId.Haku), Z(TileId.Haku),
                });

            Assert.AreEqual(ShantenCalculator.Calculate(normalHand), ShantenCalculator.Calculate(redHand));
        }
    }
}
