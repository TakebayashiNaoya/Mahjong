using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Evaluation.Tests
{
    [TestFixture]
    public class YakuEvaluatorTests
    {
        [Test]
        public void SimpleHand_TanyaoAndPinfu_BothDetected()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(2), M(3), M(4),
                    P(2), P(3), P(4),
                    S(2), S(3), S(4),
                    M(6), M(6),
                    P(6), P(7),
                });

            var winningTile = P(8);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            var ids = result.Yaku.Select(y => y.Id).ToList();
            Assert.Contains(YakuId.Tanyao, ids);
            Assert.Contains(YakuId.Pinfu, ids);
        }

        [Test]
        public void OpenDragonPon_YakuhaiChun_Detected()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    Z(TileId.Chun), Z(TileId.Chun),
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku), Z(TileId.Haku),
                });

            hand.AddMeld(Pon(Z(TileId.Chun), Z(TileId.Chun), Z(TileId.Chun), Wind.South));
            hand.Discard(Z(TileId.Haku));

            var winningTile = Z(TileId.Haku);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            Assert.IsTrue(agari.IsWin);
            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.YakuhaiChun));
        }

        [Test]
        public void OpenAllTriplets_Toitoi_Detected()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    Z(TileId.East), Z(TileId.East),
                    M(2), M(2), M(2),
                    P(5), P(5), P(5),
                    S(7), S(7),
                    Z(TileId.Chun), Z(TileId.Chun),
                    M(9),
                });

            hand.AddMeld(Pon(Z(TileId.East), Z(TileId.East), Z(TileId.East), Wind.South));
            hand.Discard(M(9));

            var winningTile = Z(TileId.Chun);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            Assert.IsTrue(agari.IsWin);
            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.Toitoi));
            Assert.IsFalse(result.Yaku.Any(y => y.Id == YakuId.Sanankou), "刻子2つ(暗)+ロン刻子+副露では暗刻3つに満たない");
        }

        [Test]
        public void ThreeConcealedTriplets_WinByRon_Sanankou_NotSuuankou()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(2), M(2), M(2),
                    P(5), P(5), P(5),
                    S(7), S(7), S(7),
                    Z(TileId.Haku), Z(TileId.Haku),
                    Z(TileId.Chun), Z(TileId.Chun),
                });

            var winningTile = Z(TileId.Chun);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context(isTsumo: false));

            var ids = result.Yaku.Select(y => y.Id).ToList();
            Assert.Contains(YakuId.Sanankou, ids);
            Assert.IsFalse(ids.Contains(YakuId.Suuankou), "ロンで完成した4つ目の刻子は暗刻に数えず、四暗刻にならない");
        }

        [Test]
        public void FourConcealedTriplets_WinByTsumo_Suuankou()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(2), M(2), M(2),
                    P(5), P(5), P(5),
                    S(7), S(7), S(7),
                    Z(TileId.Haku), Z(TileId.Haku),
                    Z(TileId.Chun), Z(TileId.Chun),
                },
                drawnTile: Z(TileId.Chun));

            var winningTile = Z(TileId.Chun);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: true);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context(isTsumo: true));

            Assert.IsTrue(result.IsYakuman);
            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.Suuankou));
        }

        [Test]
        public void Chiitoitsu_YakuDetected()
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

            var winningTile = M(5);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.Chiitoitsu && y.Han == 2));
        }

        [Test]
        public void Kokushi_YakumanDetected()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(9), P(1), P(9), S(1), S(9),
                    Z(TileId.East), Z(TileId.South), Z(TileId.West), Z(TileId.North),
                    Z(TileId.Haku), Z(TileId.Hatsu), Z(TileId.Chun),
                });

            var winningTile = M(1);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            Assert.IsTrue(result.IsYakuman);
            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.KokushiMusou));
        }

        [Test]
        public void RiichiContext_RiichiYakuDetected()
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

            var winningTile = Z(TileId.Haku);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context(isRiichi: true));

            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.Riichi && y.Han == 1));
        }

        [Test]
        public void SingleSuitNoHonors_Chinitsu_NotHonitsu()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    M(7), M(8), M(9),
                    M(2), M(2),
                    M(4), M(5),
                });

            var winningTile = M(6);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            var ids = result.Yaku.Select(y => y.Id).ToList();
            Assert.Contains(YakuId.Chinitsu, ids);
            Assert.IsFalse(ids.Contains(YakuId.Honitsu));
        }

        [Test]
        public void SingleSuitWithHonors_Honitsu_NotChinitsu()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    M(4), M(5), M(6),
                    M(7), M(8), M(9),
                    Z(TileId.Haku), Z(TileId.Haku),
                    M(4), M(5),
                });

            var winningTile = M(6);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            var ids = result.Yaku.Select(y => y.Id).ToList();
            Assert.Contains(YakuId.Honitsu, ids);
            Assert.IsFalse(ids.Contains(YakuId.Chinitsu));
        }

        [Test]
        public void MultipleDecompositions_PicksHigherActualScore_NotJustHigherHan()
        {
            // A: ピンフ・ツモ・一盃口 = 3翻・固定20符 → 基本点 20*2^5 = 640
            // B: 門前ツモ・役牌(中) = 2翻・50符 → 基本点 50*2^4 = 800
            // 翻数だけならAが選ばれるが、実際の点数はBが上回る
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    M(4), M(5),
                    Z(TileId.Haku), Z(TileId.Haku),
                },
                drawnTile: M(6));

            var context = Context(isTsumo: true);

            // P/S の順子は開始数字をずらし、萬子の重複順子と偶然「三色同順」を形成しないようにする
            var decompositionA = StandardDecomposition(
                new[] { Seq(M(1), M(2), M(3)), Seq(M(1), M(2), M(3)), Seq(P(4), P(5), P(6)), Seq(S(7), S(8), S(9)) },
                Pair(M(9), M(9)),
                WaitType.Ryanmen);

            var tripletM1 = new HandGroup(GroupType.Triplet, new List<Tile> { M(1), M(1), M(1) }, isConcealed: true, containsWinningTile: false);
            var tripletChun = new HandGroup(GroupType.Triplet, new List<Tile> { Z(TileId.Chun), Z(TileId.Chun), Z(TileId.Chun) }, isConcealed: false, containsWinningTile: false);
            var decompositionB = StandardDecomposition(
                new[] { tripletM1, tripletChun, Seq(P(4), P(5), P(6)), Seq(S(7), S(8), S(9)) },
                Pair(M(9), M(9)),
                WaitType.Kanchan);

            var agari = new AgariResult(true, new List<HandDecomposition> { decompositionA, decompositionB });
            var result = YakuEvaluator.Evaluate(hand, M(6), agari, context);

            Assert.AreSame(decompositionB, result.BestDecomposition, "翻数ではなく符×翻数の実際の点数で高い方を選ぶべき");
            Assert.AreEqual(50, result.Fu);
            Assert.AreEqual(2, result.TotalHan);
        }

        [Test]
        public void Daisangen_YakumanSuppressesOtherYaku()
        {
            var hand = CreateHand(
                new List<Tile>
                {
                    Z(TileId.Haku), Z(TileId.Haku), Z(TileId.Haku),
                    Z(TileId.Hatsu), Z(TileId.Hatsu), Z(TileId.Hatsu),
                    Z(TileId.Chun), Z(TileId.Chun),
                    M(2), M(3), M(4),
                    S(6), S(6),
                });

            var winningTile = Z(TileId.Chun);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var result = YakuEvaluator.Evaluate(hand, winningTile, agari, Context());

            Assert.IsTrue(result.IsYakuman);
            Assert.IsTrue(result.Yaku.Any(y => y.Id == YakuId.Daisangen));
            Assert.AreEqual(1, result.Yaku.Count, "役満成立時は通常役をすべて破棄する");
        }
    }
}
