using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Game;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Cpu
{
    /// <summary>
    /// CPUの鳴き判断ロジック（仕様書10.2「和了に近づく場合のみ鳴く」）
    /// 1人のプレイヤーに絞った選択肢を受け取り、他家との優先順位裁定は Round.ResolveCalls に委ねる
    /// </summary>
    public static class CpuCallSelector
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 自分に提示された選択肢から、宣言する内容を選ぶ
        /// </summary>
        /// <param name="hand">このプレイヤーの手牌</param>
        /// <param name="options">このプレイヤー自身の選択肢のみ（他プレイヤー分は含めないこと）</param>
        /// <param name="difficulty">CPU強度</param>
        /// <returns>宣言する内容。見送る場合は null</returns>
        /// <exception cref="ArgumentNullException">hand・options が null の場合</exception>
        public static DeclaredCall ChooseCall(Hand hand, IReadOnlyList<CallOption> options, CpuDifficulty difficulty)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand), "hand が null です");
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "options が null です");
            }

            var ronOption = options.FirstOrDefault(o => o.Type == CallType.Ron);

            if (ronOption != null)
            {
                return new DeclaredCall(ronOption.PlayerIndex, CallType.Ron, Array.Empty<Tile>());
            }

            if (difficulty == CpuDifficulty.Easy || options.Count == 0)
            {
                return null;
            }

            var currentShanten = ShantenCalculator.Calculate(hand);
            var bestShanten = currentShanten;
            DeclaredCall best = null;

            // カン・ポン（役割上優先度が同じ）を優先し、有効な選択肢が無い場合のみチーを検討する
            foreach (var type in new[] { CallType.Kan, CallType.Pon, CallType.Chi })
            {
                foreach (var option in options.Where(o => o.Type == type))
                {
                    foreach (var candidate in option.Candidates)
                    {
                        var shantenAfterCall = CalculateShantenAfterMeld(hand, candidate);

                        if (shantenAfterCall < bestShanten)
                        {
                            bestShanten = shantenAfterCall;
                            best = new DeclaredCall(option.PlayerIndex, type, candidate);
                        }
                    }
                }

                if (best != null)
                {
                    break;
                }
            }

            return best;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 指定した牌を使って鳴いた場合のシャンテン数を試算する
        /// 鳴きは必ず七対子・国士無双を崩すため、標準形のみのカウント配列ベースで計算する
        /// </summary>
        private static int CalculateShantenAfterMeld(Hand hand, IReadOnlyList<Tile> usedTiles)
        {
            var counts = ShantenCalculator.BuildCounts(hand);

            foreach (var tile in usedTiles)
            {
                counts[TileKind.IndexOf(tile)]--;
            }

            return ShantenCalculator.CalculateStandardFromCounts(counts, hand.Melds.Count + 1);
        }
    }
}
