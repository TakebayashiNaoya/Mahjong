using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Hands.Tests
{
    [TestFixture]
    public class HandTests
    {
        [Test]
        public void Discard_NormalTileWhileSameTypeRedDoraIsDrawn_DiscardsTheSpecifiedInstance()
        {
            // 赤ドラは IsSameType では通常牌と同種として扱われるため、
            // 参照で指定した牌ではなく別の同種牌が捨てられるバグの再発防止テスト
            var normalFive = M(5);
            var hand = CreateHand(
                new List<Tile>
                {
                    M(1), M(2), M(3),
                    normalFive,
                    P(1), P(2), P(3),
                    S(1), S(2), S(3),
                    Z(TileId.Haku), Z(TileId.Haku), Z(TileId.Haku),
                });

            var redFive = M(5, isRed: true);
            hand.Draw(redFive);

            var discarded = hand.Discard(normalFive);

            Assert.AreSame(normalFive, discarded);
            Assert.IsFalse(discarded.IsRed);
            Assert.IsNull(hand.DrawnTile);
            Assert.IsTrue(hand.Tiles.Contains(redFive));
        }
    }
}
