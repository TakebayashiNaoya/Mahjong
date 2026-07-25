using System;
using System.Collections.Generic;
using Mahjong.Model.Cpu;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Cpu.Tests
{
    [TestFixture]
    public class CpuDiscardSelectorTests
    {
        [Test]
        public void ChooseDiscard_NormalDifficultyWithIsolatedTile_DiscardsIsolatedTile()
        {
            // 123m456m789m + 11p(雀頭) + 45s(両面)のテンパイ形に、無関係な中を1枚ツモった状態
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    M(7), M(8), M(9),
                    P(1), P(1),
                    S(4), S(5),
                },
                drawnTile: Z(TileId.Chun));

            var discard = CpuDiscardSelector.ChooseDiscard(
                hand, isThreePlayer: false, mustKeepTenpai: false, safeTiles: Array.Empty<Tile>(),
                difficulty: CpuDifficulty.Normal, random: new Random(1));

            Assert.IsTrue(discard.IsSameType(Z(TileId.Chun)));
        }

        [Test]
        public void ChooseDiscard_EasyDifficultyMustKeepTenpai_OnlyPicksTenpaiKeepingTile()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    M(7), M(8), M(9),
                    P(1), P(1),
                    S(4), S(5),
                },
                drawnTile: Z(TileId.Chun));

            // ランダム性があるためシードを変えて複数回検証しても常に中を切ること（テンパイを維持する唯一の牌）を確認する
            for (var seed = 0; seed < 20; seed++)
            {
                var discard = CpuDiscardSelector.ChooseDiscard(
                    hand, isThreePlayer: false, mustKeepTenpai: true, safeTiles: Array.Empty<Tile>(),
                    difficulty: CpuDifficulty.Easy, random: new Random(seed));

                Assert.IsTrue(discard.IsSameType(Z(TileId.Chun)), $"seed={seed} で期待と異なる牌が選ばれました: {discard}");
            }
        }

        [Test]
        public void ChooseDiscard_SafeTileAvailable_PrefersSafeTileOverEfficiency()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    M(7), M(8), M(9),
                    P(1), P(1),
                    S(4), S(5),
                },
                drawnTile: Z(TileId.Haku));

            // 4索は本来なら両面搭子を崩すため効率上は選ばれないが、安全牌として優先されるべき
            var safeTiles = new List<Tile> { S(4) };

            var discard = CpuDiscardSelector.ChooseDiscard(
                hand, isThreePlayer: false, mustKeepTenpai: false, safeTiles: safeTiles,
                difficulty: CpuDifficulty.Normal, random: new Random(1));

            Assert.IsTrue(discard.IsSameType(S(4)));
        }
    }
}
