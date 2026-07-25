using System.Collections.Generic;
using Mahjong.Model.Cpu;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Cpu.Tests
{
    [TestFixture]
    public class CpuUkeireCalculatorTests
    {
        [Test]
        public void CountUkeireKinds_RyanmenTenpai_ReturnsTwoKinds()
        {
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(2), M(3),
                M(4), M(5), M(6),
                M(7), M(8), M(9),
                P(1), P(1),
                S(4), S(5),
            });

            var ukeire = CpuUkeireCalculator.CountUkeireKinds(hand, isThreePlayer: false);

            Assert.AreEqual(2, ukeire);
        }

        [Test]
        public void CountUkeireKinds_KanchanWaitInThreePlayer_ExcludesUnavailableManzu()
        {
            // 1萬・3萬の嵌張（2萬待ち）。三人麻雀では萬子2〜8が存在しないため有効牌数は0になる
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(3),
                P(4), P(5), P(6),
                P(7), P(8), P(9),
                S(1), S(2), S(3),
                P(1), P(1),
            });

            var ukeireFourPlayer = CpuUkeireCalculator.CountUkeireKinds(hand, isThreePlayer: false);
            var ukeireThreePlayer = CpuUkeireCalculator.CountUkeireKinds(hand, isThreePlayer: true);

            Assert.AreEqual(1, ukeireFourPlayer);
            Assert.AreEqual(0, ukeireThreePlayer);
        }
    }
}
