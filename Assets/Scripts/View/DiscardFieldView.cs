using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.PlayerDiscards を購読し、全プレイヤーの河（捨て牌）を3D牌メッシュで卓上に表示する
    /// PlayerDiscardsは自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 「自分（offset=0）の並べ方」を基準に、卓の中心を軸に90度ずつ回転させて他の席の配置を求める
    /// 既に配置済みの牌には触れず、増えた分だけを追加する（配置済みの牌がジッターも含めて動かないようにするため）
    /// </summary>
    public sealed class DiscardFieldView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
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
        /// 牌の位置に加えるランダムなずれの最大幅（機械的に整列しすぎないようにする）
        /// </summary>
        private const float POSITION_JITTER_RANGE = 0.015f;
        /// <summary>
        /// 牌の向きに加えるランダムな回転の最大幅（度）
        /// </summary>
        private const float ROTATION_JITTER_DEGREES = 3.0f;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 河データの購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 牌GameObjectの親
        /// </summary>
        private Transform _fieldRoot;
        /// <summary>
        /// 席（自分から見た相対位置）ごとに配置済みの牌GameObject
        /// 既に配置した牌は動かさず、増えた分だけをリストへ追加していく
        /// </summary>
        private readonly List<List<GameObject>> _seatTileObjects = new();
        /// <summary>
        /// 牌1枚分の基準サイズ（幅・奥行き）
        /// 全種類の牌でほぼ同じ実寸のため、最初に配置した1枚から計測した値を使い回す
        /// </summary>
        private Bounds? _referenceTileBounds;


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
        /// 河が更新されるたびに、増えた分の牌だけを追加で配置する
        /// </summary>
        private void OnPlayerDiscardsChanged(IReadOnlyList<IReadOnlyList<TileView>> playerDiscards)
        {
            if (playerDiscards == null || playerDiscards.Count == 0)
            {
                return;
            }

            while (_seatTileObjects.Count < playerDiscards.Count)
            {
                _seatTileObjects.Add(new List<GameObject>());
            }

            for (var offset = 0; offset < playerDiscards.Count; offset++)
            {
                UpdateSeatDiscards(playerDiscards[offset], offset);
            }
        }
        /// <summary>
        /// 1人分の河を最新の状態に合わせる
        /// 既に配置済みの牌はそのままにし、末尾に増えた牌だけを新しく配置する
        /// 局が変わって河が短くなった（リセットされた）場合のみ、すべて作り直す
        /// </summary>
        private void UpdateSeatDiscards(IReadOnlyList<TileView> discards, int offset)
        {
            var existingTiles = _seatTileObjects[offset];

            if (discards.Count < existingTiles.Count)
            {
                foreach (var tileObject in existingTiles)
                {
                    Destroy(tileObject);
                }

                existingTiles.Clear();
            }

            if (discards.Count == existingTiles.Count)
            {
                return;
            }

            for (var index = existingTiles.Count; index < discards.Count; index++)
            {
                var tileObject = PlaceDiscardTile(discards[index], index, offset);

                if (tileObject != null)
                {
                    existingTiles.Add(tileObject);
                }
            }
        }
        /// <summary>
        /// 牌1枚を、河の中でのインデックス（行・列）から求めた位置に配置する
        /// 「自分（offset=0）が手前で正面を向いている」座標系で位置を組み立てた後、
        /// 卓の中心を軸に90度×offset回転させて、その席の位置・向きに変換する
        /// </summary>
        /// <param name="tile">配置する牌</param>
        /// <param name="index">河の中での何枚目か（0から数える）</param>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        /// <returns>配置した牌のGameObject。メッシュが読み込めない場合はnull</returns>
        private GameObject PlaceDiscardTile(TileView tile, int index, int offset)
        {
            var prefab = TileMeshLibrary.LoadPrefab(tile);

            if (prefab == null)
            {
                return null;
            }

            var tileObject = Instantiate(prefab, _fieldRoot);
            tileObject.transform.position = Vector3.zero;
            tileObject.transform.rotation = Quaternion.identity;

            // 姿勢を確定させる前（恒等回転）の状態でバウンディングボックスを計測する
            // （後段でどの席向けに回転させても、幅・奥行きの意味が変わらないようにするため）
            var localBounds = TileMeshLibrary.MeasureBounds(tileObject);
            _referenceTileBounds ??= localBounds;
            var reference = _referenceTileBounds.Value;

            var tileWidth = reference.size.x * (1.0f + TILE_MARGIN_FACTOR);
            var rowDepth = reference.size.z * (1.0f + ROW_MARGIN_FACTOR);
            var rowStartX = -tileWidth * TILES_PER_ROW * 0.5f;

            var row = index / TILES_PER_ROW;
            var col = index % TILES_PER_ROW;
            var localX = rowStartX + col * tileWidth + reference.size.x * 0.5f;
            var localZ = -TableLayout.DISCARD_ROW_DISTANCE_FROM_CENTER - row * rowDepth;

            // 機械的に整列しすぎないよう、位置・向きに小さなランダムなずれを加える
            // （このメソッドは新規に増えた牌にしか呼ばれないため、既に配置済みの牌のずれは変わらない）
            var jitterX = Random.Range(-POSITION_JITTER_RANGE, POSITION_JITTER_RANGE);
            var jitterZ = Random.Range(-POSITION_JITTER_RANGE, POSITION_JITTER_RANGE);
            var jitterRotation = Quaternion.Euler(0.0f, Random.Range(-ROTATION_JITTER_DEGREES, ROTATION_JITTER_DEGREES), 0.0f);

            // 牌の底面が卓の設置面に揃うよう、実測バウンディングボックスからYを補正する
            // （ピボットが牌の中心にあるため、そのままだと底面が沈んだり浮いたりする）
            var localPosition = new Vector3(
                localX + jitterX,
                TableLayout.SURFACE_Y - localBounds.min.y,
                localZ + jitterZ);

            tileObject.transform.SetPositionAndRotation(
                TableLayout.ToWorldPosition(localPosition, offset),
                TableLayout.GetFlatTileRotation(offset) * jitterRotation);

            return tileObject;
        }
    }
}
