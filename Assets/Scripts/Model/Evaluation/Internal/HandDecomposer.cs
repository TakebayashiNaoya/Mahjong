using System;
using System.Collections.Generic;

namespace Mahjong.Model.Evaluation.Internal
{
    /// <summary>
    /// 種類インデックス（0〜33）ベースで成立した1グループを表す
    /// 順子は最小のインデックス、刻子・雀頭はそのインデックス自身を保持する
    /// </summary>
    internal readonly struct KindGroup
    {
        /// <summary>
        /// グループ種別
        /// </summary>
        public GroupType Type { get; }
        /// <summary>
        /// グループの起点となる種類インデックス
        /// 順子の場合は3枚のうち最小のインデックス
        /// </summary>
        public int KindIndex { get; }

        public KindGroup(GroupType type, int kindIndex)
        {
            Type = type;
            KindIndex = kindIndex;
        }
    }

    /// <summary>
    /// 34種のカウント配列を面子・雀頭・搭子に分解する共有ロジック
    /// シャンテン数計算（緩いモード）と和了判定（厳密モード）の両方で使用する
    /// </summary>
    internal static class HandDecomposer
    {
        // ========================================
        // パブリックメソッド（緩いモード：シャンテン数計算用）
        // ========================================
        /// <summary>
        /// 面子・搭子・雀頭の組み合わせを網羅的に探索し、
        /// 「面子数・ブロック数（搭子+雀頭）・雀頭候補の有無」の組を重複排除して返す
        /// 手牌を使い切らない解釈（浮き牌を残す解釈）も含めて全て列挙する
        /// </summary>
        /// <param name="counts">34種のカウント配列（呼び出し元は変更されない）</param>
        /// <returns>(melds, blocks, hasPair) の組の一覧</returns>
        public static List<(int Melds, int Blocks, bool HasPair)> EnumerateShapes(int[] counts)
        {
            var working = (int[])counts.Clone();
            var results = new List<(int, int, bool)>();
            SolveShapes(working, 0, 0, 0, false, results);
            return Dedup(results);
        }


        // ========================================
        // パブリックメソッド（厳密モード：和了判定用）
        // ========================================
        /// <summary>
        /// 手牌をすべて消費し、ちょうど requiredMelds 個の面子（順子・刻子）＋雀頭1つに
        /// 分解できるすべてのパターンを列挙する
        /// 搭子・浮き牌が残る解釈は含まない（和了形のみ）
        /// </summary>
        /// <param name="counts">34種のカウント配列（呼び出し元は変更されない）</param>
        /// <param name="requiredMelds">必要な面子数（4 - 既存副露数）</param>
        /// <returns>各パターンごとの KindGroup のリスト（面子 requiredMelds 個 + 雀頭1つ）</returns>
        public static List<List<KindGroup>> EnumerateExactDecompositions(int[] counts, int requiredMelds)
        {
            var working = (int[])counts.Clone();
            var results = new List<List<KindGroup>>();
            var path = new List<KindGroup>();
            SolveExact(working, 0, 0, requiredMelds, false, path, results);
            return results;
        }


