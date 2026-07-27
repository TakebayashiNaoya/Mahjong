using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.ConcealedTileCounts を購読し、他家（自分以外）の伏せ手牌を3D牌メッシュで卓上に表示する
    /// 他家の手牌の中身は公開情報ではないため、枚数分だけ絵柄の無い牌（pai.fbx）を並べる
    /// ConcealedTileCountsは自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 「自分（offset=0）の並べ方」を基準に、卓の中心を軸に90度ずつ回転させて他の席の配置を求める
    /// （offset=0は自分自身なので、2Dアイコンで表示済みのこのコンポーネントではスキップする）
    /// </summary>
    public sealed class OpponentHandFieldView : MonoBehaviour
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
        /// 卓の中心から、手牌までの絶対距離
        /// 卓のサイズに対する割合ではなく固定値にする理由: 割合にすると卓を小さくしたときに
        /// 4人分の手牌が中心の1点に近づいて重なってしまう（牌自体の大きさは卓のサイズと無関係なため）
        /// 河（DiscardFieldView.NEAR_ROW_DISTANCE_FROM_CENTER）より縁寄りに置く
        /// </summary>
        private const float NEAR_ROW_DISTANCE_FROM_CENTER = 3.5f;
        /// <summary>
        /// 牌を置くY座標（Transform Position.Yの実測値）
        /// 削除済みのHandTileFieldViewで使っていたのと同じ姿勢（起こしてカメラ側を向ける）に対する実測値のため、
        /// そのまま流用する
        /// </summary>
        private const float TILE_REST_Y = 0.20f;
        /// <summary>
        /// 牌1枚の基準姿勢（起こして正面を向ける）
        /// 削除済みのHandTileFieldViewで使っていたTileRotationと同じ
        /// </summary>
        private static readonly Quaternion BaseTileRotation = Quaternion.Euler(90f, 0f, 180f);


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 伏せ手牌データの購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 牌GameObjectの親
        /// </summary>
        private Transform _fieldRoot;
        /// <summary>
        /// 伏せ牌メッシュのプレハブ（初回読み込み後はキャッシュする）
        /// </summary>
        private GameObject _concealedTilePrefab;
        /// <summary>
        /// 牌1枚分の基準サイズ（初回配置時に計測してキャッシュする）
        /// </summary>
        private Bounds? _referenceBounds;
        /// <summary>
        /// 席（自分から見た相対位置）ごとに配置済みの牌GameObject
        /// </summary>
        private readonly Dictionary<int, List<GameObject>> _seatTileObjects = new();
        /// <summary>
        /// 席（自分から見た相対位置）ごとの直近の枚数
        /// 枚数が変わった席だけ作り直すために保持する
        /// </summary>
        private readonly Dictionary<int, int> _seatTileCounts = new();


        // ========================================
        // 起動
        // ========================================
        private void Awake()
        {
            _presenter = GetComponent<GamePresenter>();

            var fieldRootObject = new GameObject("OpponentHandField");
            fieldRootObject.transform.SetParent(transform, false);
            _fieldRoot = fieldRootObject.transform;
        }

        private void Start()
        {
            _presenter.ConcealedTileCounts.Subscribe(OnConcealedTileCountsChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（伏せ手牌の描画）
        // ========================================
        /// <summary>
        /// 伏せ手牌の枚数が更新されるたびに、枚数が変わった席だけ牌を作り直す
        /// </summary>
        private void OnConcealedTileCountsChanged(IReadOnlyList<int> counts)
        {
            if (counts == null || counts.Count == 0)
            {
                return;
            }

            var tableBounds = TableLayout.ResolveBounds();

            // offset=0は自分自身（2Dアイコンで表示済み）のためスキップする
            for (var offset = 1; offset < counts.Count; offset++)
            {
                var count = counts[offset];

                if (_seatTileCounts.TryGetValue(offset, out var previousCount) && previousCount == count)
                {
                    continue;
                }

                RebuildSeatHand(count, offset, tableBounds);
                _seatTileCounts[offset] = count;
            }
        }
        /// <summary>
        /// 1人分の伏せ手牌を作り直す
        /// 「自分（offset=0）が手前で正面を向いている」座標系で1列に並べたあと、
        /// 卓の中心を軸に90度×offset回転させて、その席の位置・向きに変換する
        /// </summary>
        private void RebuildSeatHand(int tileCount, int offset, Bounds tableBounds)
        {
            if (_seatTileObjects.TryGetValue(offset, out var existingTiles))
            {
                foreach (var tileObject in existingTiles)
                {
                    Destroy(tileObject);
                }
            }

            var newTiles = new List<GameObject>(tileCount);
            _seatTileObjects[offset] = newTiles;

            if (tileCount <= 0)
            {
                return;
            }

            var reference = ResolveReferenceBounds();

            var positionRotation = Quaternion.Euler(0f, 90f * offset, 0f);
            var facingRotation = positionRotation * BaseTileRotation;
            var localNearZ = -NEAR_ROW_DISTANCE_FROM_CENTER;

            var tileWidth = reference.size.x * (1f + TILE_MARGIN_FACTOR);
            var startX = -tileWidth * tileCount * 0.5f;

            for (var i = 0; i < tileCount; i++)
            {
                var tileObject = Instantiate(GetConcealedTilePrefab(), _fieldRoot);

                var localX = startX + i * tileWidth + reference.size.x * 0.5f;
                var localPosition = new Vector3(localX, 0f, localNearZ);
                var rotatedOffset = positionRotation * localPosition;
                var worldPosition = new Vector3(
                    tableBounds.center.x + rotatedOffset.x,
                    TILE_REST_Y,
                    tableBounds.center.z + rotatedOffset.z);

                tileObject.transform.position = worldPosition;
                tileObject.transform.rotation = facingRotation;

                newTiles.Add(tileObject);
            }
        }
        /// <summary>
        /// 牌1枚分の基準サイズを計測する（初回のみ、以後はキャッシュを返す）
        /// </summary>
        private Bounds ResolveReferenceBounds()
        {
            if (_referenceBounds.HasValue)
            {
                return _referenceBounds.Value;
            }

            var tempInstance = Instantiate(GetConcealedTilePrefab(), Vector3.zero, BaseTileRotation);
            var bounds = TileMeshLibrary.MeasureBounds(tempInstance);
            Destroy(tempInstance);

            _referenceBounds = bounds;
            return bounds;
        }
        /// <summary>
        /// 伏せ牌メッシュのプレハブを取得する（初回のみ読み込み、以後はキャッシュを返す）
        /// </summary>
        private GameObject GetConcealedTilePrefab()
        {
            if (_concealedTilePrefab == null)
            {
                _concealedTilePrefab = TileMeshLibrary.LoadPrefab(CONCEALED_TILE_MESH_NAME);
            }

            return _concealedTilePrefab;
        }
    }
}
