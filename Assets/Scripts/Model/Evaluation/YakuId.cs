namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 役の識別子
    /// </summary>
    public enum YakuId
    {
        // カテゴリA: 状況のみで決まる役
        Riichi,
        MenzenTsumo,
        Ippatsu,
        RinshanKaihou,
        HaiteiRaoyue,
        HouteiRaoyui,
        Tenhou,
        Chiihou,

        // カテゴリB: 分解に依存しない全体判定
        Tanyao,
        Honitsu,
        Chinitsu,
        Honroutou,
        Tsuuiisou,
        Chinroutou,
        Ryuuiisou,
        ChuurenPoutou,
        Chiitoitsu,
        KokushiMusou,

        // カテゴリC: 分解ごとの判定
        Pinfu,
        Iipeikou,
        Ryanpeikou,
        Chanta,
        Junchantaiyao,
        Ittsuu,
        SanshokuDoujun,
        SanshokuDoukou,
        Toitoi,
        Sanankou,
        Suuankou,
        Shousangen,
        Daisangen,
        Shousuushii,
        Daisuushii,
        Suukantsu,
        YakuhaiHaku,
        YakuhaiHatsu,
        YakuhaiChun,
        YakuhaiSeatWind,
        YakuhaiRoundWind,
    }
}
