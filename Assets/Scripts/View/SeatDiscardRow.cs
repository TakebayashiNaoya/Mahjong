using System.Collections.Generic;
using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 1席分の河（捨て牌）を、席ローカル座標で卓上に並べる
    /// 既に配置済みの牌には触れず、増えた分だけを追加する（配置済みの牌がジッターも含めて動かないようにするため）
    /// 局が変わって河が短くなった場合のみ、すべて作り直す
    /// </summary>
    public sealed class SeatDiscardRow
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
        private const float TILE_MARGIN_FACTOR = 0.05f;
        /// <summary>
        /// 行同士の隙間（牌の奥行きに対する割合）
        /// 牌同士の左右の隙間（TILE_MARGIN_FACTOR）と揃える
        /// </summary>
        private const float ROW_MARGIN_FACTOR = TILE_MARGIN_FACTOR;
        /// <summary>
        /// 牌の位置に加えるランダムなずれの最大幅（牌同士の隙間に対する割合）
        /// 機械的に整列しすぎないようにするためのもの
        /// 絶対値ではなく隙間に対する割合にしているのは、隙間を詰めたときにずれだけが残って
        /// 隣の牌と重なるのを防ぐため（半分までに抑えれば、隣り合う牌がずれても接するに留まる）
        /// </summary>
        private const float POSITION_JITTER_FACTOR = 0.5f;
        /// <summary>
        /// 牌の向きに加えるランダムな回転の最大幅（度）
        /// </summary>
        private const float ROTATION_JITTER_DEGREES = 1.5f;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 牌を置く席のルート
        /// </summary>
        private readonly Transform _seatRoot;
        /// <summary>
        /// 配置済みの牌GameObject（河の順）
        /// </summary>
        private readonly List<GameObject> _tileObjects = new();
        /// <summary>
        /// 寝かせた牌1枚分の基準サイズ（初回計測後はキャッシュする）
        /// 全種類の牌でほぼ同じ実寸のため、1枚から計測した値を使い回す
        /// </summary>
        private Bounds? _referenceTileBounds;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 河の表示を生成する
        /// </summary>
        /// <param name="seatRoot">牌を置く席のルート</param>
        public SeatDiscardRow(Transform seatRoot)
        {
            _seatRoot = seatRoot;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 河を最新の状態に合わせる
        /// </summary>
        /// <param name="discards">その席の河（捨てた順）</param>
        public void UpdateTiles(IReadOnlyList<TileView> discards)
        {
            if (discards.Count < _tileObjects.Count)
            {
                foreach (var tileObject in _tileObjects)
                {
                    Object.Destroy(tileObject);
                }

                _tileObjects.Clear();
            }

            for (var index = _tileObjects.Count; index < discards.Count; index++)
            {
                var tileObject = PlaceTile(discards[index], index);

                if (tileObject != null)
                {
                    _tileObjects.Add(tileObject);
                }
            }
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 牌1枚を、河の中でのインデックス（行・列）から求めた位置に配置する
        /// </summary>
        /// <param name="tile">配置する牌</param>
        /// <param name="index">河の中での何枚目か（0から数える）</param>
        /// <returns>配置した牌のGameObject。メッシュが読み込めない場合はnull</returns>
        private GameObject PlaceTile(TileView tile, int index)
        {
            var prefab = TileMeshLibrary.LoadPrefab(tile);

            if (prefab == null)
            {
                return null;
            }

            var reference = ResolveReferenceTileBounds(prefab);
            var tileWidth = reference.size.x * (1.0f + TILE_MARGIN_FACTOR);
            var rowDepth = reference.size.z * (1.0f + ROW_MARGIN_FACTOR);
            var rowStartX = -tileWidth * TILES_PER_ROW * 0.5f;

            var row = index / TILES_PER_ROW;
            var col = index % TILES_PER_ROW;

            // 機械的に整列しすぎないよう、位置・向きに小さなランダムなずれを加える
            // （このメソッドは新規に増えた牌にしか呼ばれないため、既に配置済みの牌のずれは変わらない）
            var jitterRange = reference.size.x * TILE_MARGIN_FACTOR * POSITION_JITTER_FACTOR;
            var jitterX = Random.Range(-jitterRange, jitterRange);
            var jitterZ = Random.Range(-jitterRange, jitterRange);
            var jitterRotation = Quaternion.Euler(0.0f, Random.Range(-ROTATION_JITTER_DEGREES, ROTATION_JITTER_DEGREES), 0.0f);

            // 牌の底面が卓の設置面に揃うようYを補正する
            // （ピボットが牌の中心にあるため、そのままだと底面が沈んだり浮いたりする）
            var localPosition = new Vector3(
                rowStartX + col * tileWidth + reference.size.x * 0.5f + jitterX,
                -reference.min.y,
                -TableLayout.DISCARD_ROW_DISTANCE_FROM_CENTER - row * rowDepth + jitterZ);

            var tileObject = Object.Instantiate(prefab, _seatRoot);
            tileObject.transform.SetLocalPositionAndRotation(localPosition, TableLayout.FlatTileRotation * jitterRotation);
            return tileObject;
        }
        /// <summary>
        /// 寝かせた牌1枚分の基準サイズを計測する（初回のみ、以後はキャッシュを返す）
        /// 席のルートは席ごとに回転しているため、その下ではなく階層の外で計測する
        /// （Renderer.boundsはワールド座標基準のため、回転した親の下では幅と奥行きが入れ替わってしまう）
        /// </summary>
        private Bounds ResolveReferenceTileBounds(GameObject prefab)
        {
            if (_referenceTileBounds.HasValue)
            {
                return _referenceTileBounds.Value;
            }

            // FBXのデフォルト姿勢が寝かせた状態のため、回転はかけずに計測する
            _referenceTileBounds = TileMeshLibrary.MeasurePrefabBounds(prefab, Quaternion.identity);
            return _referenceTileBounds.Value;
        }
    }
}
