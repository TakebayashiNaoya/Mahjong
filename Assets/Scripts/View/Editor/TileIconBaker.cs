using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mahjong.View.Editor
{
    /// <summary>
    /// TileIconCacheが実行時に生成する牌アイコンを、Assetsとして書き出すEditor専用ツール
    /// 一度書き出しておけば、TileIconCache.GetSpriteは焼き込み済みのSpriteを優先して使うようになる
    /// </summary>
    public static class TileIconBaker
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// アイコンPNGの書き出し先フォルダ
        /// TileIconCache.BAKED_ICON_RESOURCE_ROOT（"TileIcons/"）と対応させる
        /// </summary>
        private const string OUTPUT_FOLDER = "Assets/Resources/TileIcons";


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 牌37種類すべてのアイコンを撮影し、PNGとしてOUTPUT_FOLDERへ書き出す
        /// </summary>
        [MenuItem("Tools/麻雀/牌アイコンを書き出す")]
        public static void BakeAllIcons()
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);

            var fileNames = BuildAllMeshFileNames();

            foreach (var fileName in fileNames)
            {
                var texture = TileIconCache.RenderIconTexture(fileName);

                if (texture == null)
                {
                    continue;
                }

                File.WriteAllBytes($"{OUTPUT_FOLDER}/{fileName}.png", texture.EncodeToPNG());
            }

            AssetDatabase.Refresh();

            foreach (var fileName in fileNames)
            {
                ConfigureAsSprite($"{OUTPUT_FOLDER}/{fileName}.png");
            }

            Debug.Log($"牌アイコンを{fileNames.Count}種類書き出しました: {OUTPUT_FOLDER}");
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// PNGとして書き出したテクスチャを、UI用のSpriteとしてインポートし直す
        /// </summary>
        private static void ConfigureAsSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogError($"TextureImporterが取得できません: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        /// <summary>
        /// 牌37種類ぶんのメッシュファイル名（拡張子なし）を組み立てる
        /// TileIconCache.ResolveMeshFileNameが導く命名規則と一致させる
        /// </summary>
        private static List<string> BuildAllMeshFileNames()
        {
            var fileNames = new List<string>();

            foreach (var suitPrefix in new[] { "pmanzu", "ppinzu", "psouzu" })
            {
                for (var number = 1; number <= 9; number++)
                {
                    fileNames.Add($"{suitPrefix}_{number}");
                }

                fileNames.Add($"{suitPrefix}_5r");
            }

            fileNames.AddRange(new[]
            {
                "pjihai_ton",
                "pjihai_nan",
                "pjihai_sha",
                "pjihai_pe",
                "pjihai_haku",
                "pjihai_hatsu",
                "pjihai_chun",
            });

            return fileNames;
        }
    }
}
