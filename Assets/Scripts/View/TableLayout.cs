using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 卓（シーン上の "table" オブジェクト）の大きさ・位置を解決するための共通処理
    /// 河・他家の手牌など、卓上に物を並べるView側のコンポーネントから共通で使う
    /// </summary>
    public static class TableLayout
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// シーン上で卓として参照するGameObjectの名前
        /// </summary>
        private const string TABLE_OBJECT_NAME = "table";
        /// <summary>
        /// 卓が見つからない場合に使うフォールバックの大きさ（半径換算）
        /// </summary>
        private static readonly Vector3 FallbackExtents = new(3f, 0.1f, 3f);


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 卓の実測バウンディングボックスを返す
        /// 見つからない場合はフォールバックの大きさを原点中心で返す
        /// </summary>
        public static Bounds ResolveBounds()
        {
            var tableObject = GameObject.Find(TABLE_OBJECT_NAME);

            if (tableObject == null)
            {
                return new Bounds(Vector3.zero, FallbackExtents * 2f);
            }

            return TileMeshLibrary.MeasureBounds(tableObject);
        }
    }
}
