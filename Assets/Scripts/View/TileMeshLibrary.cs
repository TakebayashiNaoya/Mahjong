using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// TileViewから3D牌メッシュ（Assets/Resources/Mahjong Complete Set/Mesh配下のFBX）を
    /// 解決・読み込みするための共通処理
    /// TileIconCache（2Dアイコン撮影用）とDiscardFieldView（河の3D表示用）の両方から使う
    /// </summary>
    public static class TileMeshLibrary
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 牌メッシュの読み込み元フォルダ（Resources基準の相対パス）
        /// </summary>
        private const string MESH_RESOURCE_ROOT = "Mahjong Complete Set/Mesh/";
        /// <summary>
        /// サイズ計測用に牌を一時的に配置する座標
        /// シーン上の他オブジェクトと絶対に重ならないよう、原点から大きく離す
        /// </summary>
        private static readonly Vector3 MeasurePosition = new(10000.0f, 10000.0f, 10000.0f);


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// TileView からリソースファイル名（拡張子なし）を組み立てる
        /// 例: 萬子5(赤) → "pmanzu_5r", 東 → "pjihai_ton"
        /// </summary>
        /// <exception cref="System.InvalidOperationException">未対応のSuit/Honorの場合</exception>
        public static string ResolveFileName(TileView tile)
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
        /// <summary>
        /// TileView に対応する牌メッシュのプレハブをResourcesから読み込む
        /// 見つからない場合はnullを返し、エラーログを出す
        /// </summary>
        public static GameObject LoadPrefab(TileView tile)
        {
            return LoadPrefab(ResolveFileName(tile));
        }
        /// <summary>
        /// メッシュファイル名から直接プレハブを読み込む
        /// </summary>
        public static GameObject LoadPrefab(string meshFileName)
        {
            var prefab = Resources.Load<GameObject>(MESH_RESOURCE_ROOT + meshFileName);

            if (prefab == null)
            {
                Debug.LogError($"牌メッシュが見つかりません: {meshFileName}");
            }

            return prefab;
        }
        /// <summary>
        /// プレハブを指定の姿勢で置いたときの、ピボットを原点としたバウンディングボックスを計測する
        /// 計測用のインスタンスは原点から大きく離した位置に生成する
        /// （Destroyはフレーム末に効くため、卓の上に生成すると1フレームだけ映り込む）
        /// 実行時専用（Destroyを使うため、Editモードからは呼べない）
        /// </summary>
        /// <param name="prefab">計測する牌メッシュのプレハブ</param>
        /// <param name="rotation">計測時の姿勢</param>
        public static Bounds MeasurePrefabBounds(GameObject prefab, Quaternion rotation)
        {
            var instance = Object.Instantiate(prefab, MeasurePosition, rotation);
            var bounds = MeasureBounds(instance);
            Object.Destroy(instance);

            return new Bounds(bounds.center - MeasurePosition, bounds.size);
        }
        /// <summary>
        /// GameObject配下の全Rendererを合成したバウンディングボックスを計測する
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Rendererが1つも無い場合</exception>
        public static Bounds MeasureBounds(GameObject tileObject)
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
    }
}
