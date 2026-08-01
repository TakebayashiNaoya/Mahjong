using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Common;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Hands;
using Mahjong.Model.Scoring;
using Mahjong.Model.Tiles;
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


        // ========================================
        // インスペクタ設定
        // ========================================
        /// <summary>
        /// 各ステップ（ツモ・打牌・副露解決など）の間に空ける時間（ミリ秒）
        /// </summary>
        [SerializeField]
        private int _stepDelayMilliseconds = 250;
        /// <summary>
        /// 局が終わってから次局を始めるまでに空ける時間（ミリ秒）
        /// この間、和了・流局の結果画面（RoundResultDisplay）を表示し続ける
        /// </summary>
        [SerializeField]
        private int _roundIntervalMilliseconds = 6000;


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
        /// 人間プレイヤー自身の手牌（牌の並びとツモ牌の位置）
        /// View層はこれを購読してアイコンを並べる
        /// </summary>
        public ReactiveProperty<HumanHandView> HumanHand { get; } = new(HumanHandView.Empty);
        /// <summary>
        /// 全プレイヤーの河（捨て牌）
        /// 席順ではなく自分から見た相対位置で並ぶ（0=自分, 1=下家, 2=対面, 3=上家）
        /// 3D表示View層はこれを購読して卓上に牌を並べる
        /// </summary>
        public ReactiveProperty<IReadOnlyList<IReadOnlyList<TileView>>> PlayerDiscards { get; } = new(System.Array.Empty<IReadOnlyList<TileView>>());
        /// <summary>
        /// 全プレイヤーの伏せ手牌（副露した牌は含まない）
        /// 並びは PlayerDiscards と同じく自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）
        /// 他家の手牌は中身を公開しないため、枚数とツモ牌の有無だけを渡して伏せ牌として表示させる
        /// </summary>
        public ReactiveProperty<IReadOnlyList<ConcealedHandView>> ConcealedHands { get; } = new(System.Array.Empty<ConcealedHandView>());
        /// <summary>
        /// 全プレイヤーの副露
        /// 並びは PlayerDiscards と同じく自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）
        /// 3D表示View層はこれを購読して卓上に面子を並べる
        /// </summary>
        public ReactiveProperty<IReadOnlyList<IReadOnlyList<MeldView>>> PlayerMelds { get; } = new(System.Array.Empty<IReadOnlyList<MeldView>>());
        /// <summary>
        /// 直近に終了した局の和了・流局結果
        /// 局が終わってから次局が始まるまでの間だけ値を持ち、それ以外は null
        /// （Mahjong.Model.Game.RoundResult と名前が衝突するため RoundResultDisplay という名前にしている）
        /// </summary>
        public ReactiveProperty<RoundResultView> RoundResultDisplay { get; } = new(null);


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
        /// <summary>
        /// 人間が操作する席（それ以外はすべてCPU）。RunAsync で外部から設定される
        /// </summary>
        private int _humanPlayerIndex;
        /// <summary>
        /// CPU強度（全席共通）。RunAsync で外部から設定される
        /// </summary>
        private CpuDifficulty _difficulty;


        // ========================================
        // パブリックメソッド（進行ループ）
        // ========================================
        /// <summary>
        /// 対局全体を進行させる（東風戦・半荘戦が終わるまで局を繰り返し、ゲーム終了時に最終順位を返す）
        /// AppFlowPresenter がモード選択・設定画面で決まった内容を渡して呼び出す
        /// </summary>
        /// <param name="settings">対局設定（人数・東風戦/半荘戦・赤ドラ・北抜きなど）</param>
        /// <param name="difficulty">CPU強度（全席共通）</param>
        /// <param name="humanPlayerIndex">人間が操作する席</param>
        /// <param name="ct">ゲーム全体のキャンセルトークン</param>
        /// <returns>ゲーム終了時の最終順位</returns>
        public async UniTask<GameOverSummaryView> RunAsync(
            GameSettings settings, CpuDifficulty difficulty, int humanPlayerIndex, CancellationToken ct)
        {
            _humanPlayerIndex = humanPlayerIndex;
            _difficulty = difficulty;
            _game = new MahjongGame(settings);

            while (!_game.IsGameOver)
            {
                _round = _game.StartNextRound();
                AppendLog($"=== {_round.RoundWind}{_round.RoundNumber}局 {_round.HonbaCount}本場 開始 ===");
                Refresh();

                var result = await PlayRoundAsync(ct);
                var resultView = BuildRoundResultView(result);
                _game.ApplyRoundResult(result);

                AppendLog(DescribeResult(result));
                RoundResultDisplay.Value = resultView;
                Refresh();

                await UniTask.Delay(_roundIntervalMilliseconds, cancellationToken: ct);
                RoundResultDisplay.Value = null;
            }

            AppendLog("=== ゲーム終了 ===");
            Refresh();

            return BuildGameOverSummary();
        }


        // ========================================
        // プライベートメソッド（進行ループ）
        // ========================================
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

                    if (_round.Settings.UseChaosRules)
                    {
                        await ProcessChaosDrawPhaseAsync(ct);
                    }
                    else
                    {
                        _round.DrawTile();
                    }

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
        /// カオス麻雀ルールで、現在のプレイヤーがどこから牌を取るかを決めて実行する（仕様書16.3）
        /// 自分の副露を戻した場合は打牌せずに手番が終わり、Round が次のプレイヤーのツモ待ちに進めるため、
        /// ここでは結果のフェーズを見ずに呼び出し元のループへ戻す
        /// </summary>
        private async UniTask ProcessChaosDrawPhaseAsync(CancellationToken ct)
        {
            var playerIndex = _round.CurrentPlayerIndex;
            var options = _round.GetChaosDrawOptions();
            var option = await GetController(playerIndex).ChooseChaosDrawAsync(_round, playerIndex, options, ct);
            var drawnTile = _round.ExecuteChaosDraw(option);

            AppendLog(DescribeChaosDraw(playerIndex, option, drawnTile));
        }
        /// <summary>
        /// カオス麻雀の取得操作を1行で要約する
        /// </summary>
        private static string DescribeChaosDraw(int playerIndex, ChaosDrawOption option, Tile drawnTile)
        {
            var sourceLabel = option.Source switch
            {
                ChaosDrawSource.Wall => "山",
                ChaosDrawSource.DiscardPile => $"P{option.TargetPlayerIndex}の河",
                ChaosDrawSource.OpponentHand => $"P{option.TargetPlayerIndex}の手牌",
                ChaosDrawSource.OpponentMeld => $"P{option.TargetPlayerIndex}の副露",
                ChaosDrawSource.OwnMeld => "自分の副露",
                _ => option.Source.ToString(),
            };

            // チー・ポンを戻した手番は打牌が発生しないため、ツモ牌が無いことを明示する
            if (drawnTile == null)
            {
                return $"P{playerIndex} {sourceLabel}を手牌に戻す（打牌なし）";
            }

            return $"P{playerIndex} {sourceLabel}から取得";
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

            if (_round.CanDeclareKyuushuKyuuhai() && CpuStrategy.ShouldDeclareKyuushuKyuuhai(_round, playerIndex, _difficulty))
            {
                AppendLog($"P{playerIndex} 九種九牌");
                return _round.DeclareKyuushuKyuuhai();
            }

            if (_round.Settings.UseKitaNuki && _round.CanDeclareKitaNuki() && CpuStrategy.ShouldDeclareKitaNuki(_round, playerIndex, _difficulty))
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
        /// ゲーム終了時の最終順位を組み立てる
        /// 返し点を用いた正式な順位計算は未実装のため、持ち点降順の簡易順位とする（同点は席順を維持）
        /// </summary>
        private GameOverSummaryView BuildGameOverSummary()
        {
            var standings = _game.Players
                .Select((player, index) => (player, index))
                .OrderByDescending(p => p.player.Score)
                .Select((p, rank) => new PlayerStandingView(rank + 1, p.index, p.index == _humanPlayerIndex, p.player.Score))
                .ToList();

            return new GameOverSummaryView(standings);
        }
        /// <summary>
        /// 局の結果を、和了・流局画面向けの表示用データに組み立てる
        /// Round は次局開始（StartNextRound）で手牌・河をリセットするため、それより前に呼ぶ必要がある
        /// </summary>
        private RoundResultView BuildRoundResultView(RoundResult result)
        {
            var roundLabel = $"{_round.RoundWind}{_round.RoundNumber}局 {_round.HonbaCount}本場";
            var reasonLabel = BuildReasonLabel(result);
            var wins = result.Wins.Select(BuildWinResultView).ToList();

            return new RoundResultView(roundLabel, reasonLabel, wins, result.ScoreDeltas, result.TenpaiStates);
        }
        /// <summary>
        /// 局の終了理由を表示文字列に整形する
        /// </summary>
        private static string BuildReasonLabel(RoundResult result)
        {
            return result.Reason switch
            {
                RoundEndReason.Tsumo => "ツモ和了",
                RoundEndReason.Ron => "ロン和了",
                RoundEndReason.ExhaustiveDraw => "荒牌平局",
                RoundEndReason.AbortiveDraw => $"途中流局: {DescribeAbortiveReason(result.AbortiveReason)}",
                _ => result.Reason.ToString(),
            };
        }
        /// <summary>
        /// 途中流局の理由を日本語表示名に変換する
        /// </summary>
        private static string DescribeAbortiveReason(AbortiveDrawReason? reason)
        {
            return reason switch
            {
                AbortiveDrawReason.KyuushuKyuuhai => "九種九牌",
                AbortiveDrawReason.SuufuRenda => "四風連打",
                AbortiveDrawReason.SuuKaiSanra => "四槓散了",
                AbortiveDrawReason.Sanchaho => "三家和",
                null => string.Empty,
                _ => reason.ToString(),
            };
        }
        /// <summary>
        /// 和了1件分を表示用データに変換する
        /// </summary>
        private WinResultView BuildWinResultView(WinOutcome outcome)
        {
            var winner = _round.Players[outcome.WinnerIndex];
            var isTsumo = outcome.DiscarderIndex == null;
            var isDealer = outcome.WinnerIndex == _round.DealerIndex;
            var discarderSeatWind = outcome.DiscarderIndex.HasValue
                ? _round.Players[outcome.DiscarderIndex.Value].SeatWind
                : (Wind?)null;

            var yakuLines = outcome.Score.Yaku
                .Select(y => new YakuLineView(YakuDisplayNames.GetName(y.Id), y.IsYakuman ? y.YakumanMultiplier : y.Han))
                .ToList();

            var melds = winner.Hand.Melds
                .Select(meld => BuildMeldView(meld, winner.SeatWind, _game.Players.Count))
                .ToList();

            return new WinResultView(
                FormatSeatLabel(winner.SeatWind, isDealer), FormatSourceLabel(isTsumo, discarderSeatWind),
                outcome.Score.IsYakuman, yakuLines, outcome.Score.DoraHan, outcome.Score.AkaDoraHan,
                outcome.Score.Fu, outcome.Score.Han, YakuDisplayNames.GetLimitBandLabel(outcome.Score.Band),
                FormatPointsLabel(isDealer, outcome.Score.Payment), BuildWinningHandTiles(winner, outcome, isTsumo), melds);
        }
        /// <summary>
        /// 席を表す表示文字列を組み立てる（例: "東家（親）"）
        /// </summary>
        private static string FormatSeatLabel(Wind seatWind, bool isDealer)
        {
            var windLabel = FormatWindLabel(seatWind);
            return isDealer ? $"{windLabel}家（親）" : $"{windLabel}家";
        }
        /// <summary>
        /// 和了の種類を表す表示文字列を組み立てる（例: "ツモ", "ロン（放銃: 西家）"）
        /// </summary>
        private static string FormatSourceLabel(bool isTsumo, Wind? discarderSeatWind)
        {
            return isTsumo ? "ツモ" : $"ロン（放銃: {FormatWindLabel(discarderSeatWind.Value)}家）";
        }
        /// <summary>
        /// 風の日本語表示名を返す
        /// </summary>
        private static string FormatWindLabel(Wind wind)
        {
            return wind switch
            {
                Wind.East => "東",
                Wind.South => "南",
                Wind.West => "西",
                Wind.North => "北",
                _ => wind.ToString(),
            };
        }
        /// <summary>
        /// 和了時の手牌（門前牌＋和了牌）を組み立てる
        /// ロンの和了牌はまだ手牌に含まれていないため、放銃者の最後の捨て牌から補う
        /// 和了牌を単に末尾へ追加すると手牌の並び順が崩れて見えるため、追加後に牌全体を並べ替える
        /// </summary>
        private IReadOnlyList<TileView> BuildWinningHandTiles(PlayerState winner, WinOutcome outcome, bool isTsumo)
        {
            var tiles = new List<Tile>(winner.Hand.Tiles);

            if (isTsumo)
            {
                if (winner.Hand.DrawnTile != null)
                {
                    tiles.Add(winner.Hand.DrawnTile);
                }
            }
            else
            {
                var discarder = _round.Players[outcome.DiscarderIndex.Value];
                tiles.Add(discarder.Discards[^1]);
            }

            return SortTiles(tiles).Select(TileView.FromModel).ToList();
        }
        /// <summary>
        /// 点数を表す表示文字列を組み立てる（例: "親ロン 11600点", "子ツモ 2000/3900点"）
        /// </summary>
        private static string FormatPointsLabel(bool isDealer, PaymentBreakdown payment)
        {
            if (payment.IsTsumo)
            {
                return isDealer
                    ? $"親ツモ {payment.NonDealerPaymentAmount}点オール"
                    : $"子ツモ {payment.NonDealerPaymentAmount}/{payment.DealerPaymentAmount}点";
            }

            return isDealer ? $"親ロン {payment.DiscarderAmount}点" : $"子ロン {payment.DiscarderAmount}点";
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

                    // 手牌（自分は2D、他家は伏せ牌）・河・副露はすべて3D表示に譲るため、
                    // テキストとしては出力しない
                }

                HumanHand.Value = BuildHumanHand();
                PlayerDiscards.Value = BuildPlayerDiscards();
                ConcealedHands.Value = BuildConcealedHands();
                PlayerMelds.Value = BuildPlayerMelds();
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
        /// 人間プレイヤーの手牌（門前牌 + ツモ牌）を、ツモ牌の位置とあわせて1つのスナップショットに変換する
        /// </summary>
        private HumanHandView BuildHumanHand()
        {
            var hand = _game.Players[_humanPlayerIndex].Hand;
            var tiles = hand.Tiles.Select(TileView.FromModel).ToList();

            if (hand.DrawnTile == null)
            {
                return new HumanHandView(tiles, HumanHandView.NO_DRAWN_TILE_INDEX);
            }

            tiles.Add(TileView.FromModel(hand.DrawnTile));
            return new HumanHandView(tiles, tiles.Count - 1);
        }
        /// <summary>
        /// 全プレイヤーの河を、自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）順に
        /// TileViewのリストへ変換する
        /// </summary>
        private IReadOnlyList<IReadOnlyList<TileView>> BuildPlayerDiscards()
        {
            var playerCount = _game.Players.Count;
            var result = new List<IReadOnlyList<TileView>>(playerCount);

            for (var offset = 0; offset < playerCount; offset++)
            {
                var playerIndex = (_humanPlayerIndex + offset) % playerCount;
                var discards = _game.Players[playerIndex].Discards.Select(TileView.FromModel).ToList();
                result.Add(discards);
            }

            return result;
        }
        /// <summary>
        /// 全プレイヤーの伏せ手牌を、自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）順に
        /// 組み立てる
        /// </summary>
        private IReadOnlyList<ConcealedHandView> BuildConcealedHands()
        {
            var playerCount = _game.Players.Count;
            var result = new List<ConcealedHandView>(playerCount);

            for (var offset = 0; offset < playerCount; offset++)
            {
                var playerIndex = (_humanPlayerIndex + offset) % playerCount;
                var hand = _game.Players[playerIndex].Hand;
                result.Add(new ConcealedHandView(hand.Tiles.Count, hand.DrawnTile != null));
            }

            return result;
        }
        /// <summary>
        /// 全プレイヤーの副露を、自分から見た相対位置（0=自分, 1=下家, 2=対面, 3=上家）順に
        /// 組み立てる
        /// </summary>
        private IReadOnlyList<IReadOnlyList<MeldView>> BuildPlayerMelds()
        {
            var playerCount = _game.Players.Count;
            var result = new List<IReadOnlyList<MeldView>>(playerCount);

            for (var offset = 0; offset < playerCount; offset++)
            {
                var playerIndex = (_humanPlayerIndex + offset) % playerCount;
                var player = _game.Players[playerIndex];
                var melds = player.Hand.Melds
                    .Select(meld => BuildMeldView(meld, player.SeatWind, playerCount))
                    .ToList();

                result.Add(melds);
            }

            return result;
        }
        /// <summary>
        /// 副露1組を表示用データに変換する
        /// </summary>
        private static MeldView BuildMeldView(Meld meld, Wind seatWind, int playerCount)
        {
            var rotatedTileIndex = ResolveRotatedTileIndex(meld, seatWind, playerCount);
            return new MeldView(BuildMeldTiles(meld, rotatedTileIndex), rotatedTileIndex);
        }
        /// <summary>
        /// 副露の牌を、卓上に並べる順に組み立てる
        /// 鳴いた牌を横向きに置く位置へ移し、残りの牌は牌の順に並べる
        /// Model層は「手牌から出した牌 + 鳴いた牌」の順で保持しているため、そのまま並べると
        /// 順子が数字順にならず、横向きになる牌も鳴いた牌とは限らなくなるため
        /// </summary>
        /// <param name="meld">変換する副露</param>
        /// <param name="rotatedTileIndex">横向きに置く牌のインデックス</param>
        private static IReadOnlyList<TileView> BuildMeldTiles(Meld meld, int rotatedTileIndex)
        {
            var tiles = new List<Tile>(meld.Tiles);

            // 暗槓は横向きにする牌が無いため、並べ替えずにそのまま渡す
            if (rotatedTileIndex == MeldView.NO_ROTATED_TILE_INDEX || !tiles.Remove(meld.StolenTile))
            {
                return meld.Tiles.Select(TileView.FromModel).ToList();
            }

            var sorted = SortTiles(tiles);
            sorted.Insert(rotatedTileIndex, meld.StolenTile);
            return sorted.Select(TileView.FromModel).ToList();
        }
        /// <summary>
        /// 牌をスーツ→数字→赤ドラの順に並べ替える
        /// </summary>
        private static List<Tile> SortTiles(IEnumerable<Tile> tiles)
        {
            return tiles
                .OrderBy(tile => tile.Suit)
                .ThenBy(tile => tile.Suit == TileSuit.Jihai ? (int)tile.Id : tile.Number)
                .ThenBy(tile => tile.IsRed ? 1 : 0)
                .ToList();
        }
        /// <summary>
        /// 鳴いた相手から、横向きに置く牌のインデックスを求める
        /// 上家からなら左端、対面なら左から2枚目、下家なら右端に置く（実際の卓上の慣習に合わせる）
        /// 三人麻雀には対面が無いため、上家・下家の2方向だけになる
        /// </summary>
        /// <returns>横向きに置く牌のインデックス。暗槓の場合は MeldView.NO_ROTATED_TILE_INDEX</returns>
        private static int ResolveRotatedTileIndex(Meld meld, Wind seatWind, int playerCount)
        {
            if (meld.FromWind == null)
            {
                return MeldView.NO_ROTATED_TILE_INDEX;
            }

            // 自風から見て何席後ろの相手かを求める（1=下家, playerCount-1=上家, 2=対面）
            var direction = ((int)meld.FromWind.Value - (int)seatWind + playerCount) % playerCount;

            if (direction == 1)
            {
                return meld.Tiles.Count - 1;
            }

            if (direction == playerCount - 1)
            {
                return 0;
            }

            return 1;
        }
    }
}
