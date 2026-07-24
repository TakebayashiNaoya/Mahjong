namespace Mahjong.Model.Scoring
{
    /// <summary>
    /// 和了時の支払い内訳
    /// 座席までは持たず、役割（放銃者・親・子）ベースで表現する
    /// 座席への割り当ては将来のGame進行層の責務とする
    /// </summary>
    public sealed class PaymentBreakdown
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// ツモ和了かどうか
        /// </summary>
        public bool IsTsumo { get; }
        /// <summary>
        /// 放銃者の支払い額（ロンのみ。ツモ時は0）
        /// </summary>
        public int DiscarderAmount { get; }
        /// <summary>
        /// 親の支払い額（ツモかつ和了者が子の場合のみ使用。それ以外は0）
        /// </summary>
        public int DealerPaymentAmount { get; }
        /// <summary>
        /// 子（親以外）1人あたりの支払い額（ツモ時のみ使用）
        /// 和了者が親の場合は他家全員がこの額を支払う
        /// 和了者が子の場合は和了者以外の子がこの額を支払う
        /// </summary>
        public int NonDealerPaymentAmount { get; }
        /// <summary>
        /// NonDealerPaymentAmount を支払う人数
        /// </summary>
        public int NonDealerPayerCount { get; }
        /// <summary>
        /// リーチ棒（供託）による和了者の取り分（丸めなし）
        /// </summary>
        public int RiichiStickGain { get; }
        /// <summary>
        /// 和了者が実際に得る合計点
        /// </summary>
        public int TotalWinnerGain { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        private PaymentBreakdown(
            bool isTsumo, int discarderAmount, int dealerPaymentAmount,
            int nonDealerPaymentAmount, int nonDealerPayerCount, int riichiStickGain)
        {
            IsTsumo = isTsumo;
            DiscarderAmount = discarderAmount;
            DealerPaymentAmount = dealerPaymentAmount;
            NonDealerPaymentAmount = nonDealerPaymentAmount;
            NonDealerPayerCount = nonDealerPayerCount;
            RiichiStickGain = riichiStickGain;

            TotalWinnerGain = discarderAmount
                + dealerPaymentAmount
                + nonDealerPaymentAmount * nonDealerPayerCount
                + riichiStickGain;
        }


        // ========================================
        // パブリックメソッド（ファクトリ）
        // ========================================
        /// <summary>
        /// ロン和了の内訳を生成する
        /// </summary>
        public static PaymentBreakdown ForRon(int discarderAmount, int riichiStickGain)
        {
            return new PaymentBreakdown(
                isTsumo: false, discarderAmount, dealerPaymentAmount: 0,
                nonDealerPaymentAmount: 0, nonDealerPayerCount: 0, riichiStickGain);
        }
        /// <summary>
        /// 親のツモ和了の内訳を生成する（子全員が同額を支払う）
        /// </summary>
        public static PaymentBreakdown ForDealerTsumo(int perPlayerAmount, int payerCount, int riichiStickGain)
        {
            return new PaymentBreakdown(
                isTsumo: true, discarderAmount: 0, dealerPaymentAmount: 0,
                nonDealerPaymentAmount: perPlayerAmount, nonDealerPayerCount: payerCount, riichiStickGain);
        }
        /// <summary>
        /// 子のツモ和了の内訳を生成する（親と他の子で支払い額が異なる）
        /// </summary>
        public static PaymentBreakdown ForNonDealerTsumo(
            int dealerAmount, int otherAmount, int otherPayerCount, int riichiStickGain)
        {
            return new PaymentBreakdown(
                isTsumo: true, discarderAmount: 0, dealerPaymentAmount: dealerAmount,
                nonDealerPaymentAmount: otherAmount, nonDealerPayerCount: otherPayerCount, riichiStickGain);
        }
    }
}
