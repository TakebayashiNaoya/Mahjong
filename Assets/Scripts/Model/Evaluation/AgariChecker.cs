using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 手牌が和了形かどうかを判定する
    /// 標準形（面子4つ＋雀頭）・七対子・国士無双のすべてに対応し、
    /// 標準形は複数解釈がありうる分解パターンをすべて列挙する
    /// </summary>
    public static class AgariChecker
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 面子の最大数
        /// </summary>
        private const int MAX_MELDS = 4;
        /// <summary>
        /// 和了時の合計枚数（門前の牌 + 副露牌の面子換算）
        /// </summary>
        private const int WIN_TILE_COUNT = 14;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 手牌が和了形として成立するかどうかを判定する
        /// </summary>
        /// <param name="hand">判定対象の手牌</param>
        /// <param name="winningTile">和了牌（ロンの場合は手牌にまだ含まれていない牌）</param>
        /// <param name="isTsumo">ツモ和了かどうか</param>
        /// <returns>和了判定の結果（成立する全ての分解パターンを含む）</returns>
        /// <exception cref="ArgumentNullException">hand または winningTile が null の場合</exception>
        public static AgariResult CheckWin(Hand hand, Tile winningTile, bool isTsumo)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            if (winningTile == null)
            {
                throw new ArgumentNullException(nameof(winningTile), "winningTile が null です");
            }

            // ロンの場合、和了牌はまだ手牌に含まれていないため明示的にマージする
            // Hand 自体は変更しない
            var closedTiles = hand.GetClosedTiles();

            if (!isTsumo)
            {
                closedTiles.Add(winningTile);
            }

            var decompositions = new List<HandDecomposition>();

            decompositions.AddRange(FindStandardDecompositions(hand, closedTiles, winningTile, isTsumo));

            // 七対子・国士無双は門前限定
            if (hand.Melds.Count == 0)
            {
                if (TryBuildChiitoitsuDecomposition(closedTiles, winningTile, out var chiitoitsu))
                {
                    decompositions.Add(chiitoitsu);
                }

                if (TryBuildKokushiDecomposition(closedTiles, out var kokushi))
                {
                    decompositions.Add(kokushi);
                }
            }

            return new AgariResult(decompositions.Count > 0, decompositions);
        }


        // ========================================
        // プライベートメソッド（標準形）
        // ========================================
        /// <summary>
        /// 標準形の分解パターンをすべて列挙する
        /// </summary>
        private static List<HandDecomposition> FindStandardDecompositions(
            Hand hand, List<Tile> closedTiles, Tile winningTile, bool isTsumo)
        {
            var results = new List<HandDecomposition>();
            var counts = new int[TileKind.KIND_COUNT];

            foreach (var tile in closedTiles)
            {
                counts[TileKind.IndexOf(tile)]++;
            }

            var requiredMelds = MAX_MELDS - hand.Melds.Count;
            var exactDecompositions = HandDecomposer.EnumerateExactDecompositions(counts, requiredMelds);

            var existingMeldGroups = hand.Melds.Select(ConvertMeldToGroup).ToList();

            foreach (var kindGroups in exactDecompositions)
            {
                var closedGroups = BuildClosedGroups(closedTiles, kindGroups, winningTile, isTsumo);
                var allGroups = new List<HandGroup>(existingMeldGroups);
                allGroups.AddRange(closedGroups);

                var winningGroup = allGroups.FirstOrDefault(g => g.ContainsWinningTile);
                var waitType = DetermineWaitType(winningGroup, winningTile);

                results.Add(new HandDecomposition(WinningForm.Standard, allGroups, winningGroup, waitType));
            }

            return results;
        }
        /// <summary>
        /// KindGroup（種類インデックスベース）を実際の Tile 参照を持つ HandGroup に変換する
        /// closedTiles から種類ごとの牌プールを構築し、消費しながら組み立てる
        /// </summary>
        private static List<HandGroup> BuildClosedGroups(
            List<Tile> closedTiles, List<KindGroup> kindGroups, Tile winningTile, bool isTsumo)
        {
            var pool = BuildPoolByKind(closedTiles);
            var groups = new List<HandGroup>();

            foreach (var kindGroup in kindGroups)
            {
                List<Tile> tiles;

                switch (kindGroup.Type)
                {
                    case GroupType.Sequence:
                        tiles = new List<Tile>
                        {
                            TakeFromPool(pool, kindGroup.KindIndex),
                            TakeFromPool(pool, kindGroup.KindIndex + 1),
                            TakeFromPool(pool, kindGroup.KindIndex + 2),
                        };
                        break;

                    case GroupType.Triplet:
                        tiles = new List<Tile>
                        {
                            TakeFromPool(pool, kindGroup.KindIndex),
                            TakeFromPool(pool, kindGroup.KindIndex),
                            TakeFromPool(pool, kindGroup.KindIndex),
                        };
                        break;

                    case GroupType.Pair:
                        tiles = new List<Tile>
                        {
                            TakeFromPool(pool, kindGroup.KindIndex),
                            TakeFromPool(pool, kindGroup.KindIndex),
                        };
                        break;

                    default:
                        throw new InvalidOperationException($"閉じた牌からは生成されないはずの GroupType です: {kindGroup.Type}");
                }

                var containsWinningTile = tiles.Any(t => ReferenceEquals(t, winningTile));

                // ロンで完成した刻子は暗刻として数えない
                var isConcealed = kindGroup.Type != GroupType.Triplet || isTsumo || !containsWinningTile;

                groups.Add(new HandGroup(kindGroup.Type, tiles, isConcealed, containsWinningTile));
            }

            return groups;
        }
        /// <summary>
        /// 待ちの形を判定する
        /// </summary>
        private static WaitType DetermineWaitType(HandGroup winningGroup, Tile winningTile)
        {
            if (winningGroup == null)
            {
                return WaitType.Tanki;
            }

            if (winningGroup.Type == GroupType.Pair)
            {
                return WaitType.Tanki;
            }

            if (winningGroup.Type == GroupType.Triplet)
            {
                // 刻子内に和了牌があるのは、既存の対子が和了牌によって刻子化した場合のみ
                // （雀頭は分解上ちょうど1つに限られるため、この刻子とは別に雀頭が存在する）
                return WaitType.Shanpon;
            }

            // 順子内の待ち判定
            var numbers = winningGroup.Tiles.Select(t => t.Number).OrderBy(n => n).ToList();
            var low = numbers[0];
            var mid = numbers[1];
            var high = numbers[2];

            if (winningTile.Number == mid)
            {
                return WaitType.Kanchan;
            }

            if (winningTile.Number == high && low == 1)
            {
                return WaitType.Penchan;
            }

            if (winningTile.Number == low && high == 9)
            {
                return WaitType.Penchan;
            }

            return WaitType.Ryanmen;
        }
        /// <summary>
        /// 既存の副露（Meld）を HandGroup に変換する
        /// </summary>
        private static HandGroup ConvertMeldToGroup(Meld meld)
        {
            var groupType = meld.Type switch
            {
                MeldType.Chi => GroupType.Sequence,
                MeldType.Pon => GroupType.Triplet,
                MeldType.DaiMinKan => GroupType.Quad,
                MeldType.AnKan => GroupType.Quad,
                MeldType.KaKan => GroupType.Quad,
                _ => throw new InvalidOperationException($"未対応の MeldType です: {meld.Type}"),
            };

            // 暗槓のみ非公開（Meld.IsOpen は AnKan 以外で true）
            var isConcealed = !meld.IsOpen;

            return new HandGroup(groupType, meld.Tiles, isConcealed, containsWinningTile: false);
        }


        // ========================================
        // プライベートメソッド（七対子）
        // ========================================
        private static bool TryBuildChiitoitsuDecomposition(List<Tile> closedTiles, Tile winningTile, out HandDecomposition decomposition)
        {
            decomposition = null;

            if (closedTiles.Count != WIN_TILE_COUNT)
            {
                return false;
            }

            var pool = BuildPoolByKind(closedTiles);

            // 同一種類4枚を2対子とは数えない（count が 2 以外の種類が1つでもあれば不成立）
            foreach (var tiles in pool.Values)
            {
                if (tiles.Count != 2)
                {
                    return false;
                }
            }

            if (pool.Count != 7)
            {
                return false;
            }

            var groups = new List<HandGroup>();

            foreach (var tiles in pool.Values)
            {
                var containsWinningTile = tiles.Any(t => ReferenceEquals(t, winningTile));
                groups.Add(new HandGroup(GroupType.Pair, tiles, isConcealed: true, containsWinningTile));
            }

            var winningGroup = groups.First(g => g.ContainsWinningTile);
            decomposition = new HandDecomposition(WinningForm.Chiitoitsu, groups, winningGroup, WaitType.Tanki);
            return true;
        }


        // ========================================
        // プライベートメソッド（国士無双）
        // ========================================
        private static bool TryBuildKokushiDecomposition(List<Tile> closedTiles, out HandDecomposition decomposition)
        {
            decomposition = null;

            if (closedTiles.Count != WIN_TILE_COUNT)
            {
                return false;
            }

            if (closedTiles.Any(t => !t.IsYaochu))
            {
                return false;
            }

            var pool = BuildPoolByKind(closedTiles);

            if (pool.Count != 13)
            {
                return false;
            }

            if (!pool.Values.Any(tiles => tiles.Count == 2))
            {
                return false;
            }

            // 国士無双は意味のあるサブグループを持たないため、Groups は空のフラット形式とする
            decomposition = new HandDecomposition(WinningForm.Kokushi, Array.Empty<HandGroup>(), winningGroup: null, WaitType.Tanki);
            return true;
        }


        // ========================================
        // プライベートメソッド（共通）
        // ========================================
        /// <summary>
        /// 牌のリストを種類インデックスごとのプールに変換する
        /// </summary>
        private static Dictionary<int, List<Tile>> BuildPoolByKind(List<Tile> tiles)
        {
            var pool = new Dictionary<int, List<Tile>>();

            foreach (var tile in tiles)
            {
                var kindIndex = TileKind.IndexOf(tile);

                if (!pool.TryGetValue(kindIndex, out var list))
                {
                    list = new List<Tile>();
                    pool[kindIndex] = list;
                }

                list.Add(tile);
            }

            return pool;
        }
        /// <summary>
        /// プールから指定した種類の牌を1枚取り出す
        /// </summary>
        private static Tile TakeFromPool(Dictionary<int, List<Tile>> pool, int kindIndex)
        {
            var list = pool[kindIndex];
            var tile = list[^1];
            list.RemoveAt(list.Count - 1);
            return tile;
        }
    }
}
