using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Hands;
using R3;
using UnityEngine;

namespace Mahjong.Presenter
{
    /// <summary>
    /// 対局全体（MahjongGame）をUniTaskで実時間進行させ、状態をR3で外部に公開する
    /// 判断はすべて CpuStrategy に委ね、Model層には一切手を加えない
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 保持するログ行数の上限
        /// </summary>
        private const int MAX_LOG_LINES = 12;
        /// <summary>
        /// このマイルストーンで使用するCPU強度（全席共通）
        /// </summary>
        private const CpuDifficulty DIFFICULTY = CpuDifficulty.Normal;


        // ========================================
        // インスペクタ設定
        // ========================================
        /// <summary>
        /// 参加人数（3または4）
        /// </summary>
        [SerializeField]
        private int _playerCount = 4;
        /// <summary>
        /// 各ステップ（ツモ・打牌・副露解決など）の間に空ける時間（ミリ秒）
        /// </summary>
        [SerializeField]
        private int _stepDelayMilliseconds = 250;
        /// <summary>
        /// 局が終わってから次局を始めるまでに空ける時間（ミリ秒）
        /// </summary>
        [SerializeField]
        private int _roundIntervalMilliseconds = 2000;
        /// <summary>
        /// 人間が操作する席（それ以外はすべてCPU）
        /// </summary>
        [SerializeField]
        private int _humanPlayerIndex = 0;


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 画面表示用の整形済みテキスト（ヘッダー・各プレイヤー・ログをすべて含む）
        /// </summary>
        public ReactiveProperty<string> DisplayText { get; } = new(string.Empty);
        /// <summary>
        /// 人間プレイヤーの意思決定窓口
        /// View層はこれを購読し、ボタン操作を SubmitXxx で送り返す
        /// </summary>
        public HumanPlayerController Human { get; } = new();
        /// <summary>
        /// 人間プレイヤー自身の手牌（門前牌 + ツモ牌、この順）
        /// 3D表示View層はこれを購読して牌モデルを並べる
        /// </summary>
        public ReactiveProperty<IReadOnlyList<TileView>> HumanHandTiles { get; } = new(System.Array.Empty<TileView>());


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 人間以外の席の意思決定を担う（全席で共有する）
        /// </summary>
        private readonly CpuPlayerController _cpu = new();
        /// <summary>
        /// 直近のイベントログ
        /// </summary>
        private readonly List<string> _logLines = new();
        /// <summary>
        /// 現在の対局
        /// </summary>
        private MahjongGame _game;
        /// <summary>
        /// 現在進行中の局
        /// </summary>
        private Round _round;


        // ========================================
        // Unityライフサイクル
        // ========================================
        private void Start()
        {
            RunGameAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }


