using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 和了形に成立する役を判定する
    /// 符計算は FuCalculator、翻数から点数への変換は Score モジュールの責務で、
    /// ここでは成立した役の一覧・合計翻数・符（分解パターン比較用）までを返す
    /// </summary>
    public static class YakuEvaluator
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 和了形に成立する役を判定する
        /// AgariResult が持つ複数の分解パターンをそれぞれ評価し、
        /// 役満優先・次に符×翻数から算出した実際の点数が最大の分解パターンを採用する
        /// </summary>
        /// <param name="hand">判定対象の手牌</param>
        /// <param name="winningTile">和了牌（ロンの場合は手牌にまだ含まれていない牌）</param>
        /// <param name="agariResult">AgariChecker.CheckWin の結果</param>
        /// <param name="context">状況フラグ</param>
        /// <returns>役判定の結果</returns>
        /// <exception cref="ArgumentNullException">hand・winningTile・agariResult が null の場合</exception>
        public static YakuEvaluationResult Evaluate(
            Mahjong.Model.Hands.Hand hand, Tile winningTile, AgariResult agariResult, WinContext context)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            if (winningTile == null)
            {
                throw new ArgumentNullException(nameof(winningTile), "winningTile が null です");
            }

            if (agariResult == null)
            {
                throw new ArgumentNullException(nameof(agariResult), "agariResult が null です");
            }

            var isMenzen = hand.Melds.All(m => !m.IsOpen);
            var allTiles = hand.GetAllTiles();

            if (!context.IsTsumo)
            {
                // ロンの場合、和了牌はまだ手牌に含まれていない
                allTiles.Add(winningTile);
            }

            YakuEvaluationResult best = null;

            foreach (var decomposition in agariResult.Decompositions)
            {
                var yakuList = EvaluateDecomposition(allTiles, decomposition, context, isMenzen);
                var isYakuman = yakuList.Any(y => y.IsYakuman);
                var totalHan = isYakuman
                    ? yakuList.Sum(y => y.YakumanMultiplier)
                    : yakuList.Sum(y => y.Han);
                var isPinfu = yakuList.Any(y => y.Id == YakuId.Pinfu);
                var fu = FuCalculator.Calculate(decomposition, context, isMenzen, isPinfu);

                var candidate = new YakuEvaluationResult(yakuList, totalHan, isYakuman, decomposition, fu);

                if (best == null || IsBetter(candidate, best))
                {
                    best = candidate;
                }
            }

            return best ?? new YakuEvaluationResult(Array.Empty<YakuResult>(), 0, false, null, 0);
        }


        // ========================================
        // プライベートメソッド（分解パターン単位の評価）
        // ========================================
        /// <summary>
        /// 1つの分解パターンについて成立する役をすべて求める
        /// 役満が1つでも成立した場合、通常役は結果から除外する
        /// </summary>
        private static List<YakuResult> EvaluateDecomposition(
            List<Tile> allTiles, HandDecomposition decomposition, WinContext context, bool isMenzen)
        {
            var yaku = new List<YakuResult>();

            AddContextYaku(yaku, context, isMenzen);
            AddWholeHandYaku(yaku, allTiles, isMenzen);

            if (decomposition.Form == WinningForm.Standard)
            {
                AddStandardFormYaku(yaku, decomposition, context, isMenzen);
            }
            else if (decomposition.Form == WinningForm.Chiitoitsu)
            {
                yaku.Add(new YakuResult(YakuId.Chiitoitsu, 2));
            }
            else if (decomposition.Form == WinningForm.Kokushi)
            {
                yaku.Add(new YakuResult(YakuId.KokushiMusou, 0, isYakuman: true));
            }

            var yakumanEntries = yaku.Where(y => y.IsYakuman).ToList();
            return yakumanEntries.Count > 0 ? yakumanEntries : yaku;
        }
        /// <summary>
        /// 分解パターン間で優劣を比較する（役満優先、次に符×翻数から算出した実際の点数、最後に翻数）
        /// </summary>
        private static bool IsBetter(YakuEvaluationResult candidate, YakuEvaluationResult current)
        {
            if (candidate.IsYakuman != current.IsYakuman)
            {
                return candidate.IsYakuman;
            }

            if (candidate.IsYakuman)
            {
                // 役満同士は翻数（ダブル役満等の倍率合計）で比較する
                return candidate.TotalHan > current.TotalHan;
            }

            var candidatePoints = BasicPointFormula.Calculate(candidate.Fu, candidate.TotalHan);
            var currentPoints = BasicPointFormula.Calculate(current.Fu, current.TotalHan);

            if (candidatePoints != currentPoints)
            {
                return candidatePoints > currentPoints;
            }

            return candidate.TotalHan > current.TotalHan;
        }


        // ========================================
        // プライベートメソッド（カテゴリA: 状況のみで決まる役）
        // ========================================
        private static void AddContextYaku(List<YakuResult> yaku, WinContext context, bool isMenzen)
        {
            if (context.IsTenhou)
            {
                yaku.Add(new YakuResult(YakuId.Tenhou, 0, isYakuman: true));
                return;
            }

            if (context.IsChiihou)
            {
                yaku.Add(new YakuResult(YakuId.Chiihou, 0, isYakuman: true));
                return;
            }

            if (context.IsRiichi)
            {
                yaku.Add(new YakuResult(YakuId.Riichi, 1));
            }

            if (context.IsTsumo && isMenzen)
            {
                yaku.Add(new YakuResult(YakuId.MenzenTsumo, 1));
            }

            if (context.IsIppatsu)
            {
                yaku.Add(new YakuResult(YakuId.Ippatsu, 1));
            }

            if (context.IsRinshan)
            {
                yaku.Add(new YakuResult(YakuId.RinshanKaihou, 1));
            }

            if (context.IsHaitei)
            {
                yaku.Add(new YakuResult(YakuId.HaiteiRaoyue, 1));
            }

            if (context.IsHoutei)
            {
                yaku.Add(new YakuResult(YakuId.HouteiRaoyui, 1));
            }
        }


        // ========================================
        // プライベートメソッド（カテゴリB: 分解に依存しない全体判定）
        // ========================================
        private static void AddWholeHandYaku(List<YakuResult> yaku, List<Tile> allTiles, bool isMenzen)
        {
            if (allTiles.All(t => !t.IsYaochu))
            {
                yaku.Add(new YakuResult(YakuId.Tanyao, 1));
            }

            if (allTiles.All(t => t.IsYaochu))
            {
                yaku.Add(new YakuResult(YakuId.Honroutou, 2));
            }

            if (allTiles.All(t => t.IsJihai))
            {
                yaku.Add(new YakuResult(YakuId.Tsuuiisou, 0, isYakuman: true));
            }

            if (allTiles.All(t => t.IsSuuhai && (t.Number == 1 || t.Number == 9)))
            {
                yaku.Add(new YakuResult(YakuId.Chinroutou, 0, isYakuman: true));
            }

            if (allTiles.All(IsGreenTile))
            {
                yaku.Add(new YakuResult(YakuId.Ryuuiisou, 0, isYakuman: true));
            }

            var suuhaiSuits = allTiles.Where(t => t.IsSuuhai).Select(t => t.Suit).Distinct().ToList();
            var hasJihai = allTiles.Any(t => t.IsJihai);

            if (suuhaiSuits.Count == 1)
            {
                if (hasJihai)
                {
                    yaku.Add(new YakuResult(YakuId.Honitsu, isMenzen ? 3 : 2));
                }
                else
                {
                    yaku.Add(new YakuResult(YakuId.Chinitsu, isMenzen ? 6 : 5));

                    if (IsChuurenPoutou(allTiles))
                    {
                        yaku.Add(new YakuResult(YakuId.ChuurenPoutou, 0, isYakuman: true));
                    }
                }
            }
        }
        /// <summary>
        /// 緑一色に使える牌（索子2・3・4・6・8、發）かどうかを判定する
        /// </summary>
        private static bool IsGreenTile(Tile tile)
        {
            if (tile.Id == TileId.Hatsu)
            {
                return true;
            }

            return tile.Suit == TileSuit.Souzu
                && (tile.Number == 2 || tile.Number == 3 || tile.Number == 4 || tile.Number == 6 || tile.Number == 8);
        }
        /// <summary>
        /// 九蓮宝燈の形（同一色で 1112345678999 + 1枚）かどうかを判定する
        /// 呼び出し元で一色手であることを確認済みの前提
        /// </summary>
        private static bool IsChuurenPoutou(List<Tile> allTiles)
        {
            if (allTiles.Count != 14)
            {
                return false;
            }

            var counts = new int[9];

            foreach (var tile in allTiles)
            {
                counts[tile.Number - 1]++;
            }

            if (counts[0] < 3 || counts[8] < 3)
            {
                return false;
            }

            for (var number = 2; number <= 8; number++)
            {
                if (counts[number - 1] < 1)
                {
                    return false;
                }
            }

            return true;
        }


        // ========================================
        // プライベートメソッド（カテゴリC: 標準形の分解ごとの判定）
        // ========================================
        private static void AddStandardFormYaku(
            List<YakuResult> yaku, HandDecomposition decomposition, WinContext context, bool isMenzen)
        {
            var groups = decomposition.Groups;
            var pairGroup = groups.FirstOrDefault(g => g.Type == GroupType.Pair);
            var sequenceGroups = groups.Where(g => g.Type == GroupType.Sequence).ToList();
            var tripletLikeGroups = groups.Where(g => g.Type == GroupType.Triplet || g.Type == GroupType.Quad).ToList();

            AddPinfu(yaku, decomposition, groups, pairGroup, context, isMenzen);
            AddPeikou(yaku, sequenceGroups, isMenzen);
            AddSanshoku(yaku, sequenceGroups, tripletLikeGroups, isMenzen);
            AddIttsuu(yaku, sequenceGroups, isMenzen);
            AddToitoi(yaku, groups);
            AddAnkouYaku(yaku, tripletLikeGroups, isMenzen);
            AddDragonYaku(yaku, tripletLikeGroups, pairGroup);
            AddWindYaku(yaku, tripletLikeGroups, pairGroup, context);
            AddChantaYaku(yaku, groups, isMenzen);
            AddSuukantsu(yaku, groups);
        }

        private static void AddPinfu(
            List<YakuResult> yaku, HandDecomposition decomposition, IReadOnlyList<HandGroup> groups,
            HandGroup pairGroup, WinContext context, bool isMenzen)
        {
            if (!isMenzen || pairGroup == null)
            {
                return;
            }

            if (decomposition.WaitType != WaitType.Ryanmen)
            {
                return;
            }

            if (!groups.All(g => g.Type == GroupType.Sequence || g.Type == GroupType.Pair))
            {
                return;
            }

            if (TileClassification.IsYakuhaiTile(pairGroup.Tiles[0], context))
            {
                return;
            }

            yaku.Add(new YakuResult(YakuId.Pinfu, 1));
        }

        private static void AddPeikou(List<YakuResult> yaku, List<HandGroup> sequenceGroups, bool isMenzen)
        {
            if (!isMenzen)
            {
                return;
            }

            var peikouPairs = sequenceGroups
                .GroupBy(g => (g.Tiles[0].Suit, g.Tiles.Min(t => t.Number)))
                .Sum(g => g.Count() / 2);

            if (peikouPairs >= 2)
            {
                yaku.Add(new YakuResult(YakuId.Ryanpeikou, 3));
            }
            else if (peikouPairs == 1)
            {
                yaku.Add(new YakuResult(YakuId.Iipeikou, 1));
            }
        }

        private static void AddSanshoku(
            List<YakuResult> yaku, List<HandGroup> sequenceGroups, List<HandGroup> tripletLikeGroups, bool isMenzen)
        {
            var hasSanshokuDoujun = sequenceGroups
                .GroupBy(g => g.Tiles.Min(t => t.Number))
                .Any(g => new HashSet<TileSuit> { TileSuit.Manzu, TileSuit.Pinzu, TileSuit.Souzu }
                    .IsSubsetOf(g.Select(x => x.Tiles[0].Suit)));

            if (hasSanshokuDoujun)
            {
                yaku.Add(new YakuResult(YakuId.SanshokuDoujun, isMenzen ? 2 : 1));
            }

            var hasSanshokuDoukou = tripletLikeGroups
                .GroupBy(g => g.Tiles[0].Number)
                .Any(g => new HashSet<TileSuit> { TileSuit.Manzu, TileSuit.Pinzu, TileSuit.Souzu }
                    .IsSubsetOf(g.Select(x => x.Tiles[0].Suit)));

            if (hasSanshokuDoukou)
            {
                yaku.Add(new YakuResult(YakuId.SanshokuDoukou, 2));
            }
        }

        private static void AddIttsuu(List<YakuResult> yaku, List<HandGroup> sequenceGroups, bool isMenzen)
        {
            foreach (var suit in new[] { TileSuit.Manzu, TileSuit.Pinzu, TileSuit.Souzu })
            {
                var startNumbers = sequenceGroups
                    .Where(g => g.Tiles[0].Suit == suit)
                    .Select(g => g.Tiles.Min(t => t.Number))
                    .ToHashSet();

                if (startNumbers.Contains(1) && startNumbers.Contains(4) && startNumbers.Contains(7))
                {
                    yaku.Add(new YakuResult(YakuId.Ittsuu, isMenzen ? 2 : 1));
                    return;
                }
            }
        }

        private static void AddToitoi(List<YakuResult> yaku, IReadOnlyList<HandGroup> groups)
        {
            var meldGroups = groups.Where(g => g.Type != GroupType.Pair).ToList();

            if (meldGroups.Count > 0 && meldGroups.All(g => g.Type == GroupType.Triplet || g.Type == GroupType.Quad))
            {
                yaku.Add(new YakuResult(YakuId.Toitoi, 2));
            }
        }

        private static void AddAnkouYaku(List<YakuResult> yaku, List<HandGroup> tripletLikeGroups, bool isMenzen)
        {
            var concealedCount = tripletLikeGroups.Count(g => g.IsConcealed);

            if (concealedCount >= 4 && isMenzen)
            {
                yaku.Add(new YakuResult(YakuId.Suuankou, 0, isYakuman: true));
            }
            else if (concealedCount >= 3)
            {
                yaku.Add(new YakuResult(YakuId.Sanankou, 2));
            }
        }

        private static void AddDragonYaku(List<YakuResult> yaku, List<HandGroup> tripletLikeGroups, HandGroup pairGroup)
        {
            var dragonKinds = tripletLikeGroups
                .Where(g => TileClassification.IsDragon(g.Tiles[0]))
                .Select(g => g.Tiles[0].Id)
                .Distinct()
                .ToList();

            foreach (var dragonId in dragonKinds)
            {
                yaku.Add(new YakuResult(DragonYakuId(dragonId), 1));
            }

            var pairIsDragon = pairGroup != null && TileClassification.IsDragon(pairGroup.Tiles[0]);

            if (dragonKinds.Count == 3)
            {
                yaku.Add(new YakuResult(YakuId.Daisangen, 0, isYakuman: true));
            }
            else if (dragonKinds.Count == 2 && pairIsDragon)
            {
                yaku.Add(new YakuResult(YakuId.Shousangen, 2));
            }
        }

        private static void AddWindYaku(
            List<YakuResult> yaku, List<HandGroup> tripletLikeGroups, HandGroup pairGroup, WinContext context)
        {
            var windGroups = tripletLikeGroups.Where(g => TileClassification.IsWind(g.Tiles[0])).ToList();
            var windKinds = windGroups.Select(g => g.Tiles[0].Id).Distinct().ToList();

            var seatWindId = TileClassification.WindToTileId(context.SeatWind);
            var roundWindId = TileClassification.WindToTileId(context.RoundWind);

            if (windGroups.Any(g => g.Tiles[0].Id == seatWindId))
            {
                yaku.Add(new YakuResult(YakuId.YakuhaiSeatWind, 1));
            }

            if (windGroups.Any(g => g.Tiles[0].Id == roundWindId))
            {
                yaku.Add(new YakuResult(YakuId.YakuhaiRoundWind, 1));
            }

            var pairIsWind = pairGroup != null && TileClassification.IsWind(pairGroup.Tiles[0]);

            if (windKinds.Count == 4)
            {
                yaku.Add(new YakuResult(YakuId.Daisuushii, 0, isYakuman: true));
            }
            else if (windKinds.Count == 3 && pairIsWind)
            {
                yaku.Add(new YakuResult(YakuId.Shousuushii, 0, isYakuman: true));
            }
        }

        private static void AddChantaYaku(List<YakuResult> yaku, IReadOnlyList<HandGroup> groups, bool isMenzen)
        {
            if (!groups.All(g => g.Tiles.Any(t => t.IsYaochu)))
            {
                return;
            }

            var hasHonor = groups.Any(g => g.Tiles.Any(t => t.IsJihai));

            yaku.Add(hasHonor
                ? new YakuResult(YakuId.Chanta, isMenzen ? 2 : 1)
                : new YakuResult(YakuId.Junchantaiyao, isMenzen ? 3 : 2));
        }

        private static void AddSuukantsu(List<YakuResult> yaku, IReadOnlyList<HandGroup> groups)
        {
            if (groups.Count(g => g.Type == GroupType.Quad) == 4)
            {
                yaku.Add(new YakuResult(YakuId.Suukantsu, 0, isYakuman: true));
            }
        }


        // ========================================
        // プライベートメソッド（共通ヘルパー）
        // ========================================
        private static YakuId DragonYakuId(TileId dragonId)
        {
            return dragonId switch
            {
                TileId.Haku => YakuId.YakuhaiHaku,
                TileId.Hatsu => YakuId.YakuhaiHatsu,
                TileId.Chun => YakuId.YakuhaiChun,
                _ => throw new ArgumentException($"三元牌ではない TileId です: {dragonId}", nameof(dragonId)),
            };
        }
    }
}
