using NUnit.Framework;

namespace Mahjong.Model.Scoring.Tests
{
    [TestFixture]
    public class PaymentCalculatorTests
    {
        // ========================================
        // ロン
        // ========================================
        [Test]
        public void Calculate_NonDealerRon_Multiplies4()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: false, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.IsFalse(payment.IsTsumo);
            Assert.AreEqual(4000, payment.DiscarderAmount);
            Assert.AreEqual(4000, payment.TotalWinnerGain);
        }

        [Test]
        public void Calculate_DealerRon_Multiplies6()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: true, isTsumo: false, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.AreEqual(6000, payment.DiscarderAmount);
        }

        [Test]
        public void Calculate_Ron_RoundsUpToHundred()
        {
            // 333 * 4 = 1332 -> 1400 に切り上げ
            var payment = PaymentCalculator.Calculate(
                basicPoints: 333, isDealer: false, isTsumo: false, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.AreEqual(1400, payment.DiscarderAmount);
        }

        [Test]
        public void Calculate_Ron_WithHonba_AddsToDiscarderOnly()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: false, honbaCount: 2, riichiStickCount: 0, playerCount: 4);

            // 4000 + 2*300
            Assert.AreEqual(4600, payment.DiscarderAmount);
        }

        [Test]
        public void Calculate_Ron_WithRiichiSticks_AddsToTotalOnlyNotDiscarder()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: false, honbaCount: 0, riichiStickCount: 3, playerCount: 4);

            Assert.AreEqual(4000, payment.DiscarderAmount, "供託は放銃者の支払いには含めない");
            Assert.AreEqual(3000, payment.RiichiStickGain);
            Assert.AreEqual(7000, payment.TotalWinnerGain);
        }


        // ========================================
        // ツモ（4人麻雀）
        // ========================================
        [Test]
        public void Calculate_DealerTsumo_AllOthersPayDouble()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: true, isTsumo: true, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.IsTrue(payment.IsTsumo);
            Assert.AreEqual(2000, payment.NonDealerPaymentAmount);
            Assert.AreEqual(3, payment.NonDealerPayerCount);
            Assert.AreEqual(0, payment.DealerPaymentAmount);
            Assert.AreEqual(6000, payment.TotalWinnerGain);
        }

        [Test]
        public void Calculate_NonDealerTsumo_DealerPaysDoubleOthersPaySingle()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: true, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.AreEqual(2000, payment.DealerPaymentAmount);
            Assert.AreEqual(1000, payment.NonDealerPaymentAmount);
            Assert.AreEqual(2, payment.NonDealerPayerCount);
            Assert.AreEqual(4000, payment.TotalWinnerGain);
        }

        [Test]
        public void Calculate_Tsumo_HonbaAddsToEachPayerIndependently()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: true, honbaCount: 1, riichiStickCount: 0, playerCount: 4);

            // 親: 2000+100=2100 / 子: 1000+100=1100 x2
            Assert.AreEqual(2100, payment.DealerPaymentAmount);
            Assert.AreEqual(1100, payment.NonDealerPaymentAmount);
            Assert.AreEqual(2100 + 1100 * 2, payment.TotalWinnerGain);
        }

        [Test]
        public void Calculate_Tsumo_EachPaymentRoundedIndependently()
        {
            // 175 * 2 = 350 -> 400、175 * 1 = 175 -> 200（総和はロン一括丸めの700とは一致しない場合がある）
            var payment = PaymentCalculator.Calculate(
                basicPoints: 175, isDealer: false, isTsumo: true, honbaCount: 0, riichiStickCount: 0, playerCount: 4);

            Assert.AreEqual(400, payment.DealerPaymentAmount);
            Assert.AreEqual(200, payment.NonDealerPaymentAmount);
        }


        // ========================================
        // 3人麻雀（支払い人数のみ変化する）
        // ========================================
        [Test]
        public void Calculate_ThreePlayer_DealerTsumo_TwoPayers()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: true, isTsumo: true, honbaCount: 0, riichiStickCount: 0, playerCount: 3);

            Assert.AreEqual(2, payment.NonDealerPayerCount);
        }

        [Test]
        public void Calculate_ThreePlayer_NonDealerTsumo_OneOtherPayer()
        {
            var payment = PaymentCalculator.Calculate(
                basicPoints: 1000, isDealer: false, isTsumo: true, honbaCount: 0, riichiStickCount: 0, playerCount: 3);

            Assert.AreEqual(1, payment.NonDealerPayerCount);
        }
    }
}
