using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Hands;
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
        /// <summary>
        /// カオス麻雀の取得元選択の候補（待ち状態でない場合は null）
        /// 2段階選択のため、選択が進むたびに中身が差し替わる
        /// </summary>
        public ReactiveProperty<IReadOnlyList<ChaosDrawChoice>> PendingChaosDrawChoices { get; } = new(null);


        // ========================================
        // フィールド
        // ========================================
        private UniTaskCompletionSource<bool> _riichiSource;
        private UniTaskCompletionSource<Tile> _discardSource;
        private UniTaskCompletionSource<DeclaredCall> _callSource;
        private UniTaskCompletionSource<ChaosDrawOption> _chaosDrawSource;
        /// <summary>
        /// 取得元選択中の局（ラベルに牌・副露を出すために参照する）
        /// </summary>
        private Round _chaosDrawRound;
        /// <summary>
        /// 取得元選択中の自分の席順
        /// </summary>
        private int _chaosDrawPlayerIndex;
        /// <summary>
        /// 取得元選択中の候補一覧（2段階目の絞り込み元）
        /// </summary>
        private IReadOnlyList<ChaosDrawOption> _chaosDrawOptions;


        // ========================================
        // パブリックメソッド（IPlayerController）
        // ========================================
        public async UniTask<ChaosDrawOption> ChooseChaosDrawAsync(
            Round round, int playerIndex, IReadOnlyList<ChaosDrawOption> options, CancellationToken ct)
        {
            _chaosDrawRound = round;
            _chaosDrawPlayerIndex = playerIndex;
            _chaosDrawOptions = options;
            _chaosDrawSource = new UniTaskCompletionSource<ChaosDrawOption>();
            ShowChaosDrawGroups();

            var result = await _chaosDrawSource.Task;

            PendingChaosDrawChoices.Value = null;
            _chaosDrawRound = null;
            _chaosDrawOptions = null;
            return result;
        }

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

            // ツモ牌かどうかは候補の位置ではなく牌そのもので判定する
            // （リーチ宣言直後は候補が絞り込まれるため、末尾がツモ牌とは限らず、候補から外れることもある）
            var drawnTile = player.Hand.DrawnTile;

            _discardSource = new UniTaskCompletionSource<Tile>();
            PendingDiscardChoices.Value = candidateTiles
                .Select(t => new DiscardChoice(t, ReferenceEquals(t, drawnTile)))
                .ToList();

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
        /// <summary>
        /// カオス麻雀の取得元選択を受け取る
        /// 確定の選択肢なら待機を解き、絞り込み・戻るなら表示中の候補を差し替えるだけで待機を続ける
        /// </summary>
        public void SubmitChaosDraw(ChaosDrawChoice choice)
        {
            if (choice.IsBack)
            {
                ShowChaosDrawGroups();
                return;
            }

            if (choice.Option == null)
            {
                ShowChaosDrawTargets(choice.GroupSource, choice.GroupPlayerIndex);
                return;
            }

            _chaosDrawSource?.TrySetResult(choice.Option);
        }


        // ========================================
        // プライベートメソッド（取得元選択の組み立て）
        // ========================================
        /// <summary>
        /// 1段階目：どこから取るかを、取得元の種別と対象プレイヤーごとにまとめて提示する
        /// 山だけは候補が1つしかないため、絞り込みを挟まずその場で確定できるようにする
        /// </summary>
        private void ShowChaosDrawGroups()
        {
            var choices = new List<ChaosDrawChoice>();
            var wallOption = _chaosDrawOptions.FirstOrDefault(o => o.Source == ChaosDrawSource.Wall);

            if (wallOption != null)
            {
                choices.Add(ChaosDrawChoice.Confirm("山からツモ", wallOption));
            }

            var groups = _chaosDrawOptions
                .Where(o => o.Source != ChaosDrawSource.Wall)
                .GroupBy(o => (o.Source, o.TargetPlayerIndex));

            foreach (var group in groups)
            {
                var label = BuildGroupLabel(group.Key.Source, group.Key.TargetPlayerIndex, group.Count());
                choices.Add(ChaosDrawChoice.OpenGroup(label, group.Key.Source, group.Key.TargetPlayerIndex));
            }

            PendingChaosDrawChoices.Value = choices;
        }
        /// <summary>
        /// 2段階目：選ばれた絞り込みの中身を、取れる牌1枚ずつの候補として提示する
        /// </summary>
        private void ShowChaosDrawTargets(ChaosDrawSource source, int targetPlayerIndex)
        {
            var choices = _chaosDrawOptions
                .Where(o => o.Source == source && o.TargetPlayerIndex == targetPlayerIndex)
                .Select(o => ChaosDrawChoice.Confirm(BuildTargetLabel(o), o))
                .ToList();

            choices.Add(ChaosDrawChoice.Back("戻る"));
            PendingChaosDrawChoices.Value = choices;
        }
        /// <summary>
        /// 1段階目のラベルを組み立てる（末尾の数字は取れる牌の枚数）
        /// </summary>
        private static string BuildGroupLabel(ChaosDrawSource source, int targetPlayerIndex, int optionCount)
        {
            return source switch
            {
                ChaosDrawSource.DiscardPile => $"P{targetPlayerIndex}の河({optionCount})",
                ChaosDrawSource.OpponentHand => $"P{targetPlayerIndex}の手牌({optionCount})",
                ChaosDrawSource.OpponentMeld => $"P{targetPlayerIndex}の副露({optionCount})",
                ChaosDrawSource.OwnMeld => $"副露を戻す({optionCount})",
                _ => source.ToString(),
            };
        }
        /// <summary>
        /// 2段階目のラベルを組み立てる
        /// 河・副露は場に見えているため牌そのものを出すが、
        /// 他家の手牌は伏せたまま選ばせる必要があるため位置だけを示す（仕様書16.3）
        /// </summary>
        private string BuildTargetLabel(ChaosDrawOption option)
        {
            var players = _chaosDrawRound.Players;

            switch (option.Source)
            {
                case ChaosDrawSource.DiscardPile:
                    return players[option.TargetPlayerIndex].Discards[option.TargetIndex].ToString();

                case ChaosDrawSource.OpponentHand:
                    return $"左から{option.TargetIndex + 1}枚目";

                case ChaosDrawSource.OpponentMeld:
                    var stolenMeld = players[option.TargetPlayerIndex].Hand.Melds[option.TargetIndex];
                    return $"{DescribeMeld(stolenMeld)}の{option.MeldTileIndex + 1}枚目";

                case ChaosDrawSource.OwnMeld:
                    return DescribeMeld(players[_chaosDrawPlayerIndex].Hand.Melds[option.TargetIndex]);

                default:
                    return option.ToString();
            }
        }
        /// <summary>
        /// 副露を「ポン[2筒]」のような表示文字列に変換する
        /// </summary>
        private static string DescribeMeld(Meld meld)
        {
            var typeLabel = meld.Type switch
            {
                MeldType.Chi => "チー",
                MeldType.Pon => "ポン",
                MeldType.DaiMinKan => "大明槓",
                MeldType.KaKan => "加槓",
                MeldType.AnKan => "暗槓",
                _ => meld.Type.ToString(),
            };

            return $"{typeLabel}{meld.Tiles[0]}";
        }
    }
}
