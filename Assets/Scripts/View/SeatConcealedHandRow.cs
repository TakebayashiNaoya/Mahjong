using System.Collections.Generic;
using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 1席分の伏せ手牌を、席ローカル座標で卓上に並べる
    /// 手牌の中身は公開情報ではないため、枚数分だけ絵柄の無い牌（pai.fbx）を立てて並べる
    /// 門前牌の並びは門前の枚数だけで決まるため、ツモ・打牌のたびに増減するツモ牌は別の1枚として扱い、
    /// 門前牌が動かないようにする
    /// </summary>
    public sealed class SeatConcealedHandRow
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 伏せ牌として使う、絵柄の無い牌メッシュのファイル名
        /// </summary>
        private const string CONCEALED_TILE_MESH_NAME = "pai";
        /// <summary>
        /// 牌同士の隙間（牌の幅に対する割合）
        /// </summary>
        private const float TILE_MARGIN_FACTOR = 0.1f;
        /// <summary>
        /// 門前牌の右端からツモ牌までに空ける隙間（牌の幅に対する割合）
        /// 自分の手牌アイコン（InGameView）と同じく、牌1枚分空けてツモ牌と分かるようにする
        /// </summary>
        private const float DRAWN_TILE_GAP_FACTOR = 1.0f;
        /// <summary>
        /// 牌1枚の姿勢（起こして席の正面を向ける）
        /// </summary>
        private static readonly Quaternion StandingTileRotation = Quaternion.Euler(90.0f, 0.0f, 180.0f);


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 牌を置く席のルート
        /// </summary>
        private readonly Transform _seatRoot;
        /// <summary>
        /// 配置済みの門前牌
        /// </summary>
        private readonly List<GameObject> _concealedTiles = new();
        /// <summary>
        /// 配置済みのツモ牌（持っていない場合はnull）
        /// </summary>
        private GameObject _drawnTile;
        /// <summary>
        /// 現在配置している門前牌の枚数（未配置は-1）
        /// </summary>
        private int _concealedTileCount = -1;
        /// <summary>
        /// 伏せ牌メッシュのプレハブ（初回読み込み後はキャッシュする）
        /// </summary>
        private GameObject _tilePrefab;
        /// <summary>
        /// 立てた牌1枚分の基準サイズ（初回計測後はキャッシュする）
        /// </summary>
        private Bounds? _referenceTileBounds;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 伏せ手牌の表示を生成する
        /// </summary>
        /// <param name="seatRoot">牌を置く席のルート</param>
        public SeatConcealedHandRow(Transform seatRoot)
        {
            _seatRoot = seatRoot;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 伏せ手牌を最新の状態に合わせる
        /// 門前の枚数が変わらない限り門前牌には触れず、ツモ牌の1枚だけを足し引きする
        /// </summary>
        /// <param name="hand">その席の伏せ手牌</param>
        public void UpdateTiles(ConcealedHandView hand)
        {
            var prefab = ResolveTilePrefab();

            if (prefab == null)
            {
                return;
            }

            var isConcealedRowChanged = _concealedTileCount != hand.ConcealedTileCount;

            if (isConcealedRowChanged)
            {
                RebuildConcealedTiles(hand.ConcealedTileCount, prefab);

                // 門前牌の並びが変わるとツモ牌の位置もずれるため、いったん取り除いて置き直す
                DestroyDrawnTile();
            }

            if (hand.HasDrawnTile == (_drawnTile != null))
            {
                return;
            }

            if (!hand.HasDrawnTile)
            {
                DestroyDrawnTile();
                return;
            }

            // 門前牌の右端から隙間を1つ空けた位置に置く
            _drawnTile = PlaceTile(_concealedTileCount + DRAWN_TILE_GAP_FACTOR, prefab);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 門前牌の列を作り直す
        /// </summary>
        /// <param name="concealedTileCount">門前牌の枚数</param>
        /// <param name="prefab">伏せ牌メッシュのプレハブ</param>
        private void RebuildConcealedTiles(int concealedTileCount, GameObject prefab)
        {
            foreach (var tileObject in _concealedTiles)
            {
                Object.Destroy(tileObject);
            }

            _concealedTiles.Clear();
            _concealedTileCount = concealedTileCount;

            for (var index = 0; index < concealedTileCount; index++)
            {
                _concealedTiles.Add(PlaceTile(index, prefab));
            }
        }
        /// <summary>
        /// 牌1枚を、列の中での位置から求めた座標に配置する
        /// </summary>
        /// <param name="indexInRow">列の左端から数えた位置（ツモ牌のように隙間を空ける場合は小数もとる）</param>
        /// <param name="prefab">伏せ牌メッシュのプレハブ</param>
        /// <returns>配置した牌のGameObject</returns>
        private GameObject PlaceTile(float indexInRow, GameObject prefab)
        {
            var reference = ResolveReferenceTileBounds(prefab);
            var tileWidth = reference.size.x * (1.0f + TILE_MARGIN_FACTOR);

            // 左端を門前の最大枚数から決めることで、鳴いて枚数が減っても残った牌が動かない
            // （現在の枚数で中央揃えにすると、鳴くたびに列全体が左右にずれてしまう）
            var startX = -tileWidth * TableLayout.CONCEALED_HAND_SLOT_COUNT * 0.5f;

            // 牌の底面が卓の設置面に揃うようYを補正する
            var localPosition = new Vector3(
                startX + indexInRow * tileWidth + reference.size.x * 0.5f,
                -reference.min.y,
                -TableLayout.CONCEALED_HAND_DISTANCE_FROM_CENTER);

            var tileObject = Object.Instantiate(prefab, _seatRoot);
            tileObject.transform.SetLocalPositionAndRotation(localPosition, StandingTileRotation);
            return tileObject;
        }
        /// <summary>
        /// ツモ牌のGameObjectを破棄する（無ければ何もしない）
        /// </summary>
        private void DestroyDrawnTile()
        {
            if (_drawnTile == null)
            {
                return;
            }

            Object.Destroy(_drawnTile);
            _drawnTile = null;
        }
        /// <summary>
        /// 立てた牌1枚分の基準サイズを計測する（初回のみ、以後はキャッシュを返す）
        /// 席のルートは席ごとに回転しているため、その下ではなく階層の外で計測する
        /// （Renderer.boundsはワールド座標基準のため、回転した親の下では幅と奥行きが入れ替わってしまう）
        /// </summary>
        private Bounds ResolveReferenceTileBounds(GameObject prefab)
        {
            if (_referenceTileBounds.HasValue)
            {
                return _referenceTileBounds.Value;
            }

            _referenceTileBounds = TileMeshLibrary.MeasurePrefabBounds(prefab, StandingTileRotation);
            return _referenceTileBounds.Value;
        }
        /// <summary>
        /// 伏せ牌メッシュのプレハブを取得する（初回のみ読み込み、以後はキャッシュを返す）
        /// 読み込めない場合はnullを返す（TileMeshLibrary側でエラーログを出す）
        /// </summary>
        private GameObject ResolveTilePrefab()
        {
            if (_tilePrefab == null)
            {
                _tilePrefab = TileMeshLibrary.LoadPrefab(CONCEALED_TILE_MESH_NAME);
            }

            return _tilePrefab;
        }
    }
}
