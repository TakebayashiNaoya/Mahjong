using System.Collections.Generic;
using Mahjong.Presenter;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// 卓上の1席分の表示（河・伏せ手牌・副露）をまとめる
    /// 席の向きは生成時にルートのTransformへ設定するため、中の表示はすべて
    /// 「自分（offset=0）が手前で正面を向いている」ローカル座標だけで組み立てられる
    /// （席ごとに座標を回転させる計算をコード側に持たなくて済む）
    /// </summary>
    public sealed class SeatField
    {
        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// この席が自分自身かどうか
        /// </summary>
        private readonly bool _isSelf;
        /// <summary>
        /// 河の表示
        /// </summary>
        private readonly SeatDiscardRow _discardRow;
        /// <summary>
        /// 伏せ手牌の表示
        /// </summary>
        private readonly SeatConcealedHandRow _concealedHandRow;
        /// <summary>
        /// 副露の表示
        /// </summary>
        private readonly SeatMeldRow _meldRow;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 1席分の表示を生成する
        /// </summary>
        /// <param name="tableRoot">卓の中心に置かれたルート</param>
        /// <param name="offset">自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）</param>
        public SeatField(Transform tableRoot, int offset)
        {
            var seatRootObject = new GameObject($"Seat{offset}");
            var seatRoot = seatRootObject.transform;
            seatRoot.SetParent(tableRoot, false);
            seatRoot.localRotation = TableLayout.GetSeatRotation(offset);

            _isSelf = offset == 0;
            _discardRow = new SeatDiscardRow(seatRoot);
            _concealedHandRow = new SeatConcealedHandRow(seatRoot);
            _meldRow = new SeatMeldRow(seatRoot);
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 河を最新の状態に合わせる
        /// </summary>
        public void UpdateDiscards(IReadOnlyList<TileView> discards)
        {
            _discardRow.UpdateTiles(discards);
        }
        /// <summary>
        /// 伏せ手牌を最新の状態に合わせる
        /// 自分の手牌は画面下の2Dアイコンで表示済みのため、卓上には並べない
        /// </summary>
        public void UpdateConcealedHand(ConcealedHandView hand)
        {
            if (_isSelf)
            {
                return;
            }

            _concealedHandRow.UpdateTiles(hand);
        }
        /// <summary>
        /// 副露を最新の状態に合わせる
        /// 副露は麻雀では公開情報のため、自分の分も卓上に置く
        /// </summary>
        public void UpdateMelds(IReadOnlyList<MeldView> melds)
        {
            _meldRow.UpdateTiles(melds);
        }
    }
}
