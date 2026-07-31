using System.Collections.Generic;

namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する和了1件分の表示用データ
    /// Model型は一切含まず、席・放銃者などはすべて文字列に整形済み
    /// </summary>
    public sealed class WinResultView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 和了したプレイヤーの席を表す表示文字列（例: "東家（親）"）
        /// </summary>
        public string SeatLabel { get; }
        /// <summary>
        /// 和了の種類を表す表示文字列（例: "ツモ", "ロン（放銃: 西家）"）
        /// </summary>
        public string SourceLabel { get; }
        /// <summary>
        /// 役満が成立しているかどうか
        /// 役満の場合、YakuLines の Han は翻数ではなく役満の倍率を表す
        /// </summary>
        public bool IsYakuman { get; }
        /// <summary>
        /// 成立した役（役名＋翻数）
        /// </summary>
        public IReadOnlyList<YakuLineView> YakuLines { get; }
        /// <summary>
        /// 表ドラ・裏ドラ・北抜きによる翻数の合計
        /// </summary>
        public int DoraHan { get; }
        /// <summary>
        /// 赤ドラによる翻数
        /// </summary>
        public int AkaDoraHan { get; }
        /// <summary>
        /// 符（役満の場合は意味を持たない）
        /// </summary>
        public int Fu { get; }
        /// <summary>
        /// 翻数
        /// </summary>
        public int Han { get; }
        /// <summary>
        /// 満貫以上の区分の表示名（満貫未満は空文字）
        /// </summary>
        public string LimitBandLabel { get; }
        /// <summary>
        /// 点数を表す表示文字列（例: "親ロン 11600点", "子ツモ 2000/3900点"）
        /// </summary>
        public string PointsLabel { get; }
        /// <summary>
        /// 和了時の手牌（門前牌＋和了牌）
        /// </summary>
        public IReadOnlyList<TileView> HandTiles { get; }
        /// <summary>
        /// 副露（無ければ空）
        /// </summary>
        public IReadOnlyList<MeldView> Melds { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 和了1件分の表示用データを生成する
        /// </summary>
        internal WinResultView(
            string seatLabel,
            string sourceLabel,
            bool isYakuman,
            IReadOnlyList<YakuLineView> yakuLines,
            int doraHan,
            int akaDoraHan,
            int fu,
            int han,
            string limitBandLabel,
            string pointsLabel,
            IReadOnlyList<TileView> handTiles,
            IReadOnlyList<MeldView> melds)
        {
            SeatLabel = seatLabel;
            SourceLabel = sourceLabel;
            IsYakuman = isYakuman;
            YakuLines = yakuLines;
            DoraHan = doraHan;
            AkaDoraHan = akaDoraHan;
            Fu = fu;
            Han = han;
            LimitBandLabel = limitBandLabel;
            PointsLabel = pointsLabel;
            HandTiles = handTiles;
            Melds = melds;
        }
    }
}
