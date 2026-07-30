using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.PlayerMelds を購読し、全プレイヤーの副露を3D牌メッシュで卓上に表示する
    /// PlayerMeldsは自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 「自分（offset=0）の並べ方」を基準に、卓の中心を軸に90度ずつ回転させて他の席の配置を求める
    /// 副露は麻雀では公開情報のため、自分の分（offset=0）も他家と同じように卓上に置く
    /// 牌は河と同じく寝かせて置き、鳴いた牌1枚だけを横向きにして鳴いた相手を表す
    /// 面子は席の右端から左へ向かって並べる（実際の卓上と同じ。先に鳴いた面子は以後動かない）
    /// </summary>
    public sealed class MeldFieldView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 面子の中での牌同士の隙間（牌の幅に対する割合）
        /// </summary>
        private const float TILE_MARGIN_FACTOR = 0.05f;
        /// <summary>
        /// 面子同士の隙間（牌の幅に対する割合）
        /// 面子の区切りが分かるよう、牌同士の隙間より広くとる
        /// </summary>
        private const float MELD_MARGIN_FACTOR = 0.5f;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 副露データの購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 牌GameObjectの親
        /// </summary>
        private Transform _fieldRoot;
        /// <summary>
        /// 寝かせた牌1枚分の基準サイズ（初回計測後はキャッシュする）
        /// 全種類の牌でほぼ同じ実寸のため、1枚から計測した値を使い回す
        /// </summary>
        private Bounds? _referenceTileBounds;
        /// <summary>
        /// 席（自分から見た相対位置）ごとに配置済みの牌GameObject
        /// </summary>
        private readonly Dictionary<int, List<GameObject>> _seatTileObjects = new();
        /// <summary>
        /// 席（自分から見た相対位置）ごとの直近の副露の構成
        /// 変化した席だけ作り直すために保持する
        /// </summary>
        private readonly Dictionary<int, MeldRowSignature> _seatSignatures = new();


        // ========================================
        // 起動
        // ========================================
        private void Awake()
        {
            _presenter = GetComponent<GamePresenter>();

            var fieldRootObject = new GameObject("MeldField");
            fieldRootObject.transform.SetParent(transform, false);
            _fieldRoot = fieldRootObject.transform;
        }

        private void Start()
        {
            _presenter.PlayerMelds.Subscribe(OnPlayerMeldsChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（副露の描画）
        // ========================================
        /// <summary>
        /// 副露が更新されるたびに、構成が変わった席だけ作り直す
        /// </summary>
        private void OnPlayerMeldsChanged(IReadOnlyList<IReadOnlyList<MeldView>> playerMelds)
        {
            if (playerMelds == null)
            {
                return;
            }

            for (var offset = 0; offset < playerMelds.Count; offset++)
            {
                var melds = playerMelds[offset];
                var signature = MeldRowSignature.FromMelds(melds);

                if (_seatSignatures.TryGetValue(offset, out var previous) && previous.Equals(signature))
                {
                    continue;
                }

                RebuildSeatMelds(melds, offset);
                _seatSignatures[offset] = signature;
            }
        }
        /// <summary>
        /// 1人分の副露を作り直す
        /// </summary>
        /// <param name="melds">その席の副露（鳴いた順）</param>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        private void RebuildSeatMelds(IReadOnlyList<MeldView> melds, int offset)
        {
            if (_seatTileObjects.TryGetValue(offset, out var existingTiles))
            {
                foreach (var tileObject in existingTiles)
                {
                    Destroy(tileObject);
                }
            }

            var newTiles = new List<GameObject>();
            _seatTileObjects[offset] = newTiles;

            if (melds.Count == 0)
            {
                return;
            }

            // 基準サイズは実際に並べる牌から計測する（牌の種類による実寸の差を持ち込まないため）
            var firstTilePrefab = TileMeshLibrary.LoadPrefab(melds[0].Tiles[0]);

            if (firstTilePrefab == null)
            {
                return;
            }

            var reference = ResolveReferenceTileBounds(firstTilePrefab);
            var rightEdgeX = reference.size.x * TableLayout.MELD_ROW_RIGHT_EDGE_IN_TILES;

            // 先に鳴いた面子を右端に置き、後から鳴いた面子を左へ足していく
            foreach (var meld in melds)
            {
                rightEdgeX -= PlaceMeld(meld, offset, rightEdgeX, reference, newTiles);
                rightEdgeX -= reference.size.x * MELD_MARGIN_FACTOR;
            }
        }
        /// <summary>
        /// 面子1組を、右端の座標を基準に左から順に並べる
        /// </summary>
        /// <param name="meld">並べる面子</param>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        /// <param name="rightEdgeX">この面子の右端の、席の座標系でのX座標</param>
        /// <param name="reference">寝かせた牌1枚分の基準サイズ</param>
        /// <param name="placedTiles">配置した牌GameObjectの追加先</param>
        /// <returns>この面子が占めた幅</returns>
        private float PlaceMeld(MeldView meld, int offset, float rightEdgeX, Bounds reference, List<GameObject> placedTiles)
        {
            var meldWidth = 0.0f;

            for (var index = 0; index < meld.Tiles.Count; index++)
            {
                meldWidth += ResolveTileWidth(index == meld.RotatedTileIndex, reference);
            }

            var tileLeftX = rightEdgeX - meldWidth;

            for (var index = 0; index < meld.Tiles.Count; index++)
            {
                var isRotated = index == meld.RotatedTileIndex;
                var tileWidth = ResolveTileWidth(isRotated, reference);
                var tileObject = PlaceMeldTile(meld.Tiles[index], isRotated, tileLeftX + tileWidth * 0.5f, offset, reference);

                if (tileObject != null)
                {
                    placedTiles.Add(tileObject);
                }

                tileLeftX += tileWidth;
            }

            return meldWidth;
        }
        /// <summary>
        /// 牌1枚を、面子の中でのX座標に配置する
        /// </summary>
        /// <param name="tile">配置する牌</param>
        /// <param name="isRotated">横向きに置くかどうか</param>
        /// <param name="centerX">牌の中心の、席の座標系でのX座標</param>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        /// <param name="reference">寝かせた牌1枚分の基準サイズ</param>
        /// <returns>配置した牌のGameObject。メッシュが読み込めない場合はnull</returns>
        private GameObject PlaceMeldTile(TileView tile, bool isRotated, float centerX, int offset, Bounds reference)
        {
            var prefab = TileMeshLibrary.LoadPrefab(tile);

            if (prefab == null)
            {
                return null;
            }

            // 横向きの牌は奥行きと幅が入れ替わるため、中心を揃えると手前の辺がずれてしまう
            // 席から見て手前側の辺が他の牌と揃うようZをずらす
            var rotatedDepthOffset = isRotated ? -(reference.size.z - reference.size.x) * 0.5f : 0.0f;
            var localPosition = new Vector3(
                centerX,
                TableLayout.SURFACE_Y - reference.min.y,
                -TableLayout.CONCEALED_HAND_DISTANCE_FROM_CENTER + rotatedDepthOffset);

            var facingRotation = isRotated
                ? TableLayout.GetFlatTileRotation(offset) * Quaternion.Euler(0.0f, 90.0f, 0.0f)
                : TableLayout.GetFlatTileRotation(offset);

            var tileObject = Instantiate(prefab, _fieldRoot);
            tileObject.transform.SetPositionAndRotation(TableLayout.ToWorldPosition(localPosition, offset), facingRotation);
            return tileObject;
        }
        /// <summary>
        /// 牌1枚が列の中で占める幅を返す（横向きの牌は奥行きが幅になる）
        /// </summary>
        private static float ResolveTileWidth(bool isRotated, Bounds reference)
        {
            var size = isRotated ? reference.size.z : reference.size.x;
            return size * (1.0f + TILE_MARGIN_FACTOR);
        }
        /// <summary>
        /// 寝かせた牌1枚分の基準サイズを計測する（初回のみ、以後はキャッシュを返す）
        /// FBXのデフォルト姿勢が寝かせた状態のため、回転はかけずに計測する
        /// （河と同じ姿勢。sizeのXが幅、Zが奥行きになる）
        /// </summary>
        private Bounds ResolveReferenceTileBounds(GameObject prefab)
        {
            if (_referenceTileBounds.HasValue)
            {
                return _referenceTileBounds.Value;
            }

            _referenceTileBounds = TileMeshLibrary.MeasurePrefabBounds(prefab, Quaternion.identity);
            return _referenceTileBounds.Value;
        }


        // ========================================
        // 入れ子の型
        // ========================================
        /// <summary>
        /// 1席分の副露の構成を表す値
        /// 副露は鳴いたときにしか変わらないため、面子数と牌の合計枚数（加槓で増える）が
        /// どちらも同じなら並びも同じとみなして作り直しを省く
        /// </summary>
        private readonly struct MeldRowSignature
        {
            /// <summary>
            /// 面子の数
            /// </summary>
            private readonly int _meldCount;
            /// <summary>
            /// 牌の合計枚数
            /// </summary>
            private readonly int _tileCount;

            /// <summary>
            /// 副露の構成を表す値を生成する
            /// </summary>
            private MeldRowSignature(int meldCount, int tileCount)
            {
                _meldCount = meldCount;
                _tileCount = tileCount;
            }

            /// <summary>
            /// 副露のリストから構成を表す値を求める
            /// </summary>
            public static MeldRowSignature FromMelds(IReadOnlyList<MeldView> melds)
            {
                var tileCount = 0;

                foreach (var meld in melds)
                {
                    tileCount += meld.Tiles.Count;
                }

                return new MeldRowSignature(melds.Count, tileCount);
            }
            /// <summary>
            /// 構成が同じかどうかを返す
            /// </summary>
            public bool Equals(MeldRowSignature other)
            {
                return _meldCount == other._meldCount && _tileCount == other._tileCount;
            }
        }
    }
}
