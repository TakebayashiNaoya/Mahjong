using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.ConcealedHands を購読し、他家（自分以外）の伏せ手牌を3D牌メッシュで卓上に表示する
    /// 他家の手牌の中身は公開情報ではないため、枚数分だけ絵柄の無い牌（pai.fbx）を並べる
    /// ConcealedHandsは自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 「自分（offset=0）の並べ方」を基準に、卓の中心を軸に90度ずつ回転させて他の席の配置を求める
    /// （offset=0は自分自身なので、2Dアイコンで表示済みのこのコンポーネントではスキップする）
    /// 門前牌の並びは門前の枚数だけで決まるため、ツモ・打牌のたびに増減するツモ牌は別の1枚として扱い、
    /// 門前牌が動かないようにする
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
        /// 門前牌の右端からツモ牌までに空ける隙間（牌の幅に対する割合）
        /// 自分の手牌アイコン（InGameView）と同じく、牌1枚分空けてツモ牌と分かるようにする
        /// </summary>
        private const float DRAWN_TILE_GAP_FACTOR = 1.0f;
        /// <summary>
        /// 牌1枚の基準姿勢（起こして正面を向ける）
        /// 削除済みのHandTileFieldViewで使っていたTileRotationと同じ
        /// </summary>
        private static readonly Quaternion BaseTileRotation = Quaternion.Euler(90.0f, 0.0f, 180.0f);
        /// <summary>
        /// 基準サイズの計測用に牌を一時的に配置する座標
        /// 計測用の牌は同フレーム内では破棄されない（Destroyはフレーム末に効く）ため、
        /// 卓の上に置くと1フレームだけ映り込む。それを避けるために原点から大きく離す
        /// </summary>
        private static readonly Vector3 MeasurePosition = new(10000.0f, 10000.0f, 10000.0f);


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
        /// 牌1枚分の基準サイズ（初回計測後はキャッシュする）
        /// ピボットを原点としたときの大きさ・位置を表す（Y方向は底面までの距離を求めるのに使う）
        /// </summary>
        private Bounds? _referenceBounds;
        /// <summary>
        /// 席（自分から見た相対位置）ごとの表示状態
        /// </summary>
        private readonly Dictionary<int, SeatHand> _seatHands = new();


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
            _presenter.ConcealedHands.Subscribe(OnConcealedHandsChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（伏せ手牌の描画）
        // ========================================
        /// <summary>
        /// 伏せ手牌が更新されるたびに、変化した席の変化した部分だけを置き直す
        /// </summary>
        private void OnConcealedHandsChanged(IReadOnlyList<ConcealedHandView> hands)
        {
            if (hands == null || hands.Count == 0)
            {
                return;
            }

            var prefab = GetConcealedTilePrefab();

            if (prefab == null)
            {
                return;
            }

            var tableCenter = TableLayout.ResolveCenter();

            // offset=0は自分自身（2Dアイコンで表示済み）のためスキップする
            for (var offset = 1; offset < hands.Count; offset++)
            {
                UpdateSeatHand(hands[offset], offset, prefab, tableCenter);
            }
        }
        /// <summary>
        /// 1人分の伏せ手牌を最新の状態に合わせる
        /// 門前の枚数が変わらない限り門前牌には触れず、ツモ牌の1枚だけを足し引きする
        /// </summary>
        /// <param name="hand">その席の伏せ手牌</param>
        /// <param name="offset">自分から見た相対位置（1=下家, 2=対面, 3=上家）</param>
        /// <param name="prefab">伏せ牌メッシュのプレハブ</param>
        /// <param name="tableCenter">卓の中心のワールド座標</param>
        private void UpdateSeatHand(ConcealedHandView hand, int offset, GameObject prefab, Vector3 tableCenter)
        {
            if (!_seatHands.TryGetValue(offset, out var seat))
            {
                seat = new SeatHand();
                _seatHands[offset] = seat;
            }

            var isConcealedRowChanged = seat.ConcealedTileCount != hand.ConcealedTileCount;

            if (isConcealedRowChanged)
            {
                RebuildConcealedRow(seat, hand.ConcealedTileCount, offset, prefab, tableCenter);

                // 門前牌の並びが変わるとツモ牌の位置もずれるため、いったん取り除いて置き直す
                DestroyDrawnTile(seat);
            }

            if (hand.HasDrawnTile == (seat.DrawnTile != null))
            {
                return;
            }

            if (!hand.HasDrawnTile)
            {
                DestroyDrawnTile(seat);
                return;
            }

            // 門前牌の右端から隙間を1つ空けた位置に置く
            var drawnTileIndex = seat.ConcealedTileCount + DRAWN_TILE_GAP_FACTOR;
            seat.DrawnTile = PlaceTile(drawnTileIndex, seat.ConcealedTileCount, offset, prefab, tableCenter);
        }
        /// <summary>
        /// 門前牌の列を作り直す
        /// </summary>
        /// <param name="seat">その席の表示状態</param>
        /// <param name="concealedTileCount">門前牌の枚数</param>
        /// <param name="offset">自分から見た相対位置（1=下家, 2=対面, 3=上家）</param>
        /// <param name="prefab">伏せ牌メッシュのプレハブ</param>
        /// <param name="tableCenter">卓の中心のワールド座標</param>
        private void RebuildConcealedRow(SeatHand seat, int concealedTileCount, int offset, GameObject prefab, Vector3 tableCenter)
        {
            foreach (var tileObject in seat.ConcealedTiles)
            {
                Destroy(tileObject);
            }

            seat.ConcealedTiles.Clear();
            seat.ConcealedTileCount = concealedTileCount;

            for (var index = 0; index < concealedTileCount; index++)
            {
                seat.ConcealedTiles.Add(PlaceTile(index, concealedTileCount, offset, prefab, tableCenter));
            }
        }
        /// <summary>
        /// 牌1枚を、列の中での位置から求めたワールド座標に配置する
        /// 「自分（offset=0）が手前で正面を向いている」座標系で位置を組み立てた後、
        /// 卓の中心を軸に90度×offset回転させて、その席の位置・向きに変換する
        /// </summary>
        /// <param name="indexInRow">列の左端から数えた位置（ツモ牌のように隙間を空ける場合は小数もとる）</param>
        /// <param name="concealedTileCount">列の中央揃えの基準にする門前牌の枚数</param>
        /// <param name="offset">自分から見た相対位置（1=下家, 2=対面, 3=上家）</param>
        /// <param name="prefab">伏せ牌メッシュのプレハブ</param>
        /// <param name="tableCenter">卓の中心のワールド座標</param>
        /// <returns>配置した牌のGameObject</returns>
        private GameObject PlaceTile(float indexInRow, int concealedTileCount, int offset, GameObject prefab, Vector3 tableCenter)
        {
            var reference = ResolveReferenceBounds(prefab);
            var tileWidth = reference.size.x * (1.0f + TILE_MARGIN_FACTOR);

            // 中央揃えの基準を門前牌の枚数だけにすることで、ツモ牌の有無で門前牌が左右にずれないようにする
            var startX = -tileWidth * concealedTileCount * 0.5f;
            var localX = startX + indexInRow * tileWidth + reference.size.x * 0.5f;

            var positionRotation = Quaternion.Euler(0.0f, 90.0f * offset, 0.0f);
            var localPosition = new Vector3(localX, 0.0f, -TableLayout.CONCEALED_HAND_DISTANCE_FROM_CENTER);
            var rotatedOffset = positionRotation * localPosition;

            // ピボットが牌の中心にあるため、底面が卓の設置面に揃うようYを補正する
            var worldPosition = new Vector3(
                tableCenter.x + rotatedOffset.x,
                TableLayout.SURFACE_Y - reference.min.y,
                tableCenter.z + rotatedOffset.z);

            var tileObject = Instantiate(prefab, _fieldRoot);
            tileObject.transform.SetPositionAndRotation(worldPosition, positionRotation * BaseTileRotation);
            return tileObject;
        }
        /// <summary>
        /// ツモ牌のGameObjectを破棄する（無ければ何もしない）
        /// </summary>
        private void DestroyDrawnTile(SeatHand seat)
        {
            if (seat.DrawnTile == null)
            {
                return;
            }

            Destroy(seat.DrawnTile);
            seat.DrawnTile = null;
        }
        /// <summary>
        /// 牌1枚分の基準サイズを、ピボットを原点とした値で計測する（初回のみ、以後はキャッシュを返す）
        /// </summary>
        private Bounds ResolveReferenceBounds(GameObject prefab)
        {
            if (_referenceBounds.HasValue)
            {
                return _referenceBounds.Value;
            }

            var tempInstance = Instantiate(prefab, MeasurePosition, BaseTileRotation);
            var bounds = TileMeshLibrary.MeasureBounds(tempInstance);
            Destroy(tempInstance);

            // MeasureBoundsはワールド座標基準のため、計測用に離した分を引いてピボット基準に直す
            _referenceBounds = new Bounds(bounds.center - MeasurePosition, bounds.size);
            return _referenceBounds.Value;
        }
        /// <summary>
        /// 伏せ牌メッシュのプレハブを取得する（初回のみ読み込み、以後はキャッシュを返す）
        /// 読み込めない場合はnullを返す（TileMeshLibrary側でエラーログを出す）
        /// </summary>
        private GameObject GetConcealedTilePrefab()
        {
            if (_concealedTilePrefab == null)
            {
                _concealedTilePrefab = TileMeshLibrary.LoadPrefab(CONCEALED_TILE_MESH_NAME);
            }

            return _concealedTilePrefab;
        }


        // ========================================
        // 入れ子の型
        // ========================================
        /// <summary>
        /// 1席分の伏せ手牌の表示状態
        /// 門前牌とツモ牌を分けて持つことで、ツモ・打牌のたびに増減するのはツモ牌の1枚だけになる
        /// </summary>
        private sealed class SeatHand
        {
            /// <summary>
            /// 配置済みの門前牌
            /// </summary>
            public List<GameObject> ConcealedTiles { get; } = new();
            /// <summary>
            /// 配置済みのツモ牌（持っていない場合はnull）
            /// </summary>
            public GameObject DrawnTile { get; set; }
            /// <summary>
            /// 現在配置している門前牌の枚数（未配置は-1）
            /// </summary>
            public int ConcealedTileCount { get; set; } = -1;
        }
    }
}
