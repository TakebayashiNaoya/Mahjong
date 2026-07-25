using System.Collections.Generic;
using System.Linq;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.PlayerDiscards を購読し、全プレイヤーの河（捨て牌）を3D牌メッシュで卓上に表示する
    /// PlayerDiscardsは自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 「自分（offset=0）の並べ方」を基準に、卓の中心を軸に90度ずつ回転させて他の席の配置を求める
    /// </summary>
    public sealed class DiscardFieldView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// シーン上で卓として参照するGameObjectの名前
        /// </summary>
        private const string TABLE_OBJECT_NAME = "table";
        /// <summary>
        /// 河が見つからない場合に使うフォールバックの卓の大きさ（半径換算）
        /// </summary>
        private static readonly Vector3 FallbackTableExtents = new(3f, 0.1f, 3f);
        /// <summary>
        /// 1行に並べる牌の枚数
        /// </summary>
        private const int TILES_PER_ROW = 6;
        /// <summary>
        /// 牌同士の隙間（牌の幅に対する割合）
        /// </summary>
        private const float TILE_MARGIN_FACTOR = 0.1f;
        /// <summary>
        /// 行同士の隙間（牌の奥行きに対する割合）
        /// 牌同士の左右の隙間（TILE_MARGIN_FACTOR）と揃える
        /// </summary>
        private const float ROW_MARGIN_FACTOR = TILE_MARGIN_FACTOR;
        /// <summary>
        /// 卓の奥行きに対して、手前の縁からどれだけ内側に河の先頭行を置くか
        /// 画面手前の手牌UIとの間に余白ができるよう、卓の中央寄りに置く
        /// 0.5だと卓のちょうど中央になり、4人分の河が1点に重なってしまうため、それより十分小さい値にする
        /// </summary>
        private const float NEAR_EDGE_INSET_FRACTION = 0.39f;
        /// <summary>
        /// 卓の設置面のY座標
        /// 手牌3D表示のときの実測（立てた牌のピボットY=0.20、その半分の高さ0.20が底面までの距離）から
        /// 逆算した推定値。河の牌は寝かせて置くため、ピボットからの底面までの距離は
        /// 牌ごとに実測したバウンディングボックス（localBounds.min.y）から別途補正する
        /// </summary>
        private const float TABLE_SURFACE_Y = 0f;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 河データの購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 牌GameObjectの親（河が更新されるたびに子をすべて作り直す）
        /// </summary>
        private Transform _fieldRoot;
        /// <summary>
        /// 現在表示中の牌GameObject（次の更新でまとめて破棄する）
        /// </summary>
        private readonly List<GameObject> _activeTileObjects = new();


        // ========================================
        // 起動
        // ========================================
        private void Awake()
        {
            _presenter = GetComponent<GamePresenter>();

            var fieldRootObject = new GameObject("DiscardField");
            fieldRootObject.transform.SetParent(transform, false);
            _fieldRoot = fieldRootObject.transform;
        }

        private void Start()
        {
            _presenter.PlayerDiscards.Subscribe(OnPlayerDiscardsChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（河の描画）
        // ========================================
        /// <summary>
        /// 河が更新されるたびに、全プレイヤー分の牌を作り直して並べ直す
        /// </summary>
        private void OnPlayerDiscardsChanged(IReadOnlyList<IReadOnlyList<TileView>> playerDiscards)
        {
            ClearTiles();

            if (playerDiscards == null || playerDiscards.Count == 0)
            {
                return;
            }

            var tableBounds = ResolveTableBounds();

            for (var offset = 0; offset < playerDiscards.Count; offset++)
            {
                PlaceSeatDiscards(playerDiscards[offset], offset, tableBounds);
            }
        }
        /// <summary>
        /// 1人分の河を配置する
        /// 「自分（offset=0）が手前で正面を向いている」座標系でレイアウトを組み立てた後、
        /// 卓の中心を軸に90度×offset回転させて、その席の位置・向きに変換する
        /// </summary>
        private void PlaceSeatDiscards(IReadOnlyList<TileView> discards, int offset, Bounds tableBounds)
        {
            if (discards == null || discards.Count == 0)
            {
                return;
            }

            // 配置位置を回転させるための角度と、牌自体の向きを回転させるための角度は別物のため分ける
            // （どちらも90度×offsetで席の位置に合わせるが、牌の向きにだけ+180度の補正が要る）
            var positionRotation = Quaternion.Euler(0f, 90f * offset, 0f);
            // +180度は、牌のデフォルト姿勢を真上から見ると絵柄が上下逆になるための補正
            // （TileIconCacheのアイコン撮影カメラで必要だったZ=180の補正と同じ理由）
            var facingRotation = Quaternion.Euler(0f, 90f * offset + 180f, 0f);
            var localNearZ = -tableBounds.extents.z + tableBounds.size.z * NEAR_EDGE_INSET_FRACTION;

            // 1周目: 生成とバウンディングボックス計測、行・列への割り振りだけ行う
            // （座標系はまだ回転させていない「自分が手前」の基準のまま）
            var placements = new List<(GameObject tileObject, Bounds localBounds, int row)>();

            for (var i = 0; i < discards.Count; i++)
            {
                var tileObject = InstantiateDiscardTile(discards[i], out var localBounds);

                if (tileObject == null)
                {
                    continue;
                }

                placements.Add((tileObject, localBounds, i / TILES_PER_ROW));
            }

            if (placements.Count == 0)
            {
                return;
            }

            // 全行を「1行6枚分の幅」を基準に左揃えする（行ごとに中央揃えし直さない）
            var referenceTileWidth = placements[0].localBounds.size.x;
            var fullRowWidth = referenceTileWidth * (1f + TILE_MARGIN_FACTOR) * TILES_PER_ROW;
            var rowStartX = -fullRowWidth * 0.5f;

            // 2周目: 行ごとに左揃えで並べる。2行目以降は手前（自分側）へ積む
            foreach (var rowGroup in placements.GroupBy(p => p.row).OrderBy(g => g.Key))
            {
                var tilesInRow = rowGroup.ToList();
                var rowDepth = tilesInRow[0].localBounds.size.z * (1f + ROW_MARGIN_FACTOR);
                var localZ = localNearZ - rowGroup.Key * rowDepth;

                var offsetX = rowStartX;

                foreach (var (tileObject, localBounds, _) in tilesInRow)
                {
                    var localX = offsetX + localBounds.size.x * 0.5f;
                    var localPosition = new Vector3(localX, 0f, localZ);
                    var rotatedOffset = positionRotation * localPosition;
                    // 牌の底面が卓の設置面に揃うよう、実測バウンディングボックスからYを補正する
                    // （ピボットが牌の中心にあるため、そのままだと底面が沈んだり浮いたりする）
                    var restY = TABLE_SURFACE_Y - localBounds.min.y;
                    var worldPosition = new Vector3(
                        tableBounds.center.x + rotatedOffset.x,
                        restY,
                        tableBounds.center.z + rotatedOffset.z);

                    tileObject.transform.position = worldPosition;
                    tileObject.transform.rotation = facingRotation;

                    offsetX += localBounds.size.x * (1f + TILE_MARGIN_FACTOR);
                }
            }
        }
        /// <summary>
        /// 牌1枚をロードして生成する
        /// 姿勢を確定させる前（恒等回転）の状態でバウンディングボックスを計測して返す
        /// （後段でどの席向けに回転させても、幅・奥行きの意味が変わらないようにするため）
        /// </summary>
        private GameObject InstantiateDiscardTile(TileView tile, out Bounds localBounds)
        {
            var prefab = TileMeshLibrary.LoadPrefab(tile);

            if (prefab == null)
            {
                localBounds = default;
                return null;
            }

            var tileObject = Instantiate(prefab, _fieldRoot);
            tileObject.transform.position = Vector3.zero;
            tileObject.transform.rotation = Quaternion.identity;
            _activeTileObjects.Add(tileObject);

            localBounds = TileMeshLibrary.MeasureBounds(tileObject);
            return tileObject;
        }
        /// <summary>
        /// 表示中の牌GameObjectをすべて破棄する
        /// </summary>
        private void ClearTiles()
        {
            foreach (var tileObject in _activeTileObjects)
            {
                Destroy(tileObject);
            }

            _activeTileObjects.Clear();
        }
        /// <summary>
        /// シーンの"table"オブジェクトの実測バウンディングボックスを返す
        /// 見つからない場合はフォールバックの大きさを原点中心で返す
        /// </summary>
        private static Bounds ResolveTableBounds()
        {
            var tableObject = GameObject.Find(TABLE_OBJECT_NAME);

            if (tableObject == null)
            {
                return new Bounds(Vector3.zero, FallbackTableExtents * 2f);
            }

            return TileMeshLibrary.MeasureBounds(tableObject);
        }
    }
}
