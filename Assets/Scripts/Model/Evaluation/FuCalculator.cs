using System.Linq;
using Mahjong.Model.Evaluation.Internal;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 和了形の符を計算する
    /// </summary>
    public static class FuCalculator
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 七対子の固定符（切り上げなし）
        /// </summary>
        private const int CHIITOITSU_FU = 25;
        /// <summary>
        /// ピンフ・ツモの固定符（ツモ符を付けない）
        /// </summary>
        private const int PINFU_TSUMO_FU = 20;
        /// <summary>
        /// 喫い平和形のロンの符の下限（符無しロンは存在しない）
        /// </summary>
        private const int KUIPINFU_RON_FU_FLOOR = 30;
        /// <summary>
        /// 副底（基本符）
        /// </summary>
        private const int BASE_FU = 20;
        /// <summary>
        /// 門前・ツモ時に副底に加算する符
        /// </summary>
        private const int MENZEN_OR_TSUMO_BONUS_FU = 10;
        /// <summary>
        /// 門前ツモに加算するツモ符
        /// </summary>
        private const int TSUMO_FU = 2;
        /// <summary>
        /// 嵌張・辺張・単騎待ちの符
        /// </summary>
        private const int CLOSED_WAIT_FU = 2;
        /// <summary>
        /// 役牌雀頭の符
        /// </summary>
        private const int YAKUHAI_PAIR_FU = 2;
        /// <summary>
        /// 国士無双は符を使わない（役満固定点数）ためのセンチネル値
        /// </summary>
        private const int KOKUSHI_FU_SENTINEL = 0;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 和了形の符を計算する
        /// </summary>
        /// <param name="decomposition">評価対象の分解パターン</param>
        /// <param name="context">状況フラグ</param>
        /// <param name="isMenzen">門前かどうか（Hand.Melds から呼び出し元が判定する）</param>
        /// <param name="isPinfu">ピンフが成立しているかどうか（YakuEvaluator の判定結果を渡す。ここで再判定しない）</param>
        /// <returns>10点単位に切り上げた符</returns>
        public static int Calculate(HandDecomposition decomposition, WinContext context, bool isMenzen, bool isPinfu)
        {
            if (decomposition.Form == WinningForm.Kokushi)
            {
                return KOKUSHI_FU_SENTINEL;
            }

            if (decomposition.Form == WinningForm.Chiitoitsu)
            {
                return CHIITOITSU_FU;
            }

            if (context.IsTsumo && isPinfu)
            {
                return PINFU_TSUMO_FU;
            }

            var fu = BASE_FU;
            fu += (context.IsTsumo || isMenzen) ? MENZEN_OR_TSUMO_BONUS_FU : 0;
            fu += (context.IsTsumo && isMenzen) ? TSUMO_FU : 0;
            fu += WaitFu(decomposition.WaitType);

            foreach (var group in decomposition.Groups)
            {
                if (group.Type == GroupType.Pair)
                {
                    continue;
                }

                fu += MeldFu(group);
            }

            var pairGroup = decomposition.Groups.FirstOrDefault(g => g.Type == GroupType.Pair);

            if (pairGroup != null && TileClassification.IsYakuhaiTile(pairGroup.Tiles[0], context))
            {
                fu += YAKUHAI_PAIR_FU;
            }

            // 喫い平和形（副露ロンで他に加符が一切無い形）は符無しロンとして扱わず30符に繰り上げる
            if (fu == BASE_FU)
            {
                fu = KUIPINFU_RON_FU_FLOOR;
            }

            return RoundUpToTen(fu);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 待ちの形に応じた符を返す
        /// </summary>
        private static int WaitFu(WaitType waitType)
        {
            return waitType is WaitType.Kanchan or WaitType.Penchan or WaitType.Tanki ? CLOSED_WAIT_FU : 0;
        }
        /// <summary>
        /// 面子1つ分の符を返す（中張牌の符、么九牌は2倍）
        /// </summary>
        private static int MeldFu(HandGroup group)
        {
            var tier = (group.Type, group.IsConcealed) switch
            {
                (GroupType.Triplet, false) => 2,  // 明刻
                (GroupType.Triplet, true) => 4,   // 暗刻
                (GroupType.Quad, false) => 8,     // 明槓
                (GroupType.Quad, true) => 16,     // 暗槓
                _ => 0,                           // 順子
            };

            return group.Tiles[0].IsYaochu ? tier * 2 : tier;
        }
        /// <summary>
        /// 符を10点単位に切り上げる
        /// </summary>
        private static int RoundUpToTen(int fu)
        {
            return (fu + 9) / 10 * 10;
        }
    }
}
