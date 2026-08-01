using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;
using NUnit.Framework;
using static Mahjong.Model.Evaluation.Tests.TestTiles;

namespace Mahjong.Model.Game.Tests
{
    /// <summary>
    /// カオス麻雀ルール（仕様書16章）のテスト
    /// 各操作のあとで手牌枚数の不変条件（13 - 副露数×3）が保たれることを中心に検証する
    /// この条件が崩れるとシャンテン計算・テンパイ判定が例外を出さずに誤った値を返すため
    /// </summary>
    [TestFixture]
    public class ChaosRoundTests
    {
        // ========================================
        // 有効・無効の切り替え
        // ========================================
        [Test]
        public void GetChaosDrawOptions_ChaosRulesDisabled_Throws()
        {
            var round = CreateRound(out _, useChaosRules: false, seed: 1);
            AdvanceToNextDraw(round);

            Assert.Throws<InvalidOperationException>(() => round.GetChaosDrawOptions());
        }

        [Test]
        public void ExecuteChaosDraw_ChaosRulesDisabled_Throws()
        {
            var round = CreateRound(out _, useChaosRules: false, seed: 2);
            AdvanceToNextDraw(round);

            Assert.Throws<InvalidOperationException>(() => round.ExecuteChaosDraw(ChaosDrawOption.FromWall()));
        }

        [Test]
        public void GetChaosDrawOptions_NeverTargetsOwnHandOrOpponentAsOwnMeld()
        {
            var round = CreateRound(out _, useChaosRules: true, seed: 3);
            AdvanceToNextDraw(round);

            var options = round.GetChaosDrawOptions();

            Assert.IsTrue(options.Any(o => o.Source == ChaosDrawSource.Wall));
            Assert.IsFalse(options.Any(o =>
                o.Source == ChaosDrawSource.OpponentHand && o.TargetPlayerIndex == round.CurrentPlayerIndex));
            Assert.IsFalse(options.Any(o =>
                o.Source == ChaosDrawSource.OpponentMeld && o.TargetPlayerIndex == round.CurrentPlayerIndex));
        }


        // ========================================
        // 河から取る
        // ========================================
        [Test]
        public void ExecuteChaosDraw_FromDiscardPile_MovesTileToHandAndKeepsFuritenHistory()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 4);
            AdvanceToNextDraw(round);

            var furitenHistoryCount = players[0].HandState.DiscardedTiles.Count;
            var option = round.GetChaosDrawOptions()
                .First(o => o.Source == ChaosDrawSource.DiscardPile && o.TargetPlayerIndex == 0);
            var expectedTile = players[0].Discards[option.TargetIndex];

            var drawnTile = round.ExecuteChaosDraw(option);

            Assert.AreSame(expectedTile, drawnTile);
            Assert.AreSame(expectedTile, players[1].Hand.DrawnTile);
            Assert.AreEqual(0, players[0].Discards.Count);

            // フリテン判定用の履歴は残す（仕様書16.8）
            Assert.AreEqual(furitenHistoryCount, players[0].HandState.DiscardedTiles.Count);

