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
        /// 卓の中心から、伏せ手牌・副露の列までの絶対距離
        /// 河（DISCARD_ROW_DISTANCE_FROM_CENTER）より縁寄りに置く
        /// </summary>
        public const float CONCEALED_HAND_DISTANCE_FROM_CENTER = 3.5f;
        /// <summary>
        /// 伏せ手牌の列を左詰めする基準の枚数（門前の最大枚数）
        /// 現在の枚数ではなくこの枚数を基準に左端を決めることで、鳴いて枚数が減っても
        /// 残った牌が左右に動かず、右側の副露の置き場所とも衝突しない
        /// </summary>
        public const int CONCEALED_HAND_SLOT_COUNT = 13;
        /// <summary>
        /// 副露の列の右端（席の中心から、牌の幅＝隙間を含まない実寸の何個分か）
        /// 副露は実際の卓上と同じく、この右端から左へ向かって並べる
        /// 門前13枚の列とツモ牌（隙間1枚分を空けて置く）の右端が席の中心から約9枚分になるため、
        /// それより右になるよう余裕をとった値。4面子を鳴いても左端がツモ牌に届かない
        /// </summary>
        public const float MELD_ROW_RIGHT_EDGE_IN_TILES = 11.0f;
        /// <summary>
        /// 卓上に寝かせて置く牌の、席ローカルでの向き
        /// 180度は、牌のデフォルト姿勢を真上から見ると絵柄が上下逆になるための補正
        /// （TileIconCacheのアイコン撮影カメラで必要だったZ=180の補正と同じ理由）
        /// </summary>
        public static readonly Quaternion FlatTileRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
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
        /// <summary>
        /// 席（自分から見た相対位置）の向きを返す
        /// 「自分（offset=0）が手前で正面を向いている」状態を基準に、卓の中心を軸に90度ずつ回転させる
        /// 回転が負の向きなのは、麻雀の手番が反時計回り（下家＝自分の右隣）に進むため
        /// これを正の向きにすると下家が画面左に来て、手番が時計回りに見えてしまう
        /// </summary>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        public static Quaternion GetSeatRotation(int offset)
        {
            return Quaternion.Euler(0.0f, -90.0f * offset, 0.0f);
        }
    }
}
