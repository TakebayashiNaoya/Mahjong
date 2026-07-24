using System;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 符・翻数から基本点を計算する
    /// 符×2^(翻数+2) の式に加え、2000点キャップ（切り上げ満貫）と
    /// 5翻以上の固定基本点（満貫〜数え役満）を1つの関数に集約する
    /// YakuEvaluator の分解パターン比較と ScoreCalculator の点数テーブルの
    /// 両方から使う共有のソースオブトゥルース
    /// </summary>
    internal static class BasicPointFormula
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 満貫の基本点（子のロンで×4して8000点になる基準値）
        /// 1〜4翻の基本点はここで頭打ちになる（切り上げ満貫）
        /// </summary>
        private const int MANGAN_BASIC_POINT = 2000;
        /// <summary>
        /// 跳満の基本点
        /// </summary>
        private const int HANEMAN_BASIC_POINT = 3000;
        /// <summary>
        /// 倍満の基本点
        /// </summary>
        private const int BAIMAN_BASIC_POINT = 4000;
        /// <summary>
        /// 三倍満の基本点
        /// </summary>
        private const int SANBAIMAN_BASIC_POINT = 6000;
        /// <summary>
        /// 数え役満の基本点
        /// </summary>
        private const int KAZOE_YAKUMAN_BASIC_POINT = 8000;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 符・翻数から基本点を計算する
        /// 役満（YakuEvaluationResult.IsYakuman）の場合はこの関数を使わず、
        /// 呼び出し側で役満専用の計算を行うこと
        /// </summary>
        /// <param name="fu">符</param>
        /// <param name="han">翻数</param>
        /// <returns>基本点</returns>
        public static int Calculate(int fu, int han)
        {
            if (han >= 13)
            {
                return KAZOE_YAKUMAN_BASIC_POINT;
            }

            if (han >= 11)
            {
                return SANBAIMAN_BASIC_POINT;
            }

            if (han >= 8)
            {
                return BAIMAN_BASIC_POINT;
            }

            if (han >= 6)
            {
                return HANEMAN_BASIC_POINT;
            }

            if (han >= 5)
            {
                return MANGAN_BASIC_POINT;
            }

            return Math.Min(fu * (1 << (han + 2)), MANGAN_BASIC_POINT);
        }
    }
}
