using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// 手牌の管理・操作を担うクラス
/// プレイヤー・CPU 共通で使用する
/// </summary>
public class Hand
{
    // ========================================
    // プロパティ
    // ========================================
    /// <summary>
    /// 手牌リスト（ソート済み・ツモ牌を除く）
    /// </summary>
    public IReadOnlyList<Tile> Tiles => _tiles;
    /// <summary>
    /// 直前にツモった牌
    /// ツモっていない場合は null
    /// </summary>
    public Tile DrawnTile { get; private set; }
    /// <summary>
    /// 副露リスト
    /// </summary>
    public IReadOnlyList<Meld> Melds => _melds;
    /// <summary>
    /// 手牌の合計枚数（Tiles + DrawnTile）
    /// </summary>
    public int TileCount => _tiles.Count + (DrawnTile != null ? 1 : 0);


    // ========================================
    // フィールド
    // ========================================
    /// <summary>
    /// 手牌リスト（内部）
    /// </summary>
    private readonly List<Tile> _tiles = new();
    /// <summary>
    /// 副露リスト（内部）
    /// </summary>
    private readonly List<Meld> _melds = new();


    // ========================================
    // パブリックメソッド
    // ========================================
    /// <summary>
    /// 牌をツモる
    /// DrawnTile にセットし、手牌リストには加えない
    /// </summary>
    /// <param name="tile">ツモった牌</param>
    public void Draw(Tile tile)
    {
        if (DrawnTile != null)
        {
            Debug.LogWarning("すでにツモ牌があります");
        }

        DrawnTile = tile;
    }
    /// <summary>
    /// 牌を捨てる
    /// ツモ牌または手牌から1枚を捨て、ソートし直す
    /// </summary>
    /// <param name="tile">捨てる牌</param>
    /// <returns>捨てた牌</returns>
    public Tile Discard(Tile tile)
    {
        // ツモ牌を捨てる場合
        if (DrawnTile != null && DrawnTile.IsSameType(tile))
        {
            var discarded = DrawnTile;
            DrawnTile = null;
            return discarded;
        }

        // 手牌から捨てる場合
        var target = _tiles.FirstOrDefault(t => t.IsSameType(tile));

        if (target == null)
        {
            Debug.LogError($"捨てようとした牌が手牌にありません: {tile}");
            return null;
        }

        _tiles.Remove(target);

        // ツモ牌を手牌に移してソートする
        if (DrawnTile != null)
        {
            _tiles.Add(DrawnTile);
            DrawnTile = null;
        }

        Sort();
        return target;
    }
    /// <summary>
    /// 副露を追加する
    /// 手牌から該当する牌を取り除き、副露リストに加える
    /// </summary>
    /// <param name="meld">追加する副露</param>
    public void AddMeld(Meld meld)
    {
        // ツモ牌が残っている場合は手牌に戻す
        if (DrawnTile != null)
        {
            _tiles.Add(DrawnTile);
            DrawnTile = null;
        }

        // 副露に使う手牌の牌を取り除く（鳴いた牌を除く）
        foreach (var meldTile in meld.Tiles)
        {
            // 鳴いた牌（StolenTile）は手牌にないのでスキップ
            if (meld.StolenTile != null && meldTile == meld.StolenTile)
            {
                continue;
            }

            var target = _tiles.FirstOrDefault(t => t.IsSameType(meldTile));

            if (target == null)
            {
                Debug.LogError($"副露に使う牌が手牌にありません: {meldTile}");
                continue;
            }

            _tiles.Remove(target);
        }

        _melds.Add(meld);
        Sort();
    }
    /// <summary>
    /// 加槓を行う
    /// ポン済みの刻子に手牌から同じ牌を1枚追加して槓子にする
    /// </summary>
    /// <param name="tile">追加する牌</param>
    /// <returns>加槓が成功したかどうか</returns>
    public bool AddKakan(Tile tile)
    {
        // ポン済みの面子を探す
        var ponMeld = _melds.FirstOrDefault(m =>
            m.Type == MeldType.Pon && m.Tiles[0].IsSameType(tile));

        if (ponMeld == null)
        {
            Debug.LogWarning($"加槓できる面子が見つかりません: {tile}");
            return false;
        }

        // 手牌またはツモ牌から該当する牌を取り除く
        Tile target = null;

        if (DrawnTile != null && DrawnTile.IsSameType(tile))
        {
            target = DrawnTile;
            DrawnTile = null;
        }
        else
        {
            target = _tiles.FirstOrDefault(t => t.IsSameType(tile));

            if (target != null)
            {
                _tiles.Remove(target);
            }
        }

        if (target == null)
        {
            Debug.LogError($"加槓する牌が手牌にありません: {tile}");
            return false;
        }

        ponMeld.AddKakanTile(target);
        return true;
    }
    /// <summary>
    /// 手牌とツモ牌を合わせた全牌リストを返す
    /// 和了判定・シャンテン計算などに使用する
    /// </summary>
    /// <returns>全牌リスト</returns>
    public List<Tile> GetAllTiles()
    {
        var all = new List<Tile>(_tiles);

        if (DrawnTile != null)
        {
            all.Add(DrawnTile);
        }

        return all;
    }
    /// <summary>
    /// 配牌時に手牌をまとめてセットする
    /// </summary>
    /// <param name="tiles">配牌する牌のリスト（13枚）</param>
    public void SetInitialTiles(List<Tile> tiles)
    {
        _tiles.Clear();
        _melds.Clear();
        DrawnTile = null;
        _tiles.AddRange(tiles);
        Sort();
    }


    // ========================================
    // プライベートメソッド
    // ========================================
    /// <summary>
    /// 手牌をスーツ→数字の順にソートする
    /// ソート順：萬子 → 筒子 → 索子 → 字牌
    /// </summary>
    private void Sort()
    {
        _tiles.Sort((a, b) =>
        {
            // スーツ比較
            var suitCompare = a.Suit.CompareTo(b.Suit);

            if (suitCompare != 0)
            {
                return suitCompare;
            }

            // 数字比較（字牌は TileId で比較）
            if (a.Suit == TileSuit.Jihai)
            {
                return a.Id.CompareTo(b.Id);
            }

            return a.Number.CompareTo(b.Number);
        });
    }
}
