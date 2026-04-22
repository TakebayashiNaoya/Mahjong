using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 山牌を管理するクラス
/// 四人麻雀：136枚 / 三人麻雀：108枚（萬子2〜8を除く）
/// </summary>
public class TileDeck
{
    // ========================================
    // プロパティ
    // ========================================
    /// <summary>
    /// 残りのツモ可能枚数
    /// </summary>
    public int RemainingCount => _tiles.Count - DeadWallCount - _drawIndex;
    /// <summary>
    /// 山が尽きているかどうか
    /// </summary>
    public bool IsEmpty => RemainingCount <= 0;
    /// <summary>
    /// 王牌（ドラ表示牌・嶺上牌）
    /// </summary>
    public IReadOnlyList<Tile> DeadWall { get; private set; }


    // ========================================
    // 定数
    // ========================================
    /// <summary>
    /// 王牌（カンドラ用に残す牌）の枚数
    /// </summary>
    private const int DEAD_WALL_COUNT = 14;


    // ========================================
    // フィールド
    // ========================================
    /// <summary>
    /// 山牌のリスト（シャッフル済み）
    /// </summary>
    private readonly List<Tile> _tiles = new();
    /// <summary>
    /// 現在のツモ位置
    /// </summary>
    private int _drawIndex;


    // ========================================
    // コンストラクタ
    // ========================================
    /// <summary>
    /// 山牌を生成してシャッフルする
    /// </summary>
    /// <param name="isThreePlayer">三人麻雀かどうか</param>
    /// <param name="useRedDora">赤ドラを使用するかどうか</param>
    public TileDeck(bool isThreePlayer, bool useRedDora = true)
    {
        BuildDeck(isThreePlayer, useRedDora);
        Shuffle();
        SetupDeadWall();
    }


    // ========================================
    // パブリックメソッド
    // ========================================
    /// <summary>
    /// 山から1枚ツモる
    /// </summary>
    public Tile Draw()
    {
        if (IsEmpty)
        {
            Debug.LogError("山牌が尽きています");
            return null;
        }

        return _tiles[_drawIndex++];
    }
    /// <summary>
    /// 嶺上牌をツモる（カン時に使用）
    /// 王牌の末尾から取得する
    /// </summary>
    public Tile DrawRinshan()
    {
        // 王牌の末尾（嶺上牌）を取得する
        var rinshanIndex = _tiles.Count - DeadWallCount;
        var tile = _tiles[rinshanIndex];

        // 使用済みの嶺上牌をリストから除外する
        _tiles.RemoveAt(rinshanIndex);

        // 王牌を再設定する
        SetupDeadWall();

        return tile;
    }


    // ========================================
    // プライベートメソッド
    // ========================================
    /// <summary>
    /// 山牌を構築する
    /// </summary>
    /// <param name="isThreePlayer">三人麻雀かどうか</param>
    /// <param name="useRedDora">赤ドラを使用するかどうか</param>
    private void BuildDeck(bool isThreePlayer, bool useRedDora)
    {
        _tiles.Clear();

        // 萬子
        for (var number = 1; number <= 9; number++)
        {
            // 三人麻雀では萬子の2〜8を除く
            if (isThreePlayer && number >= 2 && number <= 8)
            {
                continue;
            }

            var id = GetManzuId(number);

            for (var i = 0; i < 4; i++)
            {
                // 赤ドラ（5萬の1枚目を赤にする）
                var isRed = useRedDora && number == 5 && i == 0;
                var redId = isRed ? TileId.Manzu5Red : id;
                _tiles.Add(new Tile(redId, TileSuit.Manzu, number, isRed));
            }
        }

        // 筒子
        for (var number = 1; number <= 9; number++)
        {
            var id = GetPinzuId(number);

            for (var i = 0; i < 4; i++)
            {
                var isRed = useRedDora && number == 5 && i == 0;
                var redId = isRed ? TileId.Pinzu5Red : id;
                _tiles.Add(new Tile(redId, TileSuit.Pinzu, number, isRed));
            }
        }

        // 索子
        for (var number = 1; number <= 9; number++)
        {
            var id = GetSouzuId(number);

            for (var i = 0; i < 4; i++)
            {
                var isRed = useRedDora && number == 5 && i == 0;
                var redId = isRed ? TileId.Souzu5Red : id;
                _tiles.Add(new Tile(redId, TileSuit.Souzu, number, isRed));
            }
        }

        // 字牌（各4枚）
        var jihaiIds = new[]
        {
                TileId.East, TileId.South, TileId.West, TileId.North,
                TileId.Haku, TileId.Hatsu, TileId.Chun,
            };

        foreach (var jihaiId in jihaiIds)
        {
            for (var i = 0; i < 4; i++)
            {
                _tiles.Add(new Tile(jihaiId, TileSuit.Jihai, 0));
            }
        }
    }
    /// <summary>
    /// 山牌をシャッフルする（Fisher-Yatesアルゴリズム）
    /// </summary>
    private void Shuffle()
    {
        for (var i = _tiles.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (_tiles[i], _tiles[j]) = (_tiles[j], _tiles[i]);
        }

        _drawIndex = 0;
    }
    /// <summary>
    /// 王牌（ドラ表示牌・嶺上牌）をセットアップする
    /// 山の末尾14枚を王牌とする
    /// </summary>
    private void SetupDeadWall()
    {
        var deadWall = new List<Tile>();

        for (var i = _tiles.Count - DeadWallCount; i < _tiles.Count; i++)
        {
            deadWall.Add(_tiles[i]);
        }

        DeadWall = deadWall.AsReadOnly();
    }


    // ========================================
    // タイルIDを取得するヘルパーメソッド
    // ========================================
    /// <summary>
    /// 数字から萬子のTileIdを取得する
    /// </summary>
    /// <param name="number">1〜9の数字</param>
    /// <returns>対応するTileId</returns>
    private static TileId GetManzuId(int number)
    {
        return number switch
        {
            1 => TileId.Manzu1,
            2 => TileId.Manzu2,
            3 => TileId.Manzu3,
            4 => TileId.Manzu4,
            5 => TileId.Manzu5,
            6 => TileId.Manzu6,
            7 => TileId.Manzu7,
            8 => TileId.Manzu8,
            9 => TileId.Manzu9,
            _ => TileId.Manzu1,
        };
    }
    /// <summary>
    /// 数字から筒子のTileIdを取得する
    /// </summary>
    /// <param name="number">1〜9の数字</param>
    /// <returns>対応するTileId</returns>
    private static TileId GetPinzuId(int number)
    {
        return number switch
        {
            1 => TileId.Pinzu1,
            2 => TileId.Pinzu2,
            3 => TileId.Pinzu3,
            4 => TileId.Pinzu4,
            5 => TileId.Pinzu5,
            6 => TileId.Pinzu6,
            7 => TileId.Pinzu7,
            8 => TileId.Pinzu8,
            9 => TileId.Pinzu9,
            _ => TileId.Pinzu1,
        };
    }
    /// <summary>
    /// 数字から索子のTileIdを取得する
    /// </summary>
    /// <param name="number">1〜9の数字</param>
    /// <returns>対応するTileId</returns>
    private static TileId GetSouzuId(int number)
    {
        return number switch
        {
            1 => TileId.Souzu1,
            2 => TileId.Souzu2,
            3 => TileId.Souzu3,
            4 => TileId.Souzu4,
            5 => TileId.Souzu5,
            6 => TileId.Souzu6,
            7 => TileId.Souzu7,
            8 => TileId.Souzu8,
            9 => TileId.Souzu9,
            _ => TileId.Souzu1,
        };
    }
}