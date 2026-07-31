using System;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Scoring;

namespace Mahjong.Presenter
{
    /// <summary>
    /// YakuId・LimitBand を和了・流局画面向けの日本語表示名に変換する
    /// </summary>
    internal static class YakuDisplayNames
    {
        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 役の日本語表示名を返す
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">未対応の YakuId の場合</exception>
        public static string GetName(YakuId id)
        {
            return id switch
            {
                YakuId.Riichi => "リーチ",
                YakuId.MenzenTsumo => "門前清自摸和",
                YakuId.Ippatsu => "一発",
                YakuId.RinshanKaihou => "嶺上開花",
                YakuId.HaiteiRaoyue => "海底摸月",
                YakuId.HouteiRaoyui => "河底撈魚",
                YakuId.Tenhou => "天和",
                YakuId.Chiihou => "地和",
                YakuId.Tanyao => "断幺九",
                YakuId.Honitsu => "混一色",
                YakuId.Chinitsu => "清一色",
                YakuId.Honroutou => "混老頭",
                YakuId.Tsuuiisou => "字一色",
                YakuId.Chinroutou => "清老頭",
                YakuId.Ryuuiisou => "緑一色",
                YakuId.ChuurenPoutou => "九蓮宝燈",
                YakuId.Chiitoitsu => "七対子",
                YakuId.KokushiMusou => "国士無双",
                YakuId.Pinfu => "平和",
                YakuId.Iipeikou => "一盃口",
                YakuId.Ryanpeikou => "二盃口",
                YakuId.Chanta => "混全帯幺九",
                YakuId.Junchantaiyao => "純全帯幺九",
                YakuId.Ittsuu => "一気通貫",
                YakuId.SanshokuDoujun => "三色同順",
                YakuId.SanshokuDoukou => "三色同刻",
                YakuId.Toitoi => "対々和",
                YakuId.Sanankou => "三暗刻",
                YakuId.Suuankou => "四暗刻",
                YakuId.Shousangen => "小三元",
                YakuId.Daisangen => "大三元",
                YakuId.Shousuushii => "小四喜",
                YakuId.Daisuushii => "大四喜",
                YakuId.Suukantsu => "四槓子",
                YakuId.YakuhaiHaku => "役牌（白）",
                YakuId.YakuhaiHatsu => "役牌（發）",
                YakuId.YakuhaiChun => "役牌（中）",
                YakuId.YakuhaiSeatWind => "役牌（自風）",
                YakuId.YakuhaiRoundWind => "役牌（場風）",
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, $"未対応の YakuId です: {id}"),
            };
        }
        /// <summary>
        /// 満貫以上の区分の日本語表示名を返す
        /// None（満貫未満）の場合は空文字を返す
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">未対応の LimitBand の場合</exception>
        public static string GetLimitBandLabel(LimitBand band)
        {
            return band switch
            {
                LimitBand.None => string.Empty,
                LimitBand.Mangan => "満貫",
                LimitBand.Haneman => "跳満",
                LimitBand.Baiman => "倍満",
                LimitBand.Sanbaiman => "三倍満",
                LimitBand.Yakuman => "役満",
                _ => throw new ArgumentOutOfRangeException(nameof(band), band, $"未対応の LimitBand です: {band}"),
            };
        }
    }
}
