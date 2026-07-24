using System.Collections.Generic;
using Mahjong.Model.Common;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Evaluation.Tests
{
    [TestFixture]
    public class FuCalculatorTests
    {
        // ========================================
        // 基本符・ツモ符・喫い平和形・ピンフツモ
        // ========================================
        [Test]
        public void Calculate_MenzenRon_AllSequencesRyanmenNonYakuhaiPair_Returns30()
        {
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: true, isPinfu: false);

            Assert.AreEqual(30, fu);
        }

        [Test]
        public void Calculate_MenzenTsumo_AllSequencesRyanmenNonYakuhaiPair_NotFlaggedPinfu_Returns40()
        {
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            // isPinfu=false を明示的に渡した場合の生の式（30+ツモ2=32→切り上げ40）を確認する
            // ピンフ成立時は呼び出し元(YakuEvaluator)が isPinfu=true を渡すため実際には固定20符になる（別テストで検証）
            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: true), isMenzen: true, isPinfu: false);

            Assert.AreEqual(40, fu);
        }

        [Test]
        public void Calculate_PinfuTsumo_ReturnsFixed20()
        {
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: true), isMenzen: true, isPinfu: true);

            Assert.AreEqual(20, fu);
        }

        [Test]
        public void Calculate_KuipinfuShapeRon_FloorsTo30()
        {
            // 開いた手・全順子・両面待ち・非役牌雀頭（鳴いているためピンフ不成立）
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: false, isPinfu: false);

            Assert.AreEqual(30, fu);
        }


        // ========================================
        // 待ちの符
        // ========================================
        [TestCase(WaitType.Ryanmen, 30)]
        [TestCase(WaitType.Shanpon, 30)]
        [TestCase(WaitType.Kanchan, 40)]
        [TestCase(WaitType.Penchan, 40)]
        [TestCase(WaitType.Tanki, 40)]
        public void Calculate_WaitFu(WaitType waitType, int expectedFu)
        {
            // 待ち符以外の加符が発生しない形にするため、雀頭は非役牌、面子はすべて順子とする
            // (Shanpon はテスト対象外の刻子形状を意味するため、ここでは待ち符の値だけを比較する目的で
            //  順子形状のまま WaitType だけ差し替える)
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                waitType);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: true, isPinfu: false);

            Assert.AreEqual(expectedFu, fu);
        }


        // ========================================
        // 面子の符（開/暗 × 刻子/槓子 × 中張/么九）
        // ========================================
        [TestCase(GroupType.Triplet, false, false, 20 + 10 + 2)]  // 明刻・中張 = 2符
        [TestCase(GroupType.Triplet, false, true, 20 + 10 + 4)]   // 明刻・么九 = 4符
        [TestCase(GroupType.Triplet, true, false, 20 + 10 + 4)]   // 暗刻・中張 = 4符
        [TestCase(GroupType.Triplet, true, true, 20 + 10 + 8)]    // 暗刻・么九 = 8符
        [TestCase(GroupType.Quad, false, false, 20 + 10 + 8)]     // 明槓・中張 = 8符
        [TestCase(GroupType.Quad, false, true, 20 + 10 + 16)]     // 明槓・么九 = 16符
        [TestCase(GroupType.Quad, true, false, 20 + 10 + 16)]     // 暗槓・中張 = 16符
        [TestCase(GroupType.Quad, true, true, 20 + 10 + 32)]      // 暗槓・么九 = 32符
        public void Calculate_MeldFu(GroupType type, bool isConcealed, bool isYaochu, int expectedRawFu)
        {
            var tile = isYaochu ? M(1) : M(5);
            var meldGroup = new HandGroup(type, new List<Tile> { tile, tile, tile }, isConcealed, false);

            var decomposition = StandardDecomposition(
                new[] { meldGroup, Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: true, isPinfu: false);

            var expected = RoundUpToTen(expectedRawFu);
            Assert.AreEqual(expected, fu);
        }


        // ========================================
        // 雀頭の符
        // ========================================
        [Test]
        public void Calculate_YakuhaiPair_Adds2Fu()
        {
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(Z(TileId.Chun), Z(TileId.Chun)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: true, isPinfu: false);

            Assert.AreEqual(RoundUpToTen(20 + 10 + 2), fu);
        }

        [Test]
        public void Calculate_DoubleWindPair_StillAdds2FuOnly()
        {
            // 自風=場風=東（連風牌）でも符は倍にしない（2符のみ）
            // 開いた手＋暗刻(么九,+8)を組み合わせ、生の符が28になるようにする。
            // 雀頭符が正しく+2なら28+2=30（丸めなし）、もし誤って+4に倍化されると
            // 28+4=32→切り上げ40になり区別できる（丸めの偶然一致で誤魔化されない構成）
            var ankouYaochu = new HandGroup(GroupType.Triplet, new List<Tile> { M(1), M(1), M(1) }, isConcealed: true, containsWinningTile: false);
            var decomposition = StandardDecomposition(
                new[] { ankouYaochu, Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(Z(TileId.East), Z(TileId.East)),
                WaitType.Ryanmen);

            var context = Context(seatWind: Wind.East, roundWind: Wind.East);
            var fu = FuCalculator.Calculate(decomposition, context, isMenzen: false, isPinfu: false);

            Assert.AreEqual(30, fu);
        }

        [Test]
        public void Calculate_NonYakuhaiPair_Adds0Fu()
        {
            var decomposition = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(P(1), P(2), P(3)), Seq(S(1), S(2), S(3)), Seq(M(4), M(5), M(6)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var fu = FuCalculator.Calculate(decomposition, Context(isTsumo: false), isMenzen: true, isPinfu: false);

            Assert.AreEqual(30, fu);
        }


        // ========================================
        // 七対子・国士無双
        // ========================================
        [Test]
        public void Calculate_Chiitoitsu_ReturnsFixed25_NoRounding()
        {
            var decomposition = new HandDecomposition(WinningForm.Chiitoitsu, System.Array.Empty<HandGroup>(), null, WaitType.Tanki);

            var fu = FuCalculator.Calculate(decomposition, Context(), isMenzen: true, isPinfu: false);

            Assert.AreEqual(25, fu);
        }

        [Test]
        public void Calculate_Kokushi_ReturnsZeroSentinel()
        {
            var decomposition = new HandDecomposition(WinningForm.Kokushi, System.Array.Empty<HandGroup>(), null, WaitType.Tanki);

            var fu = FuCalculator.Calculate(decomposition, Context(), isMenzen: true, isPinfu: false);

            Assert.AreEqual(0, fu);
        }


        // ========================================
        // 丸めの境界値
        // ========================================
        [TestCase(21, 30)]
        [TestCase(22, 30)]
        [TestCase(30, 30)]
        [TestCase(31, 40)]
        [TestCase(32, 40)]
        [TestCase(40, 40)]
        public void RoundUpToTen_BoundaryValues(int raw, int expected)
        {
            Assert.AreEqual(expected, RoundUpToTen(raw));
        }


        // ========================================
        // テストヘルパー
        // ========================================
        private static int RoundUpToTen(int fu)
        {
            return (fu + 9) / 10 * 10;
        }
    }
}
