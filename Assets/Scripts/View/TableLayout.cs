using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 卓（シーン上の "table" オブジェクト）の位置と、卓上に物を並べるときの共通レイアウト値
    /// 河・他家の手牌など、卓上に物を並べるView側のコンポーネントから共通で使う
    /// 席ごとの距離をここへ集約しているのは、河と手牌の前後関係を1か所で見比べられるようにするため
    /// </summary>
    public static class TableLayout
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 卓の設置面のY座標
        /// 牌のピボットは牌の中心にあるため、実際に置くY座標は牌ごとの実測バウンディングボックス
        /// （min.y）から別途補正する
        /// </summary>
        public const float SURFACE_Y = 0.0f;
        /// <summary>
        /// 卓の中心から、河の先頭行までの絶対距離
        /// 卓のサイズに対する割合ではなく固定値にする理由: 割合にすると卓を小さくしたときに
        /// 4人分の河が中心の1点に近づいて重なってしまう（牌自体の大きさは卓のサイズと無関係なため）
        /// </summary>
        public const float DISCARD_ROW_DISTANCE_FROM_CENTER = 1.2f;
        /// <summary>
        /// 卓の中心から、伏せ手牌までの絶対距離
        /// 河（DISCARD_ROW_DISTANCE_FROM_CENTER）より縁寄りに置く
        /// </summary>
        public const float CONCEALED_HAND_DISTANCE_FROM_CENTER = 3.5f;
        /// <summary>
        /// シーン上で卓として参照するGameObjectの名前
        /// </summary>
        private const string TABLE_OBJECT_NAME = "table";


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 直近に見つけた卓のGameObject
        /// シーンが切り替わって破棄されるとUnityのnull比較でnullになるため、その場合は探し直す
        /// </summary>
        private static GameObject _tableObject;
        /// <summary>
        /// _tableObject に対して計測済みの中心座標
        /// </summary>
        private static Vector3 _cachedCenter;


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 卓の実測バウンディングボックスの中心を返す
        /// 卓が見つからない場合は原点を返す
        /// GameObject.Find はシーン全体の走査になるため、同じ卓に対しては計測結果を使い回す
        /// </summary>
        public static Vector3 ResolveCenter()
        {
            if (_tableObject != null)
            {
                return _cachedCenter;
            }

            _tableObject = GameObject.Find(TABLE_OBJECT_NAME);
            _cachedCenter = _tableObject != null ? TileMeshLibrary.MeasureBounds(_tableObject).center : Vector3.zero;
            return _cachedCenter;
        }
    }
}
