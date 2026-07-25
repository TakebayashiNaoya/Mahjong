using System.Collections.Generic;
using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 牌の3DメッシュをオフスクリーンでレンダリングしてSpriteを生成し、種類ごとにキャッシュする
    /// 2D用の牌画像を別途用意しなくても、既存のMahjong Complete Setアセットと見た目が一致するアイコンが得られる
    /// </summary>
    public static class TileIconCache
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 牌メッシュの読み込み元フォルダ（Resources基準の相対パス）
        /// </summary>
        private const string MESH_RESOURCE_ROOT = "Mahjong Complete Set/Mesh/";
        /// <summary>
        /// 事前に焼き込んだアイコンの読み込み元フォルダ（Resources基準の相対パス）
        /// TileIconBaker（Editor専用）が書き出す先と一致させる
        /// </summary>
        private const string BAKED_ICON_RESOURCE_ROOT = "TileIcons/";
        /// <summary>
        /// 生成するアイコンの一辺のピクセル数
        /// </summary>
        private const int ICON_SIZE = 128;
        /// <summary>
        /// 撮影用に牌を一時的に配置する座標
        /// シーン上の他オブジェクトと絶対に重ならないよう、原点から大きく離す
        /// </summary>
        private static readonly Vector3 OffscreenPosition = new(10000f, 10000f, 10000f);
        /// <summary>
        /// 撮影カメラを牌の天面からどれだけ高い位置に置くか
        /// </summary>
        private const float CAMERA_HEIGHT_ABOVE_TILE = 5f;
        /// <summary>
        /// フレームに収める際の余白倍率（1.0だと牌の端がぎりぎりになる）
        /// </summary>
        private const float FRAME_MARGIN = 1.1f;
        /// <summary>
        /// 撮影カメラの姿勢（真下を向く）
        /// Z=180は、牌の絵柄が上下逆さまに映るのを補正するための調整値
        /// </summary>
        private static readonly Quaternion CameraRotation = Quaternion.Euler(90f, 0f, 180f);
        /// <summary>
        /// マテリアルの光沢（Smoothness）プロパティID
        /// </summary>
        private static readonly int SmoothnessPropertyId = Shader.PropertyToID("_Smoothness");
        /// <summary>
        /// マテリアルの金属質（Metallic）プロパティID
        /// </summary>
        private static readonly int MetallicPropertyId = Shader.PropertyToID("_Metallic");


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 牌の種類ごとに生成済みのSpriteを保持するキャッシュ
        /// </summary>
        private static readonly Dictionary<string, Sprite> _cache = new();


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 牌の2DアイコンをSpriteとして取得する
        /// 焼き込み済みのアイコン（TileIconBakerが生成したもの）があればそれを使い、
        /// 無ければ3Dメッシュをオフスクリーンで撮影してその場で生成する。以後はキャッシュを返す
        /// </summary>
        public static Sprite GetSprite(TileView tile)
        {
            var key = ResolveMeshFileName(tile);

            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(BAKED_ICON_RESOURCE_ROOT + key);

            if (sprite == null)
            {
                var texture = RenderIconTexture(key);
                sprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, ICON_SIZE, ICON_SIZE), new Vector2(0.5f, 0.5f))
                    : null;
            }

            _cache[key] = sprite;
            return sprite;
        }
        /// <summary>
        /// 牌メッシュをオフスクリーンで撮影し、Texture2Dとして返す
        /// TileIconBaker（Editor専用）からも、Assetsへ焼き込むために直接呼ばれる
        /// </summary>
        public static Texture2D RenderIconTexture(string meshFileName)
        {
            var prefab = Resources.Load<GameObject>(MESH_RESOURCE_ROOT + meshFileName);

            if (prefab == null)
            {
                Debug.LogError($"牌メッシュが見つかりません: {meshFileName}");
                return null;
            }

            // FBXのデフォルト姿勢は寝かせて面を上に向けた状態のため、回転はかけない
            var tileInstance = Object.Instantiate(prefab, OffscreenPosition, Quaternion.identity);
            FlattenMaterials(tileInstance);
            var bounds = MeasureBounds(tileInstance);

            var cameraObject = new GameObject("TileIconCamera");
            cameraObject.transform.position = new Vector3(bounds.center.x, bounds.max.y + CAMERA_HEIGHT_ABOVE_TILE, bounds.center.z);
            cameraObject.transform.rotation = CameraRotation;

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * FRAME_MARGIN;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = CAMERA_HEIGHT_ABOVE_TILE + bounds.size.y + 1f;

            var renderTexture = new RenderTexture(ICON_SIZE, ICON_SIZE, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            var texture = new Texture2D(ICON_SIZE, ICON_SIZE, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0f, 0f, ICON_SIZE, ICON_SIZE), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;

            camera.targetTexture = null;

            // DestroyImmediateを使う理由: Object.Destroyは同フレーム内では即座に消えないため、
            // 複数の牌を連続して撮影すると直前の牌が映り込んでしまう。
            // またEditモード（TileIconBakerからの呼び出し）ではDestroyは使えず、DestroyImmediateが必須
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(tileInstance);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);

            return texture;
        }


        // ========================================
        // プライベートメソッド（アイコン生成）
        // ========================================
        /// <summary>
        /// 光沢（Smoothness・Metallic）を0にして、3Dのハイライトが映り込まない平坦な見た目にする
        /// アイコンとしての表示用に一時生成したインスタンスにのみ適用するため、
        /// 共有マテリアルアセットには影響しない（Renderer.materialsはアクセス時に自動でインスタンス化される）
        /// </summary>
        private static void FlattenMaterials(GameObject tileObject)
        {
            foreach (var renderer in tileObject.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty(SmoothnessPropertyId))
                    {
                        material.SetFloat(SmoothnessPropertyId, 0f);
                    }

                    if (material.HasProperty(MetallicPropertyId))
                    {
                        material.SetFloat(MetallicPropertyId, 0f);
                    }
                }
            }
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
