using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Tiles;
using R3;

namespace Mahjong.Presenter
{
    /// <summary>
    /// UI（View層）からの入力を待つ IPlayerController 実装
    /// 意思決定のたびに ReactiveProperty へ選択肢を公開し、View がボタンをクリックして SubmitXxx を呼ぶまで待機する
    /// </summary>
    public sealed class HumanPlayerController : IPlayerController
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// リーチ宣言の確認待ちかどうか
        /// </summary>
        public ReactiveProperty<bool> IsPendingRiichiChoice { get; } = new(false);
        /// <summary>
        /// 打牌待ちの候補（待ち状態でない場合は null）
        /// </summary>
        public ReactiveProperty<IReadOnlyList<DiscardChoice>> PendingDiscardChoices { get; } = new(null);
        /// <summary>
        /// 宣言待ちの候補（待ち状態でない場合は null）。末尾に必ず「スルー」を含む
        /// </summary>
        public ReactiveProperty<IReadOnlyList<CallChoice>> PendingCallChoices { get; } = new(null);


        // ========================================
        // フィールド
        // ========================================
        private UniTaskCompletionSource<bool> _riichiSource;
        private UniTaskCompletionSource<Tile> _discardSource;
        private UniTaskCompletionSource<DeclaredCall> _callSource;


        // ========================================
        // パブリックメソッド（IPlayerController）
        // ========================================
        public async UniTask<bool> ShouldDeclareRiichiAsync(Round round, int playerIndex, CancellationToken ct)
        {
            _riichiSource = new UniTaskCompletionSource<bool>();
            IsPendingRiichiChoice.Value = true;

            var result = await _riichiSource.Task;
            IsPendingRiichiChoice.Value = false;
            return result;
        }

        public async UniTask<Tile> ChooseDiscardAsync(Round round, int playerIndex, CancellationToken ct)
        {
            var player = round.Players[playerIndex];

            // リーチ中（今回の宣言ではない）はツモ切りが強制されるため、確認せずに即返す
            var isOngoingRiichi = player.HandState.IsRiichi && player.HandState.RiichiTurnIndex != round.TurnIndex;

            if (isOngoingRiichi)
            {
                return player.Hand.DrawnTile;
            }

            // リーチ宣言直後は、テンパイを維持できる牌のみを候補にする
            var candidateTiles = player.HandState.IsRiichi
                ? CpuDiscardSelector.FindTenpaiKeepingDiscards(player.Hand)
                : player.Hand.GetClosedTiles();

            _discardSource = new UniTaskCompletionSource<Tile>();
            PendingDiscardChoices.Value = candidateTiles.Select(t => new DiscardChoice(t)).ToList();

            var result = await _discardSource.Task;
            PendingDiscardChoices.Value = null;
            return result;
        }

        public async UniTask<DeclaredCall> ChooseCallAsync(Round round, int playerIndex, IReadOnlyList<CallOption> myOptions, CancellationToken ct)
        {
            var choices = new List<CallChoice>();

            foreach (var option in myOptions)
            {
                foreach (var candidate in option.Candidates)
                {
                    var label = $"{option.Type}({string.Join(",", candidate.Select(t => t.ToString()))})";
                    choices.Add(new CallChoice(label, new DeclaredCall(option.PlayerIndex, option.Type, candidate)));
                }
            }

            choices.Add(new CallChoice("スルー", null));

            _callSource = new UniTaskCompletionSource<DeclaredCall>();
            PendingCallChoices.Value = choices;

            var result = await _callSource.Task;
            PendingCallChoices.Value = null;
            return result;
        }


        // ========================================
        // パブリックメソッド（Viewからの入力受付）
        // ========================================
        /// <summary>
        /// リーチ宣言の確認結果を受け取る
        /// </summary>
        public void SubmitRiichi(bool declare)
        {
            _riichiSource?.TrySetResult(declare);
        }
        /// <summary>
        /// 打牌する牌を受け取る
        /// </summary>
        public void SubmitDiscard(DiscardChoice choice)
        {
            _discardSource?.TrySetResult(choice.Tile);
        }
        /// <summary>
        /// 宣言内容を受け取る（スルーの場合は Call が null）
        /// </summary>
        public void SubmitCall(CallChoice choice)
        {
            _callSource?.TrySetResult(choice.Call);
        }
    }
}
