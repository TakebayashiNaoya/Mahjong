using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 副露種別ごとに以下の制約がある
    /// ・Chi / Pon          : stolenTile・fromWind は必須（非null）、枚数は3枚
    /// ・DaiMinKan          : stolenTile・fromWind は必須（非null）、枚数は4枚（手牌3枚 + 鳴いた1枚）
    /// ・AnKan              : stolenTile・fromWind は null 必須、枚数は4枚
    /// ・KaKan              : コンストラクタでの直接生成は不可。Hand.AddKakan を使用すること
    /// </summary>
    /// <param name="type">副露の種類</param>
    /// <param name="tiles">副露を構成する牌</param>
    /// <param name="stolenTile">他家から鳴いた牌（暗槓はnull）</param>
    /// <param name="fromWind">鳴いた方向（暗槓はnull）</param>
    /// <exception cref="ArgumentNullException">tiles が null の場合</exception>
    /// <exception cref="ArgumentException">副露種別ごとの制約を満たさない場合</exception>
    public Meld(MeldType type, List<Tile> tiles, Tile stolenTile = null, Wind? fromWind = null)
    {
        if (tiles == null)
        {
            throw new ArgumentNullException(nameof(tiles), "tiles が null です");
        }

        switch (type)
        {
            case MeldType.Chi:
            case MeldType.Pon:
                if (tiles.Count != 3)
                {
                    throw new ArgumentException($"{type} の枚数は3枚である必要があります: {tiles.Count}枚", nameof(tiles));
                }

                if (stolenTile == null)
                {
                    throw new ArgumentException($"{type} の stolenTile は null にできません", nameof(stolenTile));
                }

                if (fromWind == null)
                {
                    throw new ArgumentException($"{type} の fromWind は null にできません", nameof(fromWind));
                }

                break;

            case MeldType.DaiMinKan:
                // 大明槓は手牌3枚 + 鳴いた1枚 = 4枚
                if (tiles.Count != 4)
                {
                    throw new ArgumentException($"DaiMinKan の枚数は4枚である必要があります: {tiles.Count}枚", nameof(tiles));
                }

                if (stolenTile == null)
                {
                    throw new ArgumentException("DaiMinKan の stolenTile は null にできません", nameof(stolenTile));
                }

                if (fromWind == null)
                {
                    throw new ArgumentException("DaiMinKan の fromWind は null にできません", nameof(fromWind));
                }

                break;

            case MeldType.AnKan:
                if (tiles.Count != 4)
                {
                    throw new ArgumentException($"AnKan の枚数は4枚である必要があります: {tiles.Count}枚", nameof(tiles));
                }

                if (stolenTile != null)
                {
                    throw new ArgumentException("AnKan の stolenTile は null である必要があります", nameof(stolenTile));
                }

                if (fromWind != null)
                {
                    throw new ArgumentException("AnKan の fromWind は null である必要があります", nameof(fromWind));
                }

                break;

            case MeldType.KaKan:
                // KaKan は Hand.AddKakan から TryApplyKakan 経由で生成されるため
                // 直接コンストラクタで生成することは想定していない
                throw new ArgumentException("KaKan は Meld のコンストラクタで直接生成できません。Hand.AddKakan を使用してください", nameof(type));

            default:
                throw new ArgumentException($"未対応の MeldType です: {type}", nameof(type));
        }

        // StolenTile が tiles に含まれているか検証する
        if (stolenTile != null && !tiles.Any(t => t.IsSameType(stolenTile)))
        {
            throw new ArgumentException($"stolenTile が tiles に含まれていません: {stolenTile}", nameof(stolenTile));
        }

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
    /// 以下の条件を満たさない場合は false を返す
    /// ・tile が null でないこと
    /// ・Type == Pon かつ Tiles が3枚であること
    /// ・追加する牌が既存の牌と同じ種類であること
    /// 呼び出し元は戻り値を確認してから手牌の牌を除去すること
    /// </summary>
    /// <param name="tile">追加する牌</param>
    /// <returns>成功した場合は true</returns>
    public bool TryApplyKakan(Tile tile)
    {
        if (tile == null)
        {
            Debug.LogError("TryApplyKakan に渡された tile が null です");
            return false;
        }

        if (Type != MeldType.Pon || Tiles.Count != 3)
        {
            Debug.LogError($"TryApplyKakan はポン済みの面子（3枚）にのみ使用できます。Type={Type}, Count={Tiles.Count}");
            return false;
        }

        if (!Tiles[0].IsSameType(tile))
        {
            Debug.LogError($"加槓する牌の種類が一致しません: 面子={Tiles[0]}, 追加牌={tile}");
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