using System.Collections.Generic;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Evaluation.Tests;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Scoring.Tests
{
    [TestFixture]
    public class ScoreCalculatorTests
    {
        [Test]
        public void KnownReference_30Fu4Han_DealerRon_Returns11600()
        {
            // リーチ・ピンフ・タンヤオ・赤ドラ1 = 4翻30符（教科書的な参照値：親ロン11600点）
            // 3スートの順子が偶然「三色同順」を形成しないよう、開始数字をずらしている
            var hand = CreateHand(
                new List<Tile>
                {
                    M(2), M(3), M(4),
                    P(3), P(4), P(5),
                    S(4), S(5), S(6),
                    M(6), M(7),
                    P(8), P(8, isRed: true),
                });

            var winningTile = M(8);
            var context = Context(isTsumo: false, isRiichi: true, isDealer: true);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var yakuResult = YakuEvaluator.Evaluate(hand, winningTile, agari, context);

            var score = ScoreCalculator.Calculate(hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0);

            Assert.AreEqual(30, score.Fu);
            Assert.AreEqual(4, score.Han);
            Assert.AreEqual(1920, score.BasicPoints);
            Assert.AreEqual(11600, score.Payment.DiscarderAmount);
        }

        [Test]
        public void Yakuman_ChildRon_Pays32000()
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
            var context = Context(isDealer: false);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var yakuResult = YakuEvaluator.Evaluate(hand, winningTile, agari, context);

            var score = ScoreCalculator.Calculate(hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0);

            Assert.IsTrue(score.IsYakuman);
            Assert.AreEqual(8000, score.BasicPoints);
            Assert.AreEqual(32000, score.Payment.DiscarderAmount);
        }

        [Test]
        public void Yakuman_DealerRon_Pays48000()
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
            var context = Context(isDealer: true);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var yakuResult = YakuEvaluator.Evaluate(hand, winningTile, agari, context);

            var score = ScoreCalculator.Calculate(hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0);

            Assert.AreEqual(48000, score.Payment.DiscarderAmount);
        }

        [Test]
        public void IndicatorDoraHan_AddsToEffectiveHan()
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
            var context = Context(isTsumo: false, isRiichi: true);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var yakuResult = YakuEvaluator.Evaluate(hand, winningTile, agari, context);

            var withoutIndicatorDora = ScoreCalculator.Calculate(
                hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0, indicatorDoraHan: 0);
            var withIndicatorDora = ScoreCalculator.Calculate(
                hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0, indicatorDoraHan: 2);

            Assert.AreEqual(withoutIndicatorDora.Han + 2, withIndicatorDora.Han);
        }

        [Test]
        public void HonbaAndRiichiSticks_AddToWinnerGain()
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
            var context = Context(isTsumo: false, isRiichi: true);
            var agari = AgariChecker.CheckWin(hand, winningTile, isTsumo: false);
            var yakuResult = YakuEvaluator.Evaluate(hand, winningTile, agari, context);

            var withoutExtras = ScoreCalculator.Calculate(hand, winningTile, yakuResult, context, honbaCount: 0, riichiStickCount: 0);
            var withExtras = ScoreCalculator.Calculate(hand, winningTile, yakuResult, context, honbaCount: 2, riichiStickCount: 1);

            Assert.AreEqual(withoutExtras.Payment.DiscarderAmount + 600, withExtras.Payment.DiscarderAmount);
            Assert.AreEqual(1000, withExtras.Payment.RiichiStickGain);
        }
    }
}
