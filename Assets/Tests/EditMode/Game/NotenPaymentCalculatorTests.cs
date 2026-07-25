using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mahjong.Model.Game.Tests
{
    [TestFixture]
    public class NotenPaymentCalculatorTests
    {
        [Test]
        public void Calculate_AllTenpai_ReturnsNoChange()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, true, true, true }, 4);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, deltas);
        }

        [Test]
        public void Calculate_AllNoten_ReturnsNoChange()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { false, false, false, false }, 4);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, deltas);
        }

        [Test]
        public void Calculate_OneTenpaiFourPlayer_GetsThreeThousandFromOthers()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, false, false, false }, 4);
            CollectionAssert.AreEqual(new[] { 3000, -1000, -1000, -1000 }, deltas);
        }

        [Test]
        public void Calculate_TwoTenpaiFourPlayer_SplitsEvenly()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, true, false, false }, 4);
            CollectionAssert.AreEqual(new[] { 1500, 1500, -1500, -1500 }, deltas);
        }

        [Test]
        public void Calculate_ThreeTenpaiFourPlayer_OneNotenPaysThreeThousand()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, true, true, false }, 4);
            CollectionAssert.AreEqual(new[] { 1000, 1000, 1000, -3000 }, deltas);
        }

        [Test]
        public void Calculate_OneTenpaiThreePlayer_GetsTwoThousandFromOthers()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, false, false }, 3);
            CollectionAssert.AreEqual(new[] { 2000, -1000, -1000 }, deltas);
        }

        [Test]
        public void Calculate_TwoTenpaiThreePlayer_OneNotenPaysTwoThousand()
        {
            var deltas = NotenPaymentCalculator.Calculate(new List<bool> { true, true, false }, 3);
            CollectionAssert.AreEqual(new[] { 1000, 1000, -2000 }, deltas);
        }

        [Test]
        public void Calculate_MismatchedCount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                NotenPaymentCalculator.Calculate(new List<bool> { true, false }, 4));
        }

        [Test]
        public void Calculate_NullIsTenpai_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => NotenPaymentCalculator.Calculate(null, 4));
        }
    }
}