        // ========================================
        // プライベートメソッド（緩いモード）
        // ========================================
        /// <summary>
        /// 緩いモードの再帰探索本体
        /// 各インデックスで「刻子・対子・順子・両面/辺張搭子・嵌張搭子・浮き牌」を試す
        /// 対子は雀頭候補にも搭子ブロックにもなり得るため、ここでは一律ブロックとして数え、
        /// 雀頭として使うかどうかの判定は呼び出し側（ShantenCalculator）の数式評価時に行う
        /// </summary>
        private static void SolveShapes(
            int[] counts, int index, int melds, int blocks, bool hasPair,
            List<(int, int, bool)> results)
        {
            if (index == TileKind.KIND_COUNT)
            {
                results.Add((melds, blocks, hasPair));
                return;
            }

            if (counts[index] == 0)
            {
                SolveShapes(counts, index + 1, melds, blocks, hasPair, results);
                return;
            }

            // 刻子
            if (counts[index] >= 3)
            {
                counts[index] -= 3;
                SolveShapes(counts, index, melds + 1, blocks, hasPair, results);
                counts[index] += 3;
            }

            // 対子（雀頭候補 or 搭子ブロックとして後で評価する）
            if (counts[index] >= 2)
            {
                counts[index] -= 2;
                SolveShapes(counts, index, melds, blocks + 1, true, results);
                counts[index] += 2;
            }

            // 順子
            if (CanFormSequence(index) && counts[index] >= 1 && counts[index + 1] >= 1 && counts[index + 2] >= 1)
            {
                counts[index]--; counts[index + 1]--; counts[index + 2]--;
                SolveShapes(counts, index, melds + 1, blocks, hasPair, results);
                counts[index]++; counts[index + 1]++; counts[index + 2]++;
            }

            // 両面・辺張搭子（隣接2枚）
            if (CanFormAdjacentTaatsu(index) && counts[index] >= 1 && counts[index + 1] >= 1)
            {
                counts[index]--; counts[index + 1]--;
                SolveShapes(counts, index, melds, blocks + 1, hasPair, results);
                counts[index]++; counts[index + 1]++;
            }

            // 嵌張搭子（1つ飛ばし2枚）
            if (CanFormKanchanTaatsu(index) && counts[index] >= 1 && counts[index + 2] >= 1)
            {
                counts[index]--; counts[index + 2]--;
                SolveShapes(counts, index, melds, blocks + 1, hasPair, results);
                counts[index]++; counts[index + 2]++;
            }

            // 浮き牌（1枚だけ消費して次に進む解釈）
            counts[index]--;
            SolveShapes(counts, index, melds, blocks, hasPair, results);
            counts[index]++;
        }
        /// <summary>
        /// (melds, blocks, hasPair) の組の重複を取り除く
        /// </summary>
        private static List<(int, int, bool)> Dedup(List<(int, int, bool)> source)
        {
            var seen = new HashSet<(int, int, bool)>();
            var result = new List<(int, int, bool)>();

            foreach (var item in source)
            {
                if (seen.Add(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }


        // ========================================
        // プライベートメソッド（厳密モード）
        // ========================================
        /// <summary>
        /// 厳密モードの再帰探索本体
        /// 手牌をすべて消費し、面子（順子・刻子）と雀頭1つのみで構成できる場合のみ結果に加える
        /// </summary>
        private static void SolveExact(
            int[] counts, int index, int melds, int requiredMelds, bool hasPair,
            List<KindGroup> path, List<List<KindGroup>> results)
        {
            if (index == TileKind.KIND_COUNT)
            {
                if (melds == requiredMelds && hasPair)
                {
                    results.Add(new List<KindGroup>(path));
                }

                return;
            }

            if (counts[index] == 0)
            {
                SolveExact(counts, index + 1, melds, requiredMelds, hasPair, path, results);
                return;
            }

            // 刻子
            if (counts[index] >= 3 && melds < requiredMelds)
            {
                counts[index] -= 3;
                path.Add(new KindGroup(GroupType.Triplet, index));
                SolveExact(counts, index, melds + 1, requiredMelds, hasPair, path, results);
                path.RemoveAt(path.Count - 1);
                counts[index] += 3;
            }

            // 順子
            if (CanFormSequence(index) && melds < requiredMelds
                && counts[index] >= 1 && counts[index + 1] >= 1 && counts[index + 2] >= 1)
            {
                counts[index]--; counts[index + 1]--; counts[index + 2]--;
                path.Add(new KindGroup(GroupType.Sequence, index));
                SolveExact(counts, index, melds + 1, requiredMelds, hasPair, path, results);
                path.RemoveAt(path.Count - 1);
                counts[index]++; counts[index + 1]++; counts[index + 2]++;
            }

            // 雀頭（まだ雀頭が確定していない場合のみ）
            if (counts[index] >= 2 && !hasPair)
            {
                counts[index] -= 2;
                path.Add(new KindGroup(GroupType.Pair, index));
                SolveExact(counts, index, melds, requiredMelds, true, path, results);
                path.RemoveAt(path.Count - 1);
                counts[index] += 2;
            }

            // ここまでのどの分岐にも当てはまらなければ、このインデックスの残り牌を
            // 消費しきれず手詰まりになるため、何も結果に加えずに探索を終える
        }


        // ========================================
        // プライベートメソッド（共通：スート境界判定）
        // ========================================
        /// <summary>
        /// index を起点に3枚連続（順子）が同一スート内に収まるかどうかを判定する
        /// 字牌スート（index >= JIHAI_OFFSET）では常に false
        /// </summary>
        private static bool CanFormSequence(int index)
        {
            if (index >= TileKind.JIHAI_OFFSET)
            {
                return false;
            }

            return index % 9 <= 6;
        }
        /// <summary>
        /// index, index+1 が同一スート内に収まるかどうかを判定する（隣接搭子用）
        /// </summary>
        private static bool CanFormAdjacentTaatsu(int index)
        {
            if (index >= TileKind.JIHAI_OFFSET)
            {
                return false;
            }

            return index % 9 <= 7;
        }
        /// <summary>
        /// index, index+2 が同一スート内に収まるかどうかを判定する（嵌張搭子用）
        /// </summary>
        private static bool CanFormKanchanTaatsu(int index)
        {
            if (index >= TileKind.JIHAI_OFFSET)
            {
                return false;
            }

            return index % 9 <= 6;
        }
    }
}
