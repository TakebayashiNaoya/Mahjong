using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


/// <summary>
/// 手牌の管理・操作を担うクラス
/// プレイヤー・CPU 共通で使用する
/// </summary>
public class Hand
{
    // ========================================
    // 定数
    // ========================================
    /// <summary>
    /// 配牌時の初期手牌枚数
    /// </summary>
    private const int INITIAL_TILE_COUNT = 13;


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
    /// ReadOnlyCollection をフィールドとして保持することで毎回の生成コストを避ける
    /// </summary>
    public IReadOnlyList<Meld> Melds => _meldsReadOnly;
    /// <summary>
    /// 手牌の合計枚数（Tiles + DrawnTile）
    /// 副露した牌は含まない
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
    /// <summary>
    /// 副露リストの読み取り専用ラッパー
    /// _melds と同じインスタンスを参照するため、_melds の変更が反映される
    /// </summary>
    private readonly ReadOnlyCollection<Meld> _meldsReadOnly;


    // ========================================
    // コンストラクタ
    // ========================================
    /// <summary>
    /// 手牌を初期化する
    /// </summary>
    public Hand()
    {
        _meldsReadOnly = _melds.AsReadOnly();
    }


    // ========================================
    // パブリックメソッド
    // ========================================
    /// <summary>
    /// 牌をツモる
    /// DrawnTile にセットし、手牌リストには加えない
    /// </summary>
    /// <param name="tile">ツモった牌</param>
    /// <exception cref="ArgumentNullException">tile が null の場合</exception>
    /// <exception cref="InvalidOperationException">すでにツモ牌がある場合</exception>
    public void Draw(Tile tile)
    {
        if (tile == null)
        {
            throw new ArgumentNullException(nameof(tile), "ツモ牌が null です");
        }

        if (DrawnTile != null)
        {
            throw new InvalidOperationException("すでにツモ牌があります。Discard を呼んでから Draw してください");
        }

        DrawnTile = tile;
    }
    /// <summary>
    /// 牌を捨てる
    /// ツモ牌または手牌から1枚を捨て、ソートし直す
    /// 同種の牌が複数ある場合はツモ牌を優先して捨てる
    /// Sort で赤ドラは通常牌の後ろに並ぶため、通常牌が優先して捨てられる
    /// </summary>
    /// <param name="tile">捨てる牌</param>
    /// <returns>捨てた牌</returns>
    /// <exception cref="ArgumentNullException">tile が null の場合</exception>
    /// <exception cref="InvalidOperationException">捨てる牌が手牌にない場合</exception>
    public Tile Discard(Tile tile)
    {
        if (tile == null)
        {
            throw new ArgumentNullException(nameof(tile), "捨てる牌が null です");
        }

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
            throw new InvalidOperationException($"捨てようとした牌が手牌にありません: {tile}");
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
    /// 手牌から副露に使う牌を取り除き、副露リストに加える
    /// StolenTile と同種の牌は最初の1枚のみスキップし、残りは手牌から取り除く
    /// </summary>
    /// <param name="meld">追加する副露</param>
    /// <exception cref="ArgumentNullException">meld が null の場合</exception>
    /// <exception cref="InvalidOperationException">副露に使う牌が手牌にない場合</exception>
    public void AddMeld(Meld meld)
    {
        if (meld == null)
        {
            throw new ArgumentNullException(nameof(meld), "meld が null です");
        }

        // ツモ牌が残っている場合は手牌に戻す
        if (DrawnTile != null)
        {
            _tiles.Add(DrawnTile);
            DrawnTile = null;
        }

        // 手牌から取り除く牌のリストを作成する
        // StolenTile と同種の牌は最初の1枚だけスキップする（鳴いた牌は手牌にないため）
        var tilesToRemove = new List<Tile>();
        var stolenSkipped = false;

        foreach (var meldTile in meld.Tiles)
        {
            if (!stolenSkipped
                && meld.StolenTile != null
                && meldTile.IsSameType(meld.StolenTile))
            {
                stolenSkipped = true;
                continue;
            }

            tilesToRemove.Add(meldTile);
        }

        // 手牌に必要な牌がすべて揃っているか事前確認する
        var tempTiles = new List<Tile>(_tiles);

        foreach (var removeTarget in tilesToRemove)
        {
            var found = tempTiles.FirstOrDefault(t => t.IsSameType(removeTarget));

            if (found == null)
            {
                throw new InvalidOperationException($"副露に使う牌が手牌にありません: {removeTarget}");
            }

            tempTiles.Remove(found);
        }

        // 確認が取れたので実際に手牌から取り除く
        foreach (var removeTarget in tilesToRemove)
        {
            var found = _tiles.FirstOrDefault(t => t.IsSameType(removeTarget));

            if (found != null)
            {
                _tiles.Remove(found);
            }
        }

        _melds.Add(meld);
        Sort();
    }
    /// <summary>
    /// 加槓を行う
    /// ポン済みの刻子に手牌から同じ牌を1枚追加して槓子にする
    /// TryApplyKakan で検証・更新が成功してから手牌の牌を除去する
    /// </summary>
    /// <param name="tile">追加する牌</param>
    /// <returns>成功した場合は true</returns>
    /// <exception cref="ArgumentNullException">tile が null の場合</exception>
    public bool AddKakan(Tile tile)
    {
        if (tile == null)
        {
            throw new ArgumentNullException(nameof(tile), "加槓する牌が null です");
        }

        // ポン済みの面子を探す
        var ponMeld = _melds.FirstOrDefault(m =>
            m.Type == MeldType.Pon && m.Tiles[0].IsSameType(tile));

        if (ponMeld == null)
        {
            return false;
        }

        // 加槓に使う牌を手牌またはツモ牌から探す（まだ除去しない）
        Tile target = null;

        if (DrawnTile != null && DrawnTile.IsSameType(tile))
        {
            target = DrawnTile;
        }
        else
        {
            target = _tiles.FirstOrDefault(t => t.IsSameType(tile));
        }

        if (target == null)
        {
            return false;
        }

        // 検証・更新が成功してから手牌の牌を除去する
        if (!ponMeld.TryApplyKakan(target))
        {
            return false;
        }

        if (DrawnTile == target)
        {
            DrawnTile = null;
        }
        else
        {
            _tiles.Remove(target);
        }

        return true;
    }
    /// <summary>
    /// 手牌とツモ牌を返す（副露牌は含まない）
    /// シャンテン数計算など、門前の牌のみが必要な場合に使用する
    /// </summary>
    /// <returns>手牌 + ツモ牌のリスト</returns>
    public List<Tile> GetClosedTiles()
    {
        var all = new List<Tile>(_tiles);

        if (DrawnTile != null)
        {
            all.Add(DrawnTile);
        }

        return all;
    }
    /// <summary>
    /// 手牌・ツモ牌・副露牌をすべて含む全牌リストを返す
    /// 役判定など、すべての牌が必要な場合に使用する
    /// </summary>
    /// <returns>手牌 + ツモ牌 + 副露牌のリスト</returns>
    public List<Tile> GetAllTiles()
    {
        var all = GetClosedTiles();

        foreach (var meld in _melds)
        {
            all.AddRange(meld.Tiles);
        }

        return all;
    }
    /// <summary>
    /// 配牌時に手牌をまとめてセットする
    /// </summary>
    /// <param name="tiles">配牌する牌のリスト（13枚）</param>
    /// <exception cref="ArgumentNullException">tiles が null の場合</exception>
    /// <exception cref="ArgumentException">tiles の枚数が13枚でない場合</exception>
    public void SetInitialTiles(List<Tile> tiles)
    {
        if (tiles == null)
        {
            throw new ArgumentNullException(nameof(tiles), "tiles が null です");
        }

        if (tiles.Count != INITIAL_TILE_COUNT)
        {
            throw new ArgumentException($"配牌の枚数が不正です。{INITIAL_TILE_COUNT}枚である必要があります: {tiles.Count}枚", nameof(tiles));
        }

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
    /// 手牌をスーツ→数字→赤ドラの順に安定ソートする
    /// ソート順：萬子 → 筒子 → 索子 → 字牌
    /// 同種の牌では通常牌を赤ドラより前に並べる（意図しない赤ドラ捨てを防ぐ）
    /// List.Sort は不安定ソートのため、インデックスを使った安定ソートで実装する
    /// </summary>
    private void Sort()
    {
        // インデックス付きで安定ソートを実現する
        var indexed = _tiles
            .Select((tile, index) => (tile, index))
            .OrderBy(t => t.tile.Suit)
            .ThenBy(t => t.tile.Suit == TileSuit.Jihai ? (int)t.tile.Id : t.tile.Number)
            .ThenBy(t => t.tile.IsRed ? 1 : 0)
            .ThenBy(t => t.index)
            .Select(t => t.tile)
            .ToList();

        _tiles.Clear();
        _tiles.AddRange(indexed);
    }
}