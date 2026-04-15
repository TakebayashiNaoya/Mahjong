/// <summary>
/// 牌1枚のデータ
/// </summary>
public class Tile
{
    // =========================================
    // プロパティ
    // =========================================
    /// <summary>
    /// 牌の識別子
    /// </summary>
    public TileId Id { get; }
    /// <summary>
    /// 牌の種類（萬子・筒子・索子・字牌）
    /// </summary>
    public TileSuit Suit { get; }
    /// <summary>
    /// 牌の数字（1〜9）
    /// 字牌の場合は 0
    /// </summary>
    public int Number { get; }
    /// <summary>
    /// 赤ドラかどうか
    /// </summary>
    public bool IsRed { get; }
    /// <summary>
    /// 么九牌（ヤオチュウハイ）かどうか
    /// 1・9・字牌が該当する
    /// </summary>
    public bool IsYaochu => Number == 1 || Number == 9 || Suit == TileSuit.Jihai;
    /// <summary>
    /// 字牌かどうか
    /// </summary>
    public bool IsJihai => Suit == TileSuit.Jihai;
    /// <summary>
    /// 数牌かどうか
    /// </summary>
    public bool IsSuuhai => Suit != TileSuit.Jihai;
    /// <summary>
    /// 中張牌（チュンチャンハイ）かどうか
    /// 2〜8の数牌が該当する
    /// </summary>
    public bool IsChunchan => IsSuuhai && Number >= 2 && Number <= 8;


    // =========================================
    // コンストラクタ
    // =========================================
    /// <summary>
    /// 牌を生成する
    /// </summary>
    /// <param name="id">牌の識別子</param>
    /// <param name="suit">牌の種類</param>
    /// <param name="number">牌の数字（字牌は0）</param>
    /// <param name="isRed">赤ドラかどうか</param>
    public Tile(TileId id, TileSuit suit, int number, bool isRed = false)
    {
        Id = id;
        Suit = suit;
        Number = number;
        IsRed = isRed;
    }


    // =========================================
    // パブリックメソッド
    // =========================================
    /// <summary>
    /// 同じ種類の牌かどうかを判定する
    /// 赤ドラは通常の5と同じ牌として扱う
    /// </summary>
    public bool IsSameType(Tile other)
    {
        return Suit == other.Suit && Number == other.Number;
    }
    /// <summary>
    /// 牌の文字列表現を返す
    /// </summary>
    /// <returns>例: [赤5萬], [3筒], [東] など </returns>
    public override string ToString()
    {
        if (IsRed)
        {
            return $"[赤{Number}{SuitToString()}]";
        }

        if (Suit == TileSuit.Jihai)
        {
            return $"[{JihaiToString()}]";
        }

        return $"[{Number}{SuitToString()}]";
    }


    // =========================================
    // プライベートメソッド
    // =========================================
    /// <summary>
    /// 牌の種類を文字列に変換する
    /// </summary>
    /// <returns>例: "萬", "筒", "索", ""（字牌の場合） </returns>
    private string SuitToString()
    {
        return Suit switch
        {
            TileSuit.Manzu => "萬",
            TileSuit.Pinzu => "筒",
            TileSuit.Souzu => "索",
            TileSuit.Jihai => "",
            _ => "",
        };
    }
    /// <summary>
    /// 字牌の識別子を文字列に変換する
    /// </summary>
    /// <returns>例: "東", "南", "西", "北", "白", "發", "中" </returns>
    private string JihaiToString()
    {
        return Id switch
        {
            TileId.East => "東",
            TileId.South => "南",
            TileId.West => "西",
            TileId.North => "北",
            TileId.Haku => "白",
            TileId.Hatsu => "發",
            TileId.Chun => "中",
            _ => "",
        };
    }
}