using NUnit.Framework;

namespace Mahjong.Model.Scoring.Tests
{
    [TestFixture]
    public class PointTableTests
    {
        // ========================================
        // 1〜4翻：符×2^(翻+2) の素点（仕様書8.2表の子ロン値を4で割った値と一致することを確認済み）
        // ========================================
        [TestCase(1, 20, 160)]
        [TestCase(1, 30, 240)]
        [TestCase(2, 30, 480)]
        [TestCase(3, 30, 960)]
        [TestCase(4, 20, 1280)]
        [TestCase(4, 30, 1920)]
        [TestCase(3, 60, 1920)]
        public void Calculate_BelowManganCap_ReturnsRawFormula(int han, int fu, int expectedBasicPoints)
        {
            var result = PointTable.Calculate(fu, han, isYakuman: false, yakumanMultiplier: 1);

            Assert.AreEqual(expectedBasicPoints, result.BasicPoints);
            Assert.AreEqual(LimitBand.None, result.Band);
        }

        // ========================================
        // 満貫キャップ境界（符×2^(翻+2) が2000を超える場合は2000に切り詰める）
        // ========================================
        [TestCase(4, 40)]  // 40*64=2560 -> capped
        [TestCase(3, 70)]  // 70*32=2240 -> capped
        public void Calculate_ExceedsManganCap_ClampsTo2000(int han, int fu)
        {
            var result = PointTable.Calculate(fu, han, isYakuman: false, yakumanMultiplier: 1);

            Assert.AreEqual(2000, result.BasicPoints);
            Assert.AreEqual(LimitBand.Mangan, result.Band);
        }

        // ========================================
        // 5翻以上の固定基本点
        // ========================================
        [TestCase(5, LimitBand.Mangan, 2000)]
        [TestCase(6, LimitBand.Haneman, 3000)]
        [TestCase(7, LimitBand.Haneman, 3000)]
        [TestCase(8, LimitBand.Baiman, 4000)]
        [TestCase(10, LimitBand.Baiman, 4000)]
        [TestCase(11, LimitBand.Sanbaiman, 6000)]
        [TestCase(12, LimitBand.Sanbaiman, 6000)]
        [TestCase(13, LimitBand.Yakuman, 8000)]
        [TestCase(20, LimitBand.Yakuman, 8000)]
        public void Calculate_FiveHanOrMore_ReturnsFixedBasicPoints(int han, LimitBand expectedBand, int expectedBasicPoints)
        {
            // fu はこの範囲では無視されるため、あえて符無しの値(0)を渡しても結果が変わらないことを確認する
            var result = PointTable.Calculate(fu: 0, han, isYakuman: false, yakumanMultiplier: 1);

            Assert.AreEqual(expectedBasicPoints, result.BasicPoints);
            Assert.AreEqual(expectedBand, result.Band);
        }

        // ========================================
        // 役満（符・翻数を無視し、8000×倍率で扱う）
        // ========================================
        [Test]
        public void Calculate_Yakuman_IgnoresFuAndHan()
        {
            var result = PointTable.Calculate(fu: 20, han: 1, isYakuman: true, yakumanMultiplier: 1);

            Assert.AreEqual(8000, result.BasicPoints);
            Assert.AreEqual(LimitBand.Yakuman, result.Band);
        }

        [Test]
        public void Calculate_DoubleYakumanMultiplier_DoublesBasicPoints()
        {
            var result = PointTable.Calculate(fu: 0, han: 0, isYakuman: true, yakumanMultiplier: 2);

            Assert.AreEqual(16000, result.BasicPoints);
            Assert.AreEqual(LimitBand.Yakuman, result.Band);
        }
    }
}
