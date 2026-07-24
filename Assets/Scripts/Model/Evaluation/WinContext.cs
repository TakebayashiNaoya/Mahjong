using Mahjong.Model.Common;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 役判定に必要な、Hand / HandState からは導出できない状況フラグ
    /// 門前かどうかは Hand.Melds から導出できるため、ここには含めない
    /// ドラ・裏ドラは点数計算（Score モジュール）側の責務のため、ここでは扱わない
    /// </summary>
    public readonly struct WinContext
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// ツモ和了かどうか
        /// </summary>
        public bool IsTsumo { get; }
        /// <summary>
        /// リーチが宣言されているかどうか
        /// </summary>
        public bool IsRiichi { get; }
        /// <summary>
        /// 一発かどうか
        /// </summary>
        public bool IsIppatsu { get; }
        /// <summary>
        /// 海底摸月（最後のツモ牌での和了）かどうか
        /// </summary>
        public bool IsHaitei { get; }
        /// <summary>
        /// 河底撈魚（最後の捨て牌でのロン）かどうか
        /// </summary>
        public bool IsHoutei { get; }
        /// <summary>
        /// 嶺上開花（カンの嶺上牌でのツモ和了）かどうか
        /// </summary>
        public bool IsRinshan { get; }
        /// <summary>
        /// 槍槓（加槓した牌でのロン）かどうか
        /// 現在の仕様書には槍槓を得点対象の役として明記していないため、
        /// このフラグは今のところ役判定には使用しない（将来のルール拡張・ゲーム進行側の判定用に保持）
        /// </summary>
        public bool IsChankan { get; }
        /// <summary>
        /// 天和（親の配牌時和了）かどうか
        /// </summary>
        public bool IsTenhou { get; }
        /// <summary>
        /// 地和（子の第一ツモでの和了）かどうか
        /// </summary>
        public bool IsChiihou { get; }
        /// <summary>
        /// 自風
        /// </summary>
        public Wind SeatWind { get; }
        /// <summary>
        /// 場風
        /// </summary>
        public Wind RoundWind { get; }
        /// <summary>
        /// 親かどうか
        /// </summary>
        public bool IsDealer { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public WinContext(
            bool isTsumo,
            bool isRiichi,
            bool isIppatsu,
            bool isHaitei,
            bool isHoutei,
            bool isRinshan,
            bool isChankan,
            bool isTenhou,
            bool isChiihou,
            Wind seatWind,
            Wind roundWind,
            bool isDealer)
        {
            IsTsumo = isTsumo;
            IsRiichi = isRiichi;
            IsIppatsu = isIppatsu;
            IsHaitei = isHaitei;
            IsHoutei = isHoutei;
            IsRinshan = isRinshan;
            IsChankan = isChankan;
            IsTenhou = isTenhou;
            IsChiihou = isChiihou;
            SeatWind = seatWind;
            RoundWind = roundWind;
            IsDealer = isDealer;
        }
    }
}
