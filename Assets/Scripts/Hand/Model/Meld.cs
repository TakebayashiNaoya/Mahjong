using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 副露1組のデータ
/// チー・ポン・カンの面子情報を保持する
/// </summary>
public class Meld
{
    // ========================================
    // プロパティ
    // ========================================
    /// <summary>
    /// 副露の種類
    /// </summary>
    public MeldType Type { get; private set; }
    /// <summary>
    /// 副露を構成する牌（3〜4枚）
    /// </summary>
    public IReadOnlyList<Tile> Tiles { get; private set; }
    /// <summary>
    /// 他家から鳴いた牌
    /// 暗槓の場合は null
    /// </summary>
    public Tile StolenTile { get; }
    /// <summary>
    /// 鳴いた方向（どのプレイヤーから鳴いたか）
    /// 暗槓の場合は null
    /// </summary>
    public Wind? FromWind { get; }
    /// <summary>
    /// 公開副露かどうか
    /// 暗槓は false、それ以外は true
    /// </summary>
    public bool IsOpen => Type != MeldType.AnKan;


    // ========================================
    // コンストラクタ
    // ========================================
    /// <summary>
    /// 副露を生成する
    /// </summary>
    /// <param name="type">副露の種類</param>
    /// <param name="tiles">副露を構成する牌（3〜4枚）</param>
    /// <param name="stolenTile">他家から鳴いた牌（暗槓はnull）</param>
    /// <param name="fromWind">鳴いた方向（暗槓はnull）</param>
    public Meld(MeldType type, List<Tile> tiles, Tile stolenTile = null, Wind? fromWind = null)
    {
        Type = type;

        // 呼び出し元のリスト変更の影響を受けないようにコピーする
        Tiles = new List<Tile>(tiles).AsReadOnly();

        StolenTile = stolenTile;
        FromWind = fromWind;
    }


    // ========================================
    // パブリックメソッド
    // ========================================
    /// <summary>
    /// 加槓：ポン済みの刻子に1枚追加して槓子にする
    /// Type を KaKan に変更し、牌を4枚に更新する
    /// ポン済み（Type == Pon かつ Tiles が3枚）でない場合は false を返す
    /// 呼び出し元は戻り値を確認してから牌を手牌より除去すること
    /// </summary>
    /// <param name="tile">追加する牌</param>
    /// <returns>成功した場合は true</returns>
    public bool TryApplyKakan(Tile tile)
    {
        if (Type != MeldType.Pon || Tiles.Count != 3)
        {
            Debug.LogError($"TryApplyKakan はポン済みの面子（3枚）にのみ使用できます。Type={Type}, Count={Tiles.Count}");
            return false;
        }

        var newTiles = new List<Tile>(Tiles) { tile };
        Tiles = newTiles.AsReadOnly();
        Type = MeldType.KaKan;
        return true;
    }
    /// <summary>
    /// 副露の文字列表現を返す
    /// </summary>
    public override string ToString()
    {
        var tilesStr = string.Join(", ", Tiles);
        return $"[{Type}: {tilesStr}]";
    }
}