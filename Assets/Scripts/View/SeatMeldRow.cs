using System.Collections.Generic;
using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 1席分の副露を、席ローカル座標で卓上に並べる
    /// 牌は河と同じく寝かせて置き、鳴いた牌1枚だけを横向きにして鳴いた相手を表す
    /// 面子は席の右端から左へ向かって並べる（実際の卓上と同じ。先に鳴いた面子は以後動かない）
    /// </summary>
    public sealed class SeatMeldRow
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
        /// <summary>
        /// 横向きに置く牌に加える回転
        /// </summary>
        private static readonly Quaternion RotatedTileRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 牌を置く席のルート
        /// </summary>
        private readonly Transform _seatRoot;
        /// <summary>
        /// 配置済みの牌GameObject
        /// </summary>
        private readonly List<GameObject> _tileObjects = new();
        /// <summary>
        /// 直近の副露の構成
        /// 構成が変わったときだけ作り直すために保持する
        /// </summary>
        private MeldRowSignature _signature = MeldRowSignature.None;
        /// <summary>
        /// 寝かせた牌1枚分の基準サイズ（初回計測後はキャッシュする）
        /// 全種類の牌でほぼ同じ実寸のため、1枚から計測した値を使い回す
        /// </summary>
        private Bounds? _referenceTileBounds;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 副露の表示を生成する
        /// </summary>
        /// <param name="seatRoot">牌を置く席のルート</param>
        public SeatMeldRow(Transform seatRoot)
        {
            _seatRoot = seatRoot;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 副露を最新の状態に合わせる（構成が変わっていなければ何もしない）
        /// </summary>
        /// <param name="melds">その席の副露（鳴いた順）</param>
        public void UpdateTiles(IReadOnlyList<MeldView> melds)
        {
            var signature = MeldRowSignature.FromMelds(melds);

            if (_signature.Equals(signature))
            {
                return;
            }

            RebuildTiles(melds);
            _signature = signature;
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 副露をすべて作り直す
        /// </summary>
        private void RebuildTiles(IReadOnlyList<MeldView> melds)
        {
            foreach (var tileObject in _tileObjects)
            {
                Object.Destroy(tileObject);
            }

            _tileObjects.Clear();

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
                rightEdgeX -= PlaceMeld(meld, rightEdgeX, reference);
                rightEdgeX -= reference.size.x * MELD_MARGIN_FACTOR;
            }
        }
        /// <summary>
        /// 面子1組を、右端の座標を基準に左から順に並べる
        /// </summary>
        /// <param name="meld">並べる面子</param>
        /// <param name="rightEdgeX">この面子の右端のX座標</param>
        /// <param name="reference">寝かせた牌1枚分の基準サイズ</param>
        /// <returns>この面子が占めた幅</returns>
        private float PlaceMeld(MeldView meld, float rightEdgeX, Bounds reference)
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
                var tileObject = PlaceTile(meld.Tiles[index], isRotated, tileLeftX + tileWidth * 0.5f, reference);

                if (tileObject != null)
                {
                    _tileObjects.Add(tileObject);
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
        /// <param name="centerX">牌の中心のX座標</param>
        /// <param name="reference">寝かせた牌1枚分の基準サイズ</param>
        /// <returns>配置した牌のGameObject。メッシュが読み込めない場合はnull</returns>
        private GameObject PlaceTile(TileView tile, bool isRotated, float centerX, Bounds reference)
        {
            var prefab = TileMeshLibrary.LoadPrefab(tile);

            if (prefab == null)
            {
                return null;
            }

            // 横向きの牌は奥行きと幅が入れ替わるため、中心を揃えると手前の辺がずれてしまう
            // 席から見て手前側の辺が他の牌と揃うようZをずらす
            var rotatedDepthOffset = isRotated ? -(reference.size.z - reference.size.x) * 0.5f : 0.0f;

            // 牌の底面が卓の設置面に揃うようYを補正する
            var localPosition = new Vector3(
                centerX,
                -reference.min.y,
                -TableLayout.CONCEALED_HAND_DISTANCE_FROM_CENTER + rotatedDepthOffset);

            var localRotation = isRotated
                ? TableLayout.FlatTileRotation * RotatedTileRotation
                : TableLayout.FlatTileRotation;

            var tileObject = Object.Instantiate(prefab, _seatRoot);
            tileObject.transform.SetLocalPositionAndRotation(localPosition, localRotation);
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


        // ========================================
        // 入れ子の型
        // ========================================
        /// <summary>
        /// 副露の構成を表す値
        /// 副露は鳴いたときにしか変わらないため、面子数と牌の合計枚数（加槓で増える）が
        /// どちらも同じなら並びも同じとみなして作り直しを省く
        /// </summary>
        private readonly struct MeldRowSignature
        {
            /// <summary>
            /// まだ一度も並べていないことを表す値
            /// </summary>
            public static readonly MeldRowSignature None = new(-1, -1);

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
