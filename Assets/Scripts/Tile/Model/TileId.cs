/// <summary>
/// 牌の識別子
/// 三人麻雀では Manzu2〜Manzu8 を除いた108枚を使用する
/// </summary>
public enum TileId
{
    // =================
    // 萬子（まんず）
    // =================
    Manzu1,
    Manzu2,
    Manzu3,
    Manzu4,
    Manzu5,
    Manzu5Red,  // 赤ドラ（5萬）
    Manzu6,
    Manzu7,
    Manzu8,
    Manzu9,

    // =================
    // 筒子（ぴんず）
    // =================
    Pinzu1,
    Pinzu2,
    Pinzu3,
    Pinzu4,
    Pinzu5,
    Pinzu5Red,  // 赤ドラ（5筒）
    Pinzu6,
    Pinzu7,
    Pinzu8,
    Pinzu9,

    // =================
    // 索子（そうず）
    // =================
    Souzu1,
    Souzu2,
    Souzu3,
    Souzu4,
    Souzu5,
    Souzu5Red,  // 赤ドラ（5索）
    Souzu6,
    Souzu7,
    Souzu8,
    Souzu9,

    // =================
    // 字牌（じはい）
    // =================
    East,   // 東（とん）
    South,  // 南（なん）
    West,   // 西（しゃー）
    North,  // 北（ぺー）
    Haku,   // 白（はく）
    Hatsu,  // 發（はつ）
    Chun,   // 中（ちゅん）
}