        // ========================================
        // プライベートメソッド（進行ループ）
        // ========================================
        /// <summary>
        /// 対局全体を進行させる（東風戦・半荘戦が終わるまで局を繰り返す）
        /// </summary>
        private async UniTaskVoid RunGameAsync(CancellationToken ct)
        {
            var settings = GameSettings.CreateDefault(_playerCount, GameLengthType.EastOnly);
            _game = new MahjongGame(settings);

            while (!_game.IsGameOver)
            {
                _round = _game.StartNextRound();
                AppendLog($"=== {_round.RoundWind}{_round.RoundNumber}局 {_round.HonbaCount}本場 開始 ===");
                Refresh();

                var result = await PlayRoundAsync(ct);
                _game.ApplyRoundResult(result);

                AppendLog(DescribeResult(result));
                Refresh();

                await UniTask.Delay(_roundIntervalMilliseconds, cancellationToken: ct);
            }

            AppendLog("=== ゲーム終了 ===");
            Refresh();
        }
        /// <summary>
        /// 1局を、Round が終了を返すまで進行させる
        /// Assets/Tests/EditMode/CPU/CpuStrategyTests.cs の PlayRoundToCompletion と同じ状態遷移を、
        /// 実時間ペース・UI更新付きで行う（Model層に手番ループを持たせない設計方針を維持するため、あえて共通化しない）
        /// </summary>
        private async UniTask<RoundResult> PlayRoundAsync(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (_round.PendingAbortiveDraw != null)
                {
                    return _round.FinalizeAbortiveDraw();
                }

                if (_round.Phase == TurnPhase.AwaitingDraw)
                {
                    if (_round.IsWallExhausted)
                    {
                        return _round.DeclareExhaustiveDraw();
                    }

                    _round.DrawTile();
                    Refresh();
                    await DelayAsync(ct);
                    continue;
                }

                if (_round.Phase == TurnPhase.AwaitingDiscard)
                {
                    var result = await ProcessDiscardPhaseAsync(ct);

                    if (result != null)
                    {
                        return result;
                    }

                    Refresh();
                    await DelayAsync(ct);
                    continue;
                }

                if (_round.Phase == TurnPhase.AwaitingReactions)
                {
                    var result = await ProcessReactionPhaseAsync(ct);

                    if (result != null)
                    {
                        return result;
                    }

                    Refresh();
                    continue;
                }
            }
        }
        /// <summary>
        /// 現在のプレイヤーのツモ番の判断（ツモ和了・九種九牌・北抜き・リーチ・打牌）を行う
        /// ツモ和了・九種九牌・北抜きは席によらず CpuStrategy の既定判断のまま自動で行う
        /// </summary>
        /// <returns>局が終了した場合は結果を返す。継続する場合は null</returns>
        private async UniTask<RoundResult> ProcessDiscardPhaseAsync(CancellationToken ct)
        {
            var playerIndex = _round.CurrentPlayerIndex;

            if (_round.CanDeclareTsumoWin())
            {
                AppendLog($"P{playerIndex} ツモ！");
                return _round.DeclareTsumoWin();
            }

            if (_round.CanDeclareKyuushuKyuuhai() && CpuStrategy.ShouldDeclareKyuushuKyuuhai(_round, playerIndex, DIFFICULTY))
            {
                AppendLog($"P{playerIndex} 九種九牌");
                return _round.DeclareKyuushuKyuuhai();
            }

            if (_round.Settings.UseKitaNuki && _round.CanDeclareKitaNuki() && CpuStrategy.ShouldDeclareKitaNuki(_round, playerIndex, DIFFICULTY))
            {
                AppendLog($"P{playerIndex} 北抜き");
                _round.DeclareKitaNuki();
                return null;
            }

            var controller = GetController(playerIndex);

            if (_round.CanDeclareRiichi() && await controller.ShouldDeclareRiichiAsync(_round, playerIndex, ct))
            {
                _round.DeclareRiichi();
                AppendLog($"P{playerIndex} リーチ");
            }

            var discard = await controller.ChooseDiscardAsync(_round, playerIndex, ct);
            _round.Discard(discard);
            AppendLog($"P{playerIndex} 打: {discard}");
            return null;
        }
        /// <summary>
        /// 直前の捨て牌に対する他家の反応（ロン・ポン・カン・チー）を解決する
        /// </summary>
        /// <returns>局が終了した場合は結果を返す。継続する場合は null</returns>
        private async UniTask<RoundResult> ProcessReactionPhaseAsync(CancellationToken ct)
        {
            var discarderIndex = _round.CurrentPlayerIndex;
            var discardedTile = _round.Players[discarderIndex].Discards[^1];
            var options = _round.GetAvailableCalls(discardedTile, discarderIndex);

            var declarations = new List<DeclaredCall>();

            foreach (var playerIndex in options.Select(o => o.PlayerIndex).Distinct())
            {
                var myOptions = options.Where(o => o.PlayerIndex == playerIndex).ToList();
                var declared = await GetController(playerIndex).ChooseCallAsync(_round, playerIndex, myOptions, ct);

                if (declared != null)
                {
                    declarations.Add(declared);
                    AppendLog($"P{playerIndex} {declared.Type}");
                }
            }

            return _round.ResolveCalls(declarations);
        }
        /// <summary>
        /// 席番号から意思決定の窓口を選ぶ
        /// </summary>
        private IPlayerController GetController(int playerIndex)
        {
            return playerIndex == _humanPlayerIndex ? Human : _cpu;
        }
        /// <summary>
        /// 指定時間だけ待つ
        /// </summary>
        private UniTask DelayAsync(CancellationToken ct)
        {
            return UniTask.Delay(_stepDelayMilliseconds, cancellationToken: ct);
        }


        // ========================================
        // プライベートメソッド（表示整形）
        // ========================================
        /// <summary>
        /// ログ行を1つ追加し、上限を超えた分は古いものから削除する
        /// </summary>
        private void AppendLog(string line)
        {
            _logLines.Add(line);

            while (_logLines.Count > MAX_LOG_LINES)
            {
                _logLines.RemoveAt(0);
            }
        }
        /// <summary>
        /// 局の結果を1行で要約する
        /// </summary>
        private static string DescribeResult(RoundResult result)
        {
            return result.Reason switch
            {
                RoundEndReason.Tsumo => $"ツモ和了（点数増減: {string.Join(", ", result.ScoreDeltas)}）",
                RoundEndReason.Ron => $"ロン和了（点数増減: {string.Join(", ", result.ScoreDeltas)}）",
                RoundEndReason.ExhaustiveDraw => $"荒牌平局（点数増減: {string.Join(", ", result.ScoreDeltas)}）",
                RoundEndReason.AbortiveDraw => $"途中流局: {result.AbortiveReason}",
                _ => result.Reason.ToString(),
            };
        }
        /// <summary>
        /// 現在の対局・局の状態から表示テキストを組み立てる
        /// </summary>
        private void Refresh()
        {
            var sb = new StringBuilder();

            if (_round != null)
            {
                sb.AppendLine($"{_round.RoundWind}{_round.RoundNumber}局 {_round.HonbaCount}本場 供託{_round.RiichiStickCount}本");
                sb.AppendLine();

                for (var i = 0; i < _game.Players.Count; i++)
                {
                    var player = _game.Players[i];
                    var marker = _round.Phase != TurnPhase.Ended && i == _round.CurrentPlayerIndex ? "▶" : "　";
                    var riichi = player.HandState.IsRiichi ? " [リーチ]" : "";

                    sb.AppendLine($"{marker}P{i} {player.SeatWind} {player.Score}点{riichi}");

                    // P0の手牌は3D表示に譲るため、テキストとしては出力しない
                    if (i != _humanPlayerIndex)
                    {
                        sb.AppendLine($"    手牌: {FormatHand(player.Hand)}");
                    }

                    sb.AppendLine($"    河  : {string.Join(" ", player.Discards)}");
                }

                HumanHandTiles.Value = BuildHumanHandTiles();
            }

            sb.AppendLine();
            sb.AppendLine("--- ログ ---");

            foreach (var line in _logLines)
            {
                sb.AppendLine(line);
            }

            DisplayText.Value = sb.ToString();
        }
        /// <summary>
        /// 人間プレイヤーの手牌（門前牌 + ツモ牌）をTileViewのリストに変換する
        /// </summary>
        private IReadOnlyList<TileView> BuildHumanHandTiles()
        {
            var hand = _game.Players[_humanPlayerIndex].Hand;
            var tiles = hand.Tiles.Select(TileView.FromModel).ToList();

            if (hand.DrawnTile != null)
            {
                tiles.Add(TileView.FromModel(hand.DrawnTile));
            }

            return tiles;
        }
        /// <summary>
        /// 手牌を文字列化する（門前牌・ツモ牌・副露をすべて含む）
        /// </summary>
        private static string FormatHand(Hand hand)
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(" ", hand.Tiles.Select(t => t.ToString())));

            if (hand.DrawnTile != null)
            {
                sb.Append($"  ツモ:{hand.DrawnTile}");
            }

            foreach (var meld in hand.Melds)
            {
                sb.Append($"  {meld}");
            }

            return sb.ToString();
        }
    }
}
