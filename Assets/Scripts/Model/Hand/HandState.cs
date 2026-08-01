using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Hands
{
    /// <summary>
    /// 手牌全体の付加状態を管理するクラス
    /// リーチ・フリテン・一発などの状態を保持する
    /// </summary>
    public class HandState
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// リーチ中かどうか
        /// </summary>
        public bool IsRiichi { get; private set; }
        /// <summary>
        /// 一発が有効かどうか
        /// リーチ後、最初のツモまでの間は true
        /// 他家の副露・自分のツモ切り後に false になる
        /// </summary>
        public bool IppatsuAvailable { get; private set; }
        /// <summary>
        /// フリテン状態かどうか
        /// 自分の捨て牌に待ち牌が含まれている場合に true
        /// </summary>
        public bool IsFuriten { get; private set; }
        /// <summary>
        /// 捨て牌の履歴（フリテン判定に使用）
        /// ReadOnlyCollection をフィールドとして保持することで毎回の生成コストを避け、
        /// 外部からの変更（ダウンキャスト経由）も防ぐ
        /// </summary>
        public IReadOnlyList<Tile> DiscardedTiles => _discardedTilesReadOnly;
        /// <summary>
        /// リーチを宣言したターン番号
        /// リーチしていない場合は -1
        /// </summary>
        public int RiichiTurnIndex { get; private set; } = -1;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 捨て牌の履歴（内部）
        /// </summary>
        private readonly List<Tile> _discardedTiles = new();
        /// <summary>
        /// 捨て牌履歴の読み取り専用ラッパー
        /// _discardedTiles と同じインスタンスを参照するため、追加が自動的に反映される
        /// </summary>
        private readonly ReadOnlyCollection<Tile> _discardedTilesReadOnly;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 手牌状態を初期化する
        /// </summary>
        public HandState()
        {
            _discardedTilesReadOnly = _discardedTiles.AsReadOnly();
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// リーチを宣言する
        /// </summary>
        /// <param name="turnIndex">宣言したターン番号（0以上）</param>
        /// <exception cref="ArgumentOutOfRangeException">turnIndex が0未満の場合</exception>
        public void DeclareRiichi(int turnIndex)
        {
            if (turnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex), $"turnIndex は0以上である必要があります: {turnIndex}");
            }

            IsRiichi = true;
            IppatsuAvailable = true;
            RiichiTurnIndex = turnIndex;
        }
        /// <summary>
        /// 捨て牌を記録する
        /// </summary>
        /// <param name="tile">捨てた牌</param>
        /// <exception cref="ArgumentNullException">tile が null の場合</exception>
        public void AddDiscard(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile), "捨て牌が null です");
            }

            _discardedTiles.Add(tile);
        }
        /// <summary>
        /// フリテン状態を更新する
        /// 自分の捨て牌の中に、現在の待ち牌が含まれていれば true にする
        /// </summary>
        /// <param name="waitingTiles">現在の待ち牌リスト</param>
        /// <exception cref="ArgumentNullException">waitingTiles が null の場合</exception>
        public void UpdateFuriten(IEnumerable<Tile> waitingTiles)
        {
            if (waitingTiles == null)
            {
                throw new ArgumentNullException(nameof(waitingTiles), "waitingTiles が null です");
            }

            IsFuriten = waitingTiles.Any(wait =>
                _discardedTiles.Any(discarded => discarded.IsSameType(wait)));
        }
        /// <summary>
        /// 一発を消す
        /// 他家の副露や自分のツモ切り後に呼ぶ
        /// </summary>
        public void CancelIppatsu()
        {
            IppatsuAvailable = false;
        }
        /// <summary>
        /// リーチを撤回する（カオス麻雀ルール専用。仕様書16.6）
        /// リーチ中のプレイヤーが鳴いた時点で呼ぶ
        /// 役「リーチ」・一発・裏ドラはいずれも IsRiichi を参照して判定されるため、
        /// ここで状態を落とすだけで和了時の計算から自動的に外れる
        /// 供託した1000点は場に残るため、ここでは持ち点に手を触れない
        /// </summary>
        public void CancelRiichi()
        {
            IsRiichi = false;
            IppatsuAvailable = false;
            RiichiTurnIndex = -1;
        }
        /// <summary>
        /// 局開始時に状態をリセットする
        /// </summary>
        public void Reset()
        {
            IsRiichi = false;
            IppatsuAvailable = false;
            IsFuriten = false;
            RiichiTurnIndex = -1;
            _discardedTiles.Clear();
        }
    }
}