            Assert.AreEqual(TurnPhase.AwaitingDiscard, round.Phase);
            AssertHandSizesAreValid(round);
        }


        // ========================================
        // 他家の手牌から奪う
        // ========================================
        [Test]
        public void ExecuteChaosDraw_FromOpponentHand_ReplenishesVictimToKeepHandSize()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 5);
            AdvanceToNextDraw(round);

            var option = round.GetChaosDrawOptions()
                .First(o => o.Source == ChaosDrawSource.OpponentHand && o.TargetPlayerIndex == 2);
            var expectedTile = players[2].Hand.Tiles[option.TargetIndex];

            var drawnTile = round.ExecuteChaosDraw(option);

            Assert.AreSame(expectedTile, drawnTile);
            Assert.AreSame(expectedTile, players[1].Hand.DrawnTile);
            Assert.AreEqual(13, players[2].Hand.TileCount);
            Assert.IsFalse(players[2].Hand.Tiles.Any(t => ReferenceEquals(t, expectedTile)));
            AssertHandSizesAreValid(round);
        }

        [Test]
        public void ExecuteChaosDraw_FromOwnHand_Throws()
        {
            var round = CreateRound(out _, useChaosRules: true, seed: 6);
            AdvanceToNextDraw(round);

            var option = ChaosDrawOption.FromOpponentHand(round.CurrentPlayerIndex, 0);

            Assert.Throws<ArgumentException>(() => round.ExecuteChaosDraw(option));
        }


        // ========================================
        // 他家の副露から奪う
        // ========================================
        [Test]
        public void ExecuteChaosDraw_FromOpponentPonMeld_DissolvesMeldAndKeepsHandSize()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 7);
            GivePonMeld(players[2], () => P(2));
            AdvanceToNextDraw(round);

            var option = round.GetChaosDrawOptions()
                .First(o => o.Source == ChaosDrawSource.OpponentMeld && o.TargetPlayerIndex == 2);
            var expectedTile = players[2].Hand.Melds[option.TargetIndex].Tiles[option.MeldTileIndex];

            var drawnTile = round.ExecuteChaosDraw(option);

            Assert.AreSame(expectedTile, drawnTile);
            Assert.AreEqual(0, players[2].Hand.Melds.Count);

            // 副露の残り2枚が手牌に戻り、不足する1枚を山から補充する
            Assert.AreEqual(13, players[2].Hand.TileCount);
            AssertHandSizesAreValid(round);
        }

        [Test]
        public void ExecuteChaosDraw_FromOpponentKanMeld_NeedsNoReplenishment()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 8);
            GiveDaiMinKanMeld(players[2], () => P(3));
            AdvanceToNextDraw(round);

            var option = round.GetChaosDrawOptions()
                .First(o => o.Source == ChaosDrawSource.OpponentMeld && o.TargetPlayerIndex == 2);

            round.ExecuteChaosDraw(option);

            // カンは3枚戻るため枠と過不足なく釣り合い、補充は発生しない
            Assert.AreEqual(0, players[2].Hand.Melds.Count);
            Assert.AreEqual(13, players[2].Hand.TileCount);
            AssertHandSizesAreValid(round);
        }


        // ========================================
        // 自分の副露を戻す
        // ========================================
        [Test]
        public void ExecuteChaosDraw_FromOwnPonMeld_EndsTurnWithoutDiscard()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 9);
            GivePonMeld(players[1], () => P(2));
            AdvanceToNextDraw(round);

            Assert.AreEqual(1, round.CurrentPlayerIndex);

            var option = round.GetChaosDrawOptions().First(o => o.Source == ChaosDrawSource.OwnMeld);
            var drawnTile = round.ExecuteChaosDraw(option);

            // チー・ポンは3枚戻ってちょうど正規枚数になるため、打牌せずに手番が終わる（仕様書16.5）
            Assert.IsNull(drawnTile);
            Assert.AreEqual(TurnPhase.AwaitingDraw, round.Phase);
            Assert.AreEqual(2, round.CurrentPlayerIndex);
            Assert.AreEqual(0, players[1].Hand.Melds.Count);
            Assert.IsNull(players[1].Hand.DrawnTile);
            Assert.AreEqual(13, players[1].Hand.TileCount);
            AssertHandSizesAreValid(round);
        }

        [Test]
        public void ExecuteChaosDraw_FromOwnKanMeld_AllowsDiscardOnSameTurn()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 10);
            GiveDaiMinKanMeld(players[1], () => P(3));
            AdvanceToNextDraw(round);

            var option = round.GetChaosDrawOptions().First(o => o.Source == ChaosDrawSource.OwnMeld);
            var drawnTile = round.ExecuteChaosDraw(option);

            // カンは4枚戻って1枚多くなるため、そのまま打牌に進める（仕様書16.5）
            Assert.IsNotNull(drawnTile);
            Assert.AreEqual(TurnPhase.AwaitingDiscard, round.Phase);
            Assert.AreEqual(1, round.CurrentPlayerIndex);
            Assert.AreEqual(0, players[1].Hand.Melds.Count);
            Assert.AreEqual(14, players[1].Hand.TileCount);
            AssertHandSizesAreValid(round);

            round.Discard(drawnTile);
            Assert.AreEqual(TurnPhase.AwaitingReactions, round.Phase);
        }

        [Test]
        public void ExecuteChaosDraw_ReturningOneOfTwoMelds_KeepsRemainingMeldConsistent()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 11);
            GivePonMeld(players[1], () => P(2));
            GivePonMeld(players[1], () => P(6));
            AdvanceToNextDraw(round);

            Assert.AreEqual(2, players[1].Hand.Melds.Count);
            Assert.AreEqual(7, players[1].Hand.TileCount);

            round.ExecuteChaosDraw(round.GetChaosDrawOptions().First(o => o.Source == ChaosDrawSource.OwnMeld));

            // 1組戻すごとに手牌の枠が3枚ぶん開き、副露1組ぶんの牌がそこに収まる
            Assert.AreEqual(1, players[1].Hand.Melds.Count);
            Assert.AreEqual(10, players[1].Hand.TileCount);
            AssertHandSizesAreValid(round);
        }


        // ========================================
        // リーチの撤回
        // ========================================
        [Test]
        public void GetAvailableCalls_RiichiPlayerWithoutChaosRules_OffersNoPon()
        {
            var round = CreateRound(out var players, useChaosRules: false, seed: 12);
            SetUpPonnableDiscard(round, players);

            var options = round.GetAvailableCalls(P(2), 0);

            Assert.IsFalse(options.Any(o => o.PlayerIndex == 1 && o.Type == CallType.Pon));
        }

        [Test]
        public void ResolveCalls_RiichiPlayerPonsWithChaosRules_CancelsRiichiAndKeepsStick()
        {
            var round = CreateRound(out var players, useChaosRules: true, seed: 13, riichiStickCount: 1);
            SetUpPonnableDiscard(round, players);

            var options = round.GetAvailableCalls(P(2), 0);
            var pon = options.FirstOrDefault(o => o.PlayerIndex == 1 && o.Type == CallType.Pon);
            Assert.IsNotNull(pon);

            var result = round.ResolveCalls(new List<DeclaredCall>
            {
                new DeclaredCall(1, CallType.Pon, pon.Candidates[0]),
            });

            Assert.IsNull(result);
            Assert.IsFalse(players[1].HandState.IsRiichi);
            Assert.IsFalse(players[1].HandState.IppatsuAvailable);
            Assert.AreEqual(1, players[1].Hand.Melds.Count);

            // 供託した1000点は場に残る（仕様書16.6）
            Assert.AreEqual(1, round.RiichiStickCount);
        }


        // ========================================
        // テストヘルパー
        // ========================================
        /// <summary>
        /// 四人麻雀・親0の局を生成する
        /// </summary>
        private static Round CreateRound(
            out List<PlayerState> players, bool useChaosRules, int seed, int riichiStickCount = 0)
        {
            var settings = GameSettings.CreateDefault(
                playerCount: 4, GameLengthType.HalfGame, useRedDora: true, useKitaNuki: false, useChaosRules);

            players = Enumerable.Range(0, 4).Select(i => new PlayerState(i, settings.InitialScore)).ToList();

            return new Round(
                settings, players, Wind.East, roundNumber: 1, dealerIndex: 0, honbaCount: 0, riichiStickCount,
                new Random(seed));
        }
        /// <summary>
        /// 現在のプレイヤーに打牌させ、次のプレイヤーのツモ待ちまで進める
        /// </summary>
        private static void AdvanceToNextDraw(Round round)
        {
            var hand = round.Players[round.CurrentPlayerIndex].Hand;
            round.Discard(hand.DrawnTile ?? hand.Tiles[0]);
            round.ResolveCalls(Array.Empty<DeclaredCall>());
        }
        /// <summary>
        /// プレイヤーにポンの副露を1組持たせる
        /// 手牌の2枚を同種牌に差し替えてからポンさせ、AddMeld 後に1枚捨てて手番待ちの正規枚数に戻す
        /// 牌は参照で区別されるため、同じインスタンスを使い回さず1枚ずつ生成する
        /// </summary>
        private static void GivePonMeld(PlayerState player, Func<Tile> createTile)
        {
            ReplaceTiles(player, createTile, count: 2);
            player.Hand.AddMeld(new Meld(
                MeldType.Pon, new List<Tile> { createTile(), createTile(), createTile() }, createTile(), Wind.South));
            player.Hand.Discard(player.Hand.Tiles[0]);
        }
        /// <summary>
        /// プレイヤーに大明槓の副露を1組持たせる
        /// 大明槓は手牌から3枚が副露に移るため、打牌による調整は要らない
        /// </summary>
        private static void GiveDaiMinKanMeld(PlayerState player, Func<Tile> createTile)
        {
            ReplaceTiles(player, createTile, count: 3);
            player.Hand.AddMeld(new Meld(
                MeldType.DaiMinKan,
                new List<Tile> { createTile(), createTile(), createTile(), createTile() }, createTile(), Wind.South));
        }
        /// <summary>
        /// 手牌の先頭から指定枚数を、生成した牌に差し替える（枚数を変えずに構成だけ変える）
        /// </summary>
        private static void ReplaceTiles(PlayerState player, Func<Tile> createTile, int count)
        {
            for (var i = 0; i < count; i++)
            {
                player.Hand.TakeTileAt(0);
                player.Hand.AddTile(createTile());
            }
        }
        /// <summary>
        /// 親がP(2)を捨て、リーチ中のP1がポンできる状態を作る
        /// P1の手牌はテンパイしない形にして、ロンの選択肢がポンの検証に混ざらないようにする
        /// </summary>
        private static void SetUpPonnableDiscard(Round round, List<PlayerState> players)
        {
            players[1].HandState.DeclareRiichi(1);
            players[1].Hand.SetInitialTiles(new List<Tile>
            {
                P(2), P(2), M(2), M(3), M(5), P(5), P(7), S(2), S(4), S(6), S(8),
                Z(TileId.East), Z(TileId.West),
            });

            players[0].Hand.SetInitialTiles(new List<Tile>
            {
                P(2), M(1), M(1), M(1), P(1), P(1), P(1), S(1), S(1), S(1),
                Z(TileId.East), Z(TileId.East), Z(TileId.East),
            });

            round.Discard(P(2));
        }
        /// <summary>
        /// 全プレイヤーの手牌が正規の枚数（13 - 副露数×3、打牌待ちのみ+1）であることを検証する
        /// </summary>
        private static void AssertHandSizesAreValid(Round round)
        {
            for (var i = 0; i < round.Players.Count; i++)
            {
                var hand = round.Players[i].Hand;
                var expectedCount = 13 - hand.Melds.Count * 3;
                var isAwaitingDiscard = i == round.CurrentPlayerIndex && round.Phase == TurnPhase.AwaitingDiscard;

                Assert.AreEqual(
                    isAwaitingDiscard ? expectedCount + 1 : expectedCount, hand.TileCount,
                    $"P{i} の手牌枚数が不正です（副露{hand.Melds.Count}組）");
            }
        }
    }
}
