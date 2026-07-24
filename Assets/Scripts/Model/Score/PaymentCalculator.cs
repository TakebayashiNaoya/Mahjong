namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 基本点から実際の支払い額（本場・供託込み）を計算する
    /// 符・翻数・役の知識は持たず、PointTable が算出した基本点のみを受け取る純粋な計算
    /// </summary>
    public static class PaymentCalculator
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// ロン時、本場1本あたり放銃者に追加させる点数
        /// </summary>
        private const int HONBA_RON_UNIT = 300;
        /// <summary>
        /// ツモ時、本場1本あたり各員に追加させる点数
        /// </summary>
        private const int HONBA_TSUMO_UNIT = 100;
        /// <summary>
        /// リーチ棒1本の価値
        /// </summary>
        private const int RIICHI_STICK_VALUE = 1000;
        /// <summary>
        /// 子の和了（ロン）の基本点倍率
        /// </summary>
        private const int NON_DEALER_RON_MULTIPLIER = 4;
        /// <summary>
        /// 親の和了（ロン）の基本点倍率
        /// </summary>
        private const int DEALER_RON_MULTIPLIER = 6;
        /// <summary>
        /// 親のツモ和了で子1人あたりが支払う基本点倍率、および
        /// 子のツモ和了で親が支払う基本点倍率
        /// </summary>
        private const int TSUMO_DEALER_SHARE_MULTIPLIER = 2;
        /// <summary>
        /// 子のツモ和了で他の子1人あたりが支払う基本点倍率
        /// </summary>
        private const int TSUMO_NON_DEALER_SHARE_MULTIPLIER = 1;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 支払い内訳を計算する
        /// </summary>
        /// <param name="basicPoints">PointTable が算出した基本点</param>
        /// <param name="isDealer">和了者が親かどうか</param>
        /// <param name="isTsumo">ツモ和了かどうか</param>
        /// <param name="honbaCount">本場数</param>
        /// <param name="riichiStickCount">供託されているリーチ棒の本数</param>
        /// <param name="playerCount">参加人数（3人麻雀は3、4人麻雀は4）</param>
        /// <returns>支払い内訳</returns>
        public static PaymentBreakdown Calculate(
            int basicPoints, bool isDealer, bool isTsumo, int honbaCount, int riichiStickCount, int playerCount)
        {
            var riichiStickGain = riichiStickCount * RIICHI_STICK_VALUE;

            if (!isTsumo)
            {
                var multiplier = isDealer ? DEALER_RON_MULTIPLIER : NON_DEALER_RON_MULTIPLIER;
                var discarderAmount = RoundUpToHundred(basicPoints * multiplier) + honbaCount * HONBA_RON_UNIT;
                return PaymentBreakdown.ForRon(discarderAmount, riichiStickGain);
            }

            if (isDealer)
            {
                var perPlayerAmount = RoundUpToHundred(basicPoints * TSUMO_DEALER_SHARE_MULTIPLIER)
                    + honbaCount * HONBA_TSUMO_UNIT;
                return PaymentBreakdown.ForDealerTsumo(perPlayerAmount, playerCount - 1, riichiStickGain);
            }

            var dealerAmount = RoundUpToHundred(basicPoints * TSUMO_DEALER_SHARE_MULTIPLIER)
                + honbaCount * HONBA_TSUMO_UNIT;
            var otherAmount = RoundUpToHundred(basicPoints * TSUMO_NON_DEALER_SHARE_MULTIPLIER)
                + honbaCount * HONBA_TSUMO_UNIT;
            return PaymentBreakdown.ForNonDealerTsumo(dealerAmount, otherAmount, playerCount - 2, riichiStickGain);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 100点単位に切り上げる
        /// </summary>
        private static int RoundUpToHundred(int points)
        {
            return (points + 99) / 100 * 100;
        }
    }
}
