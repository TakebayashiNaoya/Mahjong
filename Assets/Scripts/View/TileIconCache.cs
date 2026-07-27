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
        /// 事前に焼き込んだアイコンの読み込み元フォルダ（Resources基準の相対パス）
        /// TileIconBaker（Editor専用）が書き出す先と一致させる
        /// </summary>
        private const string BAKED_ICON_RESOURCE_ROOT = "TileIcons/";
        /// <summary>
        /// 生成するアイコンの高さのピクセル数
        /// 牌は正方形ではないため、幅は牌の実測アスペクト比（幅÷奥行き）から都度算出する
        /// （正方形に固定すると、短い方の軸に余白ができてしまうため）
        /// </summary>
        private const int ICON_HEIGHT = 128;
        /// <summary>
        /// 撮影用に牌を一時的に配置する座標
        /// シーン上の他オブジェクトと絶対に重ならないよう、原点から大きく離す
        /// </summary>
        private static readonly Vector3 OffscreenPosition = new(10000.0f, 10000.0f, 10000.0f);
        /// <summary>
        /// 撮影カメラを牌の天面からどれだけ高い位置に置くか
        /// </summary>
        private const float CAMERA_HEIGHT_ABOVE_TILE = 5.0f;
        /// <summary>
        /// 撮影カメラの姿勢（真下を向く）
        /// Z=180は、牌の絵柄が上下逆さまに映るのを補正するための調整値
        /// </summary>
        private static readonly Quaternion CameraRotation = Quaternion.Euler(90.0f, 0.0f, 180.0f);
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
            var key = TileMeshLibrary.ResolveFileName(tile);

            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(BAKED_ICON_RESOURCE_ROOT + key);

            if (sprite == null)
            {
                var texture = RenderIconTexture(key);
                sprite = texture != null
                    ? Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f))
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
            var prefab = TileMeshLibrary.LoadPrefab(meshFileName);

            if (prefab == null)
            {
                return null;
            }

            // FBXのデフォルト姿勢は寝かせて面を上に向けた状態のため、回転はかけない
            var tileInstance = Object.Instantiate(prefab, OffscreenPosition, Quaternion.identity);
            FlattenMaterials(tileInstance);
            var bounds = TileMeshLibrary.MeasureBounds(tileInstance);

            var cameraObject = new GameObject("TileIconCamera");
            cameraObject.transform.position = new Vector3(bounds.center.x, bounds.max.y + CAMERA_HEIGHT_ABOVE_TILE, bounds.center.z);
            cameraObject.transform.rotation = CameraRotation;

            // CameraRotationにより、画面の縦方向はワールドZ、横方向はワールドXに対応する
            // （牌は正方形ではないため、正方形のオルソサイズにすると短い方の軸に余白ができる）
            var iconWidth = Mathf.Max(1, Mathf.RoundToInt(ICON_HEIGHT * (bounds.extents.x / bounds.extents.z)));

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            // 手牌アイコンを隙間なく並べたいため、余白を付けず牌の境界ぴったりに収める
            camera.orthographicSize = bounds.extents.z;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = CAMERA_HEIGHT_ABOVE_TILE + bounds.size.y + 1.0f;

            var renderTexture = new RenderTexture(iconWidth, ICON_HEIGHT, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            var texture = new Texture2D(iconWidth, ICON_HEIGHT, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0.0f, 0.0f, iconWidth, ICON_HEIGHT), 0, 0);
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
                        material.SetFloat(SmoothnessPropertyId, 0.0f);
                    }

                    if (material.HasProperty(MetallicPropertyId))
                    {
                        material.SetFloat(MetallicPropertyId, 0.0f);
                    }
                }
            }
        }
    }
}
