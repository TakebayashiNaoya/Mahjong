using System.Collections.Generic;
using Mahjong.Presenter;
using R3;
using UnityEngine;

namespace Mahjong.View
{
    /// <summary>
    /// GamePresenter が公開する卓上の情報（河・伏せ手牌・副露）を購読し、席ごとに配って3D表示させる
    /// Presenterのリストはどれも自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）で並んでいるため、
    /// 添字をそのまま席の番号として使える
    /// 卓の中心にルートを1つ置き、その下に席を人数分ぶら下げる構成にしているのは、
    /// 席ごとの向きをTransformに任せて、表示側が座標を回転させる計算を持たなくて済むようにするため
    /// </summary>
    public sealed class TableFieldView : MonoBehaviour
    {
        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 卓上の情報の購読元
        /// </summary>
        private GamePresenter _presenter;
        /// <summary>
        /// 卓の中心に置くルート
        /// </summary>
        private Transform _tableRoot;
        /// <summary>
        /// 席（自分から見た相対位置）ごとの表示
        /// </summary>
        private readonly List<SeatField> _seats = new();


        // ========================================
        // 起動
        // ========================================
        private void Awake()
        {
            _presenter = GetComponent<GamePresenter>();

            var tableRootObject = new GameObject("TableField");
            _tableRoot = tableRootObject.transform;
            _tableRoot.SetParent(transform, false);

            // 卓の中心・設置面を原点にすることで、席の表示は卓の中心からの相対位置だけで書ける
            var center = TableLayout.ResolveCenter();
            _tableRoot.position = new Vector3(center.x, TableLayout.SURFACE_Y, center.z);
        }

        private void Start()
        {
            _presenter.PlayerDiscards.Subscribe(OnPlayerDiscardsChanged).AddTo(this);
            _presenter.ConcealedHands.Subscribe(OnConcealedHandsChanged).AddTo(this);
            _presenter.PlayerMelds.Subscribe(OnPlayerMeldsChanged).AddTo(this);
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 河が更新されるたびに、席ごとに配る
        /// </summary>
        private void OnPlayerDiscardsChanged(IReadOnlyList<IReadOnlyList<TileView>> playerDiscards)
        {
            if (playerDiscards == null)
            {
                return;
            }

            EnsureSeats(playerDiscards.Count);

            for (var offset = 0; offset < playerDiscards.Count; offset++)
            {
                _seats[offset].UpdateDiscards(playerDiscards[offset]);
            }
        }
        /// <summary>
        /// 伏せ手牌が更新されるたびに、席ごとに配る
        /// </summary>
        private void OnConcealedHandsChanged(IReadOnlyList<ConcealedHandView> hands)
        {
            if (hands == null)
            {
                return;
            }

            EnsureSeats(hands.Count);

            for (var offset = 0; offset < hands.Count; offset++)
            {
                _seats[offset].UpdateConcealedHand(hands[offset]);
            }
        }
        /// <summary>
        /// 副露が更新されるたびに、席ごとに配る
        /// </summary>
        private void OnPlayerMeldsChanged(IReadOnlyList<IReadOnlyList<MeldView>> playerMelds)
        {
            if (playerMelds == null)
            {
                return;
            }

            EnsureSeats(playerMelds.Count);

            for (var offset = 0; offset < playerMelds.Count; offset++)
            {
                _seats[offset].UpdateMelds(playerMelds[offset]);
            }
        }
        /// <summary>
        /// 席が足りなければ作る
        /// 参加人数は対局開始時にPresenterから最初のデータが届くまで分からないため、
        /// 起動時ではなく受け取ったリストの長さに合わせて用意する
        /// </summary>
        private void EnsureSeats(int seatCount)
        {
            while (_seats.Count < seatCount)
            {
                _seats.Add(new SeatField(_tableRoot, _seats.Count));
            }
        }
    }
}
