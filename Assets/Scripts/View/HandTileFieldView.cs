using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter.HumanHandTiles を購読し、人間プレイヤー自身の手牌を3D牌メッシュで表示する
    /// 牌メッシュは Assets/Resources/Mahjong Complete Set/Mesh 配下のFBXを Resources.Load で読み込む
    /// 牌メッシュの実寸はアセット側の値次第で不明なため、配置・カメラともに
    /// インスタンス化後の実測バウンディングボックスから逆算する（決め打ちの座標を使わない）
    /// </summary>
    public sealed class HandTileFieldView : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 牌メッシュの読み込み元フォルダ（Resources基準の相対パス）
        /// </summary>
        private const string MESH_RESOURCE_ROOT = "Mahjong Complete Set/Mesh/";
        /// <summary>
        /// 牌同士の隙間（牌の幅に対する割合）
        /// </summary>
        private const float TILE_MARGIN_FACTOR = 0.15f;
        /// <summary>
        /// ツモ牌の手前に追加で空ける隙間（牌の幅に対する割合）
        /// </summary>
        private const float DRAWN_TILE_GAP_FACTOR = 0.6f;
        /// <summary>
        /// 手牌が画面の縦方向に占める割合
        /// 小さいほど手牌は画面下側に小さくまとまり、奥の卓面が広く見える（雀魂等の構図に近づける）
        /// </summary>
        private const float ROW_SCREEN_HEIGHT_FRACTION = 0.3f;
        /// <summary>
        /// 手牌が画面の横方向に占める割合
        /// </summary>
        private const float ROW_SCREEN_WIDTH_FRACTION = 0.85f;
        /// <summary>
        /// カメラを手牌からどれだけ高い位置・後方に置くか（度）
        /// </summary>
        private const float CAMERA_ELEVATION_DEGREES = 50f;
        /// <summary>
        /// 手牌を画面中央からどれだけ下にずらして注視するか
        /// 0で画面中央、1で画面下端ぎりぎり（クリッピングを避けるため1未満に収める）
        /// </summary>
        private const float ROW_VERTICAL_OFFSET_FRACTION = 0.55f;
        /// <summary>
        /// シーン上で卓として参照するGameObjectの名前
        /// 見つかった場合はその実測バウンディングボックスから手前の縁・天面の高さを算出する
        /// </summary>
        private const string TABLE_OBJECT_NAME = "table";
        /// <summary>
        /// 卓の奥行きに対して、手前の縁からどれだけ内側に手牌を置くか（卓からはみ出さないための余白）
        /// </summary>
        private const float TABLE_NEAR_EDGE_INSET_FRACTION = 0.15f;
        /// <summary>
        /// 牌を置くY座標（Transform Position.Yの実測値）
        /// tableの外周（縁の木枠）がバウンディングボックスの最高点になり、実際に牌を置くプレイエリア面とは
        /// 一致しないため、バウンディングボックスからの逆算はやめて固定値を使う
        /// Play中に牌をフェルト面に手動で合わせて得た実測値。本物の卓アートに差し替える際に見直す
        /// </summary>
        private const float TILE_REST_Y = 0.20f;
        /// <summary>
        /// 卓が見つからない場合に使うフォールバック位置
        /// </summary>
        private static readonly Vector3 FallbackFieldStart = new(0f, TILE_REST_Y, -1.2f);
        /// <summary>
        /// 牌1枚の姿勢（起こしてカメラ側を向ける）
        /// </summary>
        private static readonly Quaternion TileRotation = Quaternion.Euler(90f, 0f, 180f);


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 手牌データの購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 牌GameObjectの親（手牌が更新されるたびに子をすべて作り直す）
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

            var fieldRootObject = new GameObject("HumanHandField");
            fieldRootObject.transform.SetParent(transform, false);
            _fieldRoot = fieldRootObject.transform;
        }

        private void Start()
        {
            _presenter.HumanHandTiles.Subscribe(OnHumanHandTilesChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド（手牌の描画）
        // ========================================
        /// <summary>
        /// 手牌が更新されるたびに、牌GameObjectを作り直して並べ直す
        /// </summary>
        private void OnHumanHandTilesChanged(IReadOnlyList<TileView> tiles)
        {
            ClearTiles();

            if (tiles == null || tiles.Count == 0)
            {
                return;
            }

            // 1周目: 生成とバウンディングボックス計測だけ行い、列内での相対オフセットを求める
            // （全体の幅が確定してからでないと、基準位置を中心にした中央揃えができない）
            var placements = new List<(GameObject tileObject, Bounds localBounds, float offsetX)>();
            var offsetX = 0f;

            for (var i = 0; i < tiles.Count; i++)
            {
                var tileObject = InstantiateTile(tiles[i], out var localBounds);

                if (tileObject == null)
                {
                    continue;
                }

                // 最後の1枚はツモ牌として少し隙間を空ける
                if (i == tiles.Count - 1)
                {
                    offsetX += localBounds.size.x * DRAWN_TILE_GAP_FACTOR;
                }

                placements.Add((tileObject, localBounds, offsetX));
                offsetX += localBounds.size.x * (1f + TILE_MARGIN_FACTOR);
            }

            if (placements.Count == 0)
            {
                return;
            }

            var fieldStart = ResolveFieldStart();
            var lastPlacement = placements[^1];
            var totalSpan = lastPlacement.offsetX + lastPlacement.localBounds.size.x;
            var startX = fieldStart.x - totalSpan * 0.5f;

            // 2周目: 卓の手前側の中心（fieldStart）を基準に列全体を中央揃えして配置する
            Bounds rowBounds = default;
            var hasRowBounds = false;

            foreach (var (tileObject, localBounds, tileOffsetX) in placements)
            {
                var position = new Vector3(startX + tileOffsetX, fieldStart.y, fieldStart.z);
                tileObject.transform.localPosition = position;

                var placedBounds = new Bounds(position, localBounds.size);

                if (hasRowBounds)
                {
                    rowBounds.Encapsulate(placedBounds);
                }
                else
                {
                    rowBounds = placedBounds;
                    hasRowBounds = true;
                }

                _activeTileObjects.Add(tileObject);
            }

            FrameCamera(rowBounds);
        }
        /// <summary>
        /// 牌1枚をロードして生成する
        /// 姿勢を確定させた上で、原点に置いた状態のバウンディングボックスを計測して返す
        /// （_fieldRoot はワールド原点にあるため、この時点のワールドバウンディングボックスがそのままローカル座標として使える）
        /// </summary>
        /// <param name="tile">生成する牌</param>
        /// <param name="localBounds">計測したバウンディングボックス（生成失敗時はdefault）</param>
        private GameObject InstantiateTile(TileView tile, out Bounds localBounds)
        {
            var resourcePath = MESH_RESOURCE_ROOT + ResolveMeshFileName(tile);
            var prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab == null)
            {
                Debug.LogError($"牌メッシュが見つかりません: {resourcePath}");
                localBounds = default;
                return null;
            }

            var tileObject = Instantiate(prefab, _fieldRoot);
            tileObject.transform.localPosition = Vector3.zero;
            tileObject.transform.localRotation = TileRotation;

            localBounds = MeasureBounds(tileObject);
            return tileObject;
        }
        /// <summary>
        /// 手牌の基準位置を決める
        /// X・Zはシーンに "table" という名前のGameObjectがあれば、その実測バウンディングボックスから
        /// 手前側（カメラ側）の縁を算出して使う（決め打ちの座標に頼らないため）
        /// Yは卓の外周（縁の木枠）とプレイエリア面の高さが一致しないため、TILE_REST_Yの固定値を使う
        /// 見つからない場合はフォールバック位置を使う
        /// </summary>
        private static Vector3 ResolveFieldStart()
        {
            var tableObject = GameObject.Find(TABLE_OBJECT_NAME);

            if (tableObject == null)
            {
                return FallbackFieldStart;
            }

            var tableBounds = MeasureBounds(tableObject);
            var nearEdgeZ = tableBounds.min.z + tableBounds.size.z * TABLE_NEAR_EDGE_INSET_FRACTION;

            return new Vector3(tableBounds.center.x, TILE_REST_Y, nearEdgeZ);
        }
        /// <summary>
        /// GameObject配下の全Rendererを合成したバウンディングボックスを計測する
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Rendererが1つも無い場合</exception>
        private static Bounds MeasureBounds(GameObject tileObject)
        {
            var renderers = tileObject.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                throw new System.InvalidOperationException($"牌メッシュにRendererがありません: {tileObject.name}");
            }

            var bounds = renderers[0].bounds;

            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
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
        /// MainCameraを手牌の斜め後方・上方に配置する
        /// 手牌の中心を直視すると画面中央に来てしまうため、カメラの向きだけを
        /// ROW_VERTICAL_OFFSET_FRACTION分だけ浅く（水平に近く）することで、
        /// 手牌を画面下側に寄せ、奥の卓面が画面上部に見える構図にする
        /// シーンファイルは編集しないため、実行時にTransformだけを調整する
        /// </summary>
        private static void FrameCamera(Bounds rowBounds)
        {
            var camera = Camera.main;

            if (camera == null)
            {
                return;
            }

            var verticalFovRad = camera.fieldOfView * Mathf.Deg2Rad;
            var horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * camera.aspect);

            var halfWidth = rowBounds.extents.x;
            var halfHeight = Mathf.Max(rowBounds.extents.y, rowBounds.extents.z);

            var distanceForWidth = (halfWidth / ROW_SCREEN_WIDTH_FRACTION) / Mathf.Tan(horizontalFovRad * 0.5f);
            var distanceForHeight = (halfHeight / ROW_SCREEN_HEIGHT_FRACTION) / Mathf.Tan(verticalFovRad * 0.5f);
            var distance = Mathf.Max(distanceForWidth, distanceForHeight, 1f);

            // カメラの位置は「手牌を真正面に見る」角度（elevationRad）を基準に決める
            var elevationRad = CAMERA_ELEVATION_DEGREES * Mathf.Deg2Rad;
            var cameraOffset = new Vector3(0f, Mathf.Sin(elevationRad) * distance, -Mathf.Cos(elevationRad) * distance);
            camera.transform.position = rowBounds.center + cameraOffset;

            // 向きだけは水平に近づけ（浅い角度にし）、手牌が画面下側に来るようにする
            var lookPitchRad = elevationRad - ROW_VERTICAL_OFFSET_FRACTION * (verticalFovRad * 0.5f);
            var forward = new Vector3(0f, -Mathf.Sin(lookPitchRad), Mathf.Cos(lookPitchRad));
            camera.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }


        // ========================================
        // プライベートメソッド（牌メッシュの解決）
        // ========================================
        /// <summary>
        /// TileView からリソースファイル名（拡張子なし）を組み立てる
        /// 例: 萬子5(赤) → "pmanzu_5r", 東 → "pjihai_ton"
        /// </summary>
        /// <exception cref="System.InvalidOperationException">未対応のSuit/Honorの場合</exception>
        private static string ResolveMeshFileName(TileView tile)
        {
            if (tile.Suit == TileSuitView.Jihai)
            {
                return tile.Honor switch
                {
                    HonorTileView.East => "pjihai_ton",
                    HonorTileView.South => "pjihai_nan",
                    HonorTileView.West => "pjihai_sha",
                    HonorTileView.North => "pjihai_pe",
                    HonorTileView.Haku => "pjihai_haku",
                    HonorTileView.Hatsu => "pjihai_hatsu",
                    HonorTileView.Chun => "pjihai_chun",
                    _ => throw new System.InvalidOperationException($"字牌のHonorが不正です: {tile.Honor}"),
                };
            }

            var suitPrefix = tile.Suit switch
            {
                TileSuitView.Manzu => "pmanzu",
                TileSuitView.Pinzu => "ppinzu",
                TileSuitView.Souzu => "psouzu",
                _ => throw new System.InvalidOperationException($"未対応のTileSuitViewです: {tile.Suit}"),
            };

            var numberSuffix = tile.IsRed ? "5r" : tile.Number.ToString();
            return $"{suitPrefix}_{numberSuffix}";
        }
    }
}
