using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mahjong.Model.Common;
using Mahjong.Model.Hands;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 対局を通じて維持される、プレイヤー1人分の状態
    /// 席順（PlayerIndex）は対局中固定。自風（SeatWind）は局ごとに Round が再割当する
    /// </summary>
    public class PlayerState
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 固定席順（0始まり）
        /// </summary>
        public int PlayerIndex { get; }
        /// <summary>
        /// 現在の持ち点
        /// </summary>
        public int Score { get; private set; }
        /// <summary>
        /// 手牌
        /// </summary>
        public Hand Hand { get; }
        /// <summary>
        /// 手牌の付加状態（リーチ・フリテン等）
        /// </summary>
        public HandState HandState { get; }
        /// <summary>
        /// 現在の局における自風
        /// </summary>
        public Wind SeatWind { get; private set; }
        /// <summary>
        /// 河（捨て牌の履歴）
        /// </summary>
        public IReadOnlyList<Tile> Discards => _discardsReadOnly;
        /// <summary>
        /// 北抜きした牌の枚数（三人麻雀のみ使用）
        /// </summary>
        public int KitaCount { get; private set; }


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 河の内部リスト
        /// </summary>
        private readonly List<Tile> _discards = new();
        /// <summary>
        /// 河の読み取り専用ラッパー
        /// </summary>
        private readonly ReadOnlyCollection<Tile> _discardsReadOnly;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// プレイヤー状態を初期化する
        /// </summary>
        /// <param name="playerIndex">固定席順（0始まり）</param>
        /// <param name="initialScore">初期持ち点</param>
        public PlayerState(int playerIndex, int initialScore)
        {
            PlayerIndex = playerIndex;
            Score = initialScore;
            Hand = new Hand();
            HandState = new HandState();
            _discardsReadOnly = _discards.AsReadOnly();
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 持ち点を増減する
        /// </summary>
        public void AddScore(int delta)
        {
            Score += delta;
        }
        /// <summary>
        /// 局開始時に自風を割り当てる
        /// </summary>
        public void AssignSeatWind(Wind seatWind)
        {
            SeatWind = seatWind;
        }
        /// <summary>
        /// 捨て牌を河に積む
        /// </summary>
        /// <exception cref="ArgumentNullException">tile が null の場合</exception>
        public void AddDiscard(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile), "捨て牌が null です");
            }

            _discards.Add(tile);
        }
        /// <summary>
        /// 河から指定位置の捨て牌を抜き取る（カオス麻雀ルール専用。仕様書16.8）
        /// フリテン判定に使う捨て牌履歴（HandState.DiscardedTiles）からは削除しない
        /// 卓上から牌が持ち去られても「一度捨てた」事実は消えないため、フリテンは継続する
        /// </summary>
        /// <param name="index">河の中での位置</param>
        /// <returns>抜き取った牌</returns>
        /// <exception cref="ArgumentOutOfRangeException">index が範囲外の場合</exception>
        public Tile RemoveDiscardAt(int index)
        {
            if (index < 0 || index >= _discards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"index が範囲外です: {index}");
            }

            var tile = _discards[index];
            _discards.RemoveAt(index);
            return tile;
        }
        /// <summary>
        /// 北抜きの枚数を1増やす
        /// </summary>
        public void AddKita()
        {
            KitaCount++;
        }
        /// <summary>
        /// 新しい局の開始に合わせて、手牌の付加状態・河・北抜き枚数をリセットする
        /// 持ち点は維持する。手牌自体は Round が SetInitialTiles で配り直す
        /// </summary>
        public void ResetForNewRound()
        {
            HandState.Reset();
            _discards.Clear();
            KitaCount = 0;
        }
    }
}
