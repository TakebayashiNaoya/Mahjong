using System.Collections.Generic;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Cpu.Tests
{
    [TestFixture]
    public class CpuCallSelectorTests
    {
        [Test]
        public void ChooseCall_RonOptionAvailable_AlwaysChoosesRon()
        {
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(2), M(3),
                M(4), M(5), M(6),
                M(7), M(8), M(9),
                P(1), P(1),
                S(4), S(5),
            });

            var options = new List<CallOption>
            {
                new(2, CallType.Ron, new List<IReadOnlyList<Tile>> { new List<Tile> { S(6) } }),
                new(2, CallType.Pon, new List<IReadOnlyList<Tile>> { new List<Tile> { P(1), P(1) } }),
            };

            var declared = CpuCallSelector.ChooseCall(hand, options, CpuDifficulty.Normal);

            Assert.IsNotNull(declared);
            Assert.AreEqual(CallType.Ron, declared.Type);
        }

        [Test]
        public void ChooseCall_EasyDifficultyNonRonOption_ReturnsNull()
        {
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(2), M(3),
                M(4), M(5), M(6),
                P(1), P(1),
                S(1), S(1),
                S(4), S(5),
                Z(TileId.Chun),
            });

            var options = new List<CallOption>
            {
                new(1, CallType.Pon, new List<IReadOnlyList<Tile>> { new List<Tile> { P(1), P(1) } }),
            };

            var declared = CpuCallSelector.ChooseCall(hand, options, CpuDifficulty.Easy);

            Assert.IsNull(declared);
        }

        [Test]
        public void ChooseCall_NormalDifficultyPonImprovesShanten_ChoosesPon()
        {
            // 123m456m + 11p(ポン候補) + 11s(雀頭) + 45s(両面) + 中(浮き牌) の1シャンテン
            // 1筒をポンすると、123m456m + ポン1筒 + 11s(雀頭) + 45s(両面) のテンパイになる
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(2), M(3),
                M(4), M(5), M(6),
                P(1), P(1),
                S(1), S(1),
                S(4), S(5),
                Z(TileId.Chun),
            });

            var options = new List<CallOption>
            {
                new(1, CallType.Pon, new List<IReadOnlyList<Tile>> { new List<Tile> { P(1), P(1) } }),
            };

            var declared = CpuCallSelector.ChooseCall(hand, options, CpuDifficulty.Normal);

            Assert.IsNotNull(declared);
            Assert.AreEqual(CallType.Pon, declared.Type);
        }

        [Test]
        public void ChooseCall_NormalDifficultyPonDoesNotImproveShanten_ReturnsNull()
        {
            // 12m(搭子) + 78m(搭子) + 456m(完成) + 123s(完成) + 白白(雀頭候補) + 9索(浮き牌) の1シャンテン
            // 白をポンすると雀頭を失い、シャンテン数が変わらないため鳴くべきではない
            var hand = CreateHand(new List<Tile>
            {
                M(1), M(2),
                M(7), M(8),
                M(4), M(5), M(6),
                S(1), S(2), S(3),
                Z(TileId.Haku), Z(TileId.Haku),
                S(9),
            });

            var options = new List<CallOption>
            {
                new(1, CallType.Pon, new List<IReadOnlyList<Tile>> { new List<Tile> { Z(TileId.Haku), Z(TileId.Haku) } }),
            };

            var declared = CpuCallSelector.ChooseCall(hand, options, CpuDifficulty.Normal);

            Assert.IsNull(declared);
        }
    }
}
