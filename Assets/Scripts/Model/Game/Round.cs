using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;
using Mahjong.Model.Evaluation;
using Mahjong.Model.Evaluation.Internal;
using Mahjong.Model.Hands;
using Mahjong.Model.Scoring;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 1局（配牌から和了・流局まで）の進行を担う
    /// 実時間の待ち（CPU思考・入力待ち）は扱わない同期メソッド群として実装し、
    /// 呼び出し順の制御・待ち時間の管理は将来のPresenter層の責務とする
    /// </summary>
    public class Round
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 配牌の枚数
        /// </summary>
        private const int INITIAL_HAND_SIZE = 13;
        /// <summary>
        /// 面子1つ分の牌数（サイズ計算用。カンも1ブロックとして扱う）
        /// </summary>
        private const int MELD_BLOCK_SIZE = 3;
        /// <summary>
        /// リーチ棒1本の点数
        /// </summary>
        private const int RIICHI_STICK_VALUE = 1000;
        /// <summary>
        /// 四槓散了の判定に使うカン回数の上限
        /// </summary>
        private const int MAX_KAN_COUNT = 4;
        /// <summary>
        /// ドラ表示牌の最大枚数（表ドラ1 + カンドラ最大4）
        /// </summary>
        private const int MAX_DORA_INDICATORS = 5;
        /// <summary>
        /// 九種九牌に必要な么九牌の最小種類数
        /// </summary>
        private const int KYUUSHU_KYUUHAI_MIN_KINDS = 9;


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 対局設定
        /// </summary>
        public GameSettings Settings { get; }
        /// <summary>
        /// 参加プレイヤー（席順固定）
        /// </summary>
        public IReadOnlyList<PlayerState> Players { get; }
        /// <summary>
        /// 場風
        /// </summary>
        public Wind RoundWind { get; }
        /// <summary>
        /// 局数（東1局なら1）
        /// </summary>
        public int RoundNumber { get; }
        /// <summary>
        /// 本場数
        /// </summary>
        public int HonbaCount { get; private set; }
        /// <summary>
        /// 供託されているリーチ棒の本数
        /// </summary>
        public int RiichiStickCount { get; private set; }
        /// <summary>
        /// 親の席順
        /// </summary>
        public int DealerIndex { get; }
        /// <summary>
        /// 現在のターン進行フェーズ
        /// </summary>
        public TurnPhase Phase { get; private set; }
        /// <summary>
        /// 現在手番のプレイヤーの席順
        /// </summary>
        public int CurrentPlayerIndex { get; private set; }
        /// <summary>
        /// 通算ツモ回数（一発・海底・天和地和の判定に使用）
        /// </summary>
        public int TurnIndex { get; private set; }
        /// <summary>
        /// 山が尽きているかどうか
        /// </summary>
        public bool IsWallExhausted => _wall.IsEmpty;
        /// <summary>
        /// 途中流局が確定しているかどうか（四風連打・四槓散了など、打牌・副露の直後に自動検出されるもの）
        /// 非nullの場合、他の操作を続ける前に FinalizeAbortiveDraw を呼ぶ必要がある
        /// </summary>
        public AbortiveDrawReason? PendingAbortiveDraw { get; private set; }


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 山牌
        /// </summary>
        private readonly TileDeck _wall;
        /// <summary>
        /// プレイヤーごとに一度でもツモったことがあるか（天和・地和判定用）
        /// </summary>
        private readonly bool[] _hasDrawnBefore;
        /// <summary>
        /// カンを宣言したプレイヤーの履歴（四槓散了判定用）
        /// </summary>
        private readonly List<int> _kanDeclarers = new();
        /// <summary>
        /// 各プレイヤーの最初の打牌（四風連打判定用）
        /// </summary>
        private readonly List<Tile> _openingDiscards = new();
        /// <summary>
        /// 公開されているドラ表示牌の枚数
        /// </summary>
        private int _revealedDoraCount = 1;
        /// <summary>
        /// この局でまだ誰も鳴いていないかどうか（天和・地和・九種九牌の判定に使用）
        /// </summary>
        private bool _noCallsYet = true;
        /// <summary>
        /// 直前の DeclareRiichi 呼び出し直後の Discard 呼び出しかどうか（一発の誤消去防止用）
        /// </summary>
        private bool _justDeclaredRiichi;
        /// <summary>
        /// 直前のツモが海底（山の最後の1枚）だったかどうか
        /// </summary>
        private bool _lastDrawWasHaitei;
        /// <summary>
        /// 直前のツモが嶺上牌だったかどうか
        /// </summary>
        private bool _lastDrawWasRinshan;
        /// <summary>
        /// 直前のツモが現在のプレイヤーにとって最初のツモだったかどうか
        /// </summary>
        private bool _lastDrawWasFirstDrawForCurrentPlayer;
        /// <summary>
        /// 槍槓判定中かどうか（GetChankanRonCandidates / DeclareKakan の実行中のみ true）
        /// </summary>
        private bool _isChankanInProgress;
        /// <summary>
        /// 直前に打たれた牌（ResolveCalls が副露・ロンの組み立てに使用する）
        /// </summary>
        private Tile _lastDiscardedTile;


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 局を開始する（配牌・親の初回ツモまでを自動的に行う）
        /// </summary>
        /// <exception cref="ArgumentNullException">settings または players が null の場合</exception>
        /// <exception cref="ArgumentException">players の人数が settings.PlayerCount と一致しない場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">dealerIndex が範囲外の場合</exception>
        public Round(
            GameSettings settings,
            IReadOnlyList<PlayerState> players,
            Wind roundWind,
            int roundNumber,
            int dealerIndex,
            int honbaCount,
            int riichiStickCount,
            Random random = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "settings が null です");
            }

            if (players == null)
            {
                throw new ArgumentNullException(nameof(players), "players が null です");
            }

            if (players.Count != settings.PlayerCount)
            {
                throw new ArgumentException(
                    $"players の人数は settings.PlayerCount と一致する必要があります: {players.Count} / {settings.PlayerCount}", nameof(players));
            }

            if (dealerIndex < 0 || dealerIndex >= settings.PlayerCount)
            {
                throw new ArgumentOutOfRangeException(nameof(dealerIndex), $"dealerIndex が範囲外です: {dealerIndex}");
            }

            Settings = settings;
            Players = players;
            RoundWind = roundWind;
            RoundNumber = roundNumber;
            DealerIndex = dealerIndex;
            HonbaCount = honbaCount;
            RiichiStickCount = riichiStickCount;

            _wall = new TileDeck(settings.PlayerCount == 3, settings.UseRedDora, random);
            _hasDrawnBefore = new bool[settings.PlayerCount];

            AssignSeatWinds();
            DealInitialHands();

            CurrentPlayerIndex = dealerIndex;
            Phase = TurnPhase.AwaitingDraw;
            DrawTile();
        }


        // ========================================
        // パブリックメソッド（ツモ・和了）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーが山からツモる
        /// </summary>
        /// <exception cref="InvalidOperationException">AwaitingDraw フェーズでない場合、または途中流局が確定している場合</exception>
        public Tile DrawTile()
        {
            RequireNoPendingAbortiveDraw();
            RequirePhase(TurnPhase.AwaitingDraw);

            var player = Players[CurrentPlayerIndex];
            var tile = _wall.Draw();
            player.Hand.Draw(tile);

            _lastDrawWasHaitei = _wall.IsEmpty;
            _lastDrawWasRinshan = false;
            _lastDrawWasFirstDrawForCurrentPlayer = !_hasDrawnBefore[CurrentPlayerIndex];
            _hasDrawnBefore[CurrentPlayerIndex] = true;
            TurnIndex++;

            Phase = TurnPhase.AwaitingDiscard;
            return tile;
        }
        /// <summary>
        /// 現在のプレイヤーがツモ牌でツモ和了を宣言できるかどうか（役の有無まで確認する）
        /// </summary>
        public bool CanDeclareTsumoWin()
        {
            if (Phase != TurnPhase.AwaitingDiscard)
            {
                return false;
            }

            var player = Players[CurrentPlayerIndex];
            return player.Hand.DrawnTile != null && CanWin(player, player.Hand.DrawnTile, isTsumo: true);
        }
        /// <summary>
        /// ツモ和了を宣言し、点数を精算する
        /// </summary>
        /// <exception cref="InvalidOperationException">ツモ和了を宣言できない状態の場合</exception>
        public RoundResult DeclareTsumoWin()
        {
            if (!CanDeclareTsumoWin())
            {
                throw new InvalidOperationException("ツモ和了を宣言できない状態です");
            }

            var player = Players[CurrentPlayerIndex];
            var winningTile = player.Hand.DrawnTile;
            var score = ComputeWinScore(player, winningTile, isTsumo: true, RiichiStickCount);
            var deltas = BuildScoreDeltas(player.PlayerIndex, discarderIndex: null, score.Payment);
            ApplyDeltas(deltas);

            var dealerContinues = player.PlayerIndex == DealerIndex;
            RiichiStickCount = 0;
            Phase = TurnPhase.Ended;

            return new RoundResult(
                RoundEndReason.Tsumo,
                new[] { new WinOutcome(player.PlayerIndex, null, score) },
                deltas, dealerContinues);
        }


        // ========================================
        // パブリックメソッド（途中流局）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーが九種九牌を宣言できるかどうか
        /// 自分の最初のツモ番で、誰も鳴いておらず、么九牌が9種類以上ある場合のみ true
        /// </summary>
        public bool CanDeclareKyuushuKyuuhai()
        {
            if (Phase != TurnPhase.AwaitingDiscard || !_noCallsYet || !_lastDrawWasFirstDrawForCurrentPlayer)
            {
                return false;
            }

            var player = Players[CurrentPlayerIndex];
            var kinds = player.Hand.GetClosedTiles()
                .Where(t => t.IsYaochu)
                .Select(t => t.IsJihai ? (int)t.Id : TileKind.IndexOf(t))
                .Distinct()
                .Count();

            return kinds >= KYUUSHU_KYUUHAI_MIN_KINDS;
        }
        /// <summary>
        /// 九種九牌による途中流局を宣言する
        /// </summary>
        /// <exception cref="InvalidOperationException">九種九牌を宣言できない状態の場合</exception>
        public RoundResult DeclareKyuushuKyuuhai()
        {
            if (!CanDeclareKyuushuKyuuhai())
            {
                throw new InvalidOperationException("九種九牌を宣言できない状態です");
            }

            return TriggerAbortiveDraw(AbortiveDrawReason.KyuushuKyuuhai);
        }
        /// <summary>
        /// 自動検出された途中流局（四風連打・四槓散了・三家和）を確定させる
        /// </summary>
        /// <exception cref="InvalidOperationException">途中流局の条件が成立していない場合</exception>
        public RoundResult FinalizeAbortiveDraw()
        {
            if (PendingAbortiveDraw == null)
            {
                throw new InvalidOperationException("途中流局の条件が成立していません");
            }

            return TriggerAbortiveDraw(PendingAbortiveDraw.Value);
        }
        /// <summary>
        /// 荒牌平局（山が尽きた場合の流局）を宣言し、テンパイ・ノーテン精算を行う
        /// </summary>
        /// <exception cref="InvalidOperationException">山がまだ残っている場合</exception>
        public RoundResult DeclareExhaustiveDraw()
        {
            if (!IsWallExhausted)
            {
                throw new InvalidOperationException("山がまだ残っているため荒牌平局を宣言できません");
            }

            var tenpaiStates = Players.Select(p => FindTenpaiWaits(p.Hand).Count > 0).ToList();
            var deltas = NotenPaymentCalculator.Calculate(tenpaiStates, Settings.PlayerCount);
            ApplyDeltas(deltas);

            var dealerContinues = tenpaiStates[DealerIndex];
            Phase = TurnPhase.Ended;

            return new RoundResult(RoundEndReason.ExhaustiveDraw, wins: null, deltas, dealerContinues, tenpaiStates);
        }


        // ========================================
        // パブリックメソッド（リーチ）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーがリーチを宣言できるかどうか
        /// 門前・持ち点1000点以上・残り牌数・打牌後もテンパイを維持できることを確認する
        /// </summary>
        public bool CanDeclareRiichi()
        {
            if (Phase != TurnPhase.AwaitingDiscard)
            {
                return false;
            }

            var player = Players[CurrentPlayerIndex];

            if (player.HandState.IsRiichi || player.Hand.Melds.Count > 0)
            {
                return false;
            }

            if (player.Score < RIICHI_STICK_VALUE || _wall.RemainingCount < Settings.PlayerCount)
            {
                return false;
            }

            return HasDiscardThatKeepsTenpai(player.Hand);
        }
        /// <summary>
        /// リーチを宣言する（1000点減算・供託+1）。この直後に Discard を呼ぶこと
        /// </summary>
        /// <exception cref="InvalidOperationException">リーチを宣言できない状態の場合</exception>
        public void DeclareRiichi()
        {
            if (!CanDeclareRiichi())
            {
                throw new InvalidOperationException("リーチを宣言できない状態です");
            }

            var player = Players[CurrentPlayerIndex];
            player.HandState.DeclareRiichi(TurnIndex);
            player.AddScore(-RIICHI_STICK_VALUE);
            RiichiStickCount++;
            _justDeclaredRiichi = true;
        }


        // ========================================
        // パブリックメソッド（カン）
        // ========================================
        /// <summary>
        /// 指定した種類の牌で暗槓を宣言できるかどうか（手牌に4枚あるか）
        /// </summary>
        /// <exception cref="ArgumentNullException">kind が null の場合</exception>
        public bool CanDeclareAnKan(Tile kind)
        {
            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind), "kind が null です");
            }

            if (Phase != TurnPhase.AwaitingDiscard)
            {
                return false;
            }

            var player = Players[CurrentPlayerIndex];
            return player.Hand.GetClosedTiles().Count(t => t.IsSameType(kind)) >= 4;
        }
        /// <summary>
        /// 暗槓を宣言する。四槓散了が成立した場合はそのまま局を終了して結果を返す
        /// </summary>
        /// <returns>四槓散了が成立した場合は結果を返す。それ以外（続行）は null</returns>
        /// <exception cref="InvalidOperationException">暗槓を宣言できない状態の場合</exception>
        public RoundResult DeclareAnKan(Tile kind)
        {
            if (!CanDeclareAnKan(kind))
            {
                throw new InvalidOperationException("暗槓を宣言できる牌ではありません");
            }

            var player = Players[CurrentPlayerIndex];
            var tiles = player.Hand.GetClosedTiles().Where(t => t.IsSameType(kind)).Take(4).ToList();
            player.Hand.AddMeld(new Meld(MeldType.AnKan, tiles));

            if (RegisterKanAndCheckAbort(player.PlayerIndex))
            {
                return TriggerAbortiveDraw(AbortiveDrawReason.SuuKaiSanra);
            }

            CancelAllIppatsu();
            DrawRinshanTileForCurrentPlayer(countsAsRinshanKaihou: true);
            return null;
        }
        /// <summary>
        /// 指定した種類の牌で加槓を宣言できるかどうか（ポン済みの刻子があり、手牌に同種牌があるか）
        /// </summary>
        /// <exception cref="ArgumentNullException">kind が null の場合</exception>
        public bool CanDeclareKakan(Tile kind)
        {
            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind), "kind が null です");
            }

            if (Phase != TurnPhase.AwaitingDiscard)
            {
                return false;
            }

            var player = Players[CurrentPlayerIndex];
            return player.Hand.Melds.Any(m => m.Type == MeldType.Pon && m.Tiles[0].IsSameType(kind))
                && player.Hand.GetClosedTiles().Any(t => t.IsSameType(kind));
        }
        /// <summary>
        /// 加槓によって発生する槍槓ロンの候補者を列挙する
        /// DeclareKakan の前に呼び、他家の意思決定を集めるために使用する
        /// </summary>
        /// <exception cref="ArgumentNullException">kind が null の場合</exception>
        public IReadOnlyList<int> GetChankanRonCandidates(Tile kind)
        {
            if (kind == null)
            {
                throw new ArgumentNullException(nameof(kind), "kind が null です");
            }

            RequirePhase(TurnPhase.AwaitingDiscard);

            var candidates = new List<int>();
            _isChankanInProgress = true;

            for (var i = 0; i < Settings.PlayerCount; i++)
            {
                if (i != CurrentPlayerIndex && CanWin(Players[i], kind, isTsumo: false))
                {
                    candidates.Add(i);
                }
            }

            _isChankanInProgress = false;
            return candidates;
        }
        /// <summary>
        /// 加槓を確定させる
        /// chankanDeclarations にロン宣言が含まれる場合は槍槓として局を終了する（複数人の槍槓にも対応）
        /// 誰もロンしない場合は加槓を適用し、嶺上牌をツモって続行する
        /// </summary>
        /// <returns>槍槓が成立した場合は結果を返す。それ以外（続行）は null</returns>
        /// <exception cref="ArgumentNullException">chankanDeclarations が null の場合</exception>
        /// <exception cref="InvalidOperationException">加槓を宣言できない状態の場合</exception>
        public RoundResult DeclareKakan(Tile kind, IReadOnlyList<DeclaredCall> chankanDeclarations)
        {
            if (chankanDeclarations == null)
            {
                throw new ArgumentNullException(nameof(chankanDeclarations), "chankanDeclarations が null です");
            }

            if (!CanDeclareKakan(kind))
            {
                throw new InvalidOperationException("加槓を宣言できる牌ではありません");
            }

            var player = Players[CurrentPlayerIndex];
            var ronDeclarers = chankanDeclarations.Where(d => d.Type == CallType.Ron).Select(d => d.PlayerIndex).ToList();

            if (ronDeclarers.Count > 0)
            {
                _isChankanInProgress = true;
                var result = ResolveRon(ronDeclarers, CurrentPlayerIndex, kind);
                _isChankanInProgress = false;
                return result;
            }

            if (!player.Hand.AddKakan(kind))
            {
                throw new InvalidOperationException("加槓の適用に失敗しました");
            }

            if (RegisterKanAndCheckAbort(player.PlayerIndex))
            {
                return TriggerAbortiveDraw(AbortiveDrawReason.SuuKaiSanra);
            }

            CancelAllIppatsu();
            DrawRinshanTileForCurrentPlayer(countsAsRinshanKaihou: true);
            return null;
        }


        // ========================================
        // パブリックメソッド（北抜き：三人麻雀）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーが北抜きを宣言できるかどうか（三人麻雀・北抜き設定有効時のみ）
        /// </summary>
        public bool CanDeclareKitaNuki()
        {
            if (!Settings.UseKitaNuki || Phase != TurnPhase.AwaitingDiscard)
            {
                return false;
            }

            var drawnTile = Players[CurrentPlayerIndex].Hand.DrawnTile;
            return drawnTile != null && drawnTile.Id == TileId.North;
        }
        /// <summary>
        /// 北抜きを宣言する。北を手牌から除外し、嶺上牌から補充する
        /// </summary>
        /// <exception cref="InvalidOperationException">北抜きを宣言できない状態の場合</exception>
        public void DeclareKitaNuki()
        {
            if (!CanDeclareKitaNuki())
            {
                throw new InvalidOperationException("北抜きを宣言できない状態です");
            }

            var player = Players[CurrentPlayerIndex];
            player.Hand.Discard(player.Hand.DrawnTile);
            player.AddKita();

            _noCallsYet = false;
            CancelAllIppatsu();
            DrawRinshanTileForCurrentPlayer(countsAsRinshanKaihou: false);
        }


        // ========================================
        // パブリックメソッド（打牌・他家の反応）
        // ========================================
        /// <summary>
        /// 現在のプレイヤーが牌を打つ
        /// 河への記録・フリテン更新・四風連打の自動判定までを行う
        /// 呼び出し後は PendingAbortiveDraw を確認し、非nullなら FinalizeAbortiveDraw を呼ぶこと
        /// </summary>
        /// <exception cref="ArgumentNullException">tile が null の場合</exception>
        /// <exception cref="InvalidOperationException">AwaitingDiscard フェーズでない場合</exception>
        public Tile Discard(Tile tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile), "tile が null です");
            }

            RequirePhase(TurnPhase.AwaitingDiscard);

            var player = Players[CurrentPlayerIndex];

            if (player.HandState.IsRiichi && player.HandState.IppatsuAvailable && !_justDeclaredRiichi)
            {
                player.HandState.CancelIppatsu();
            }
            _justDeclaredRiichi = false;

            var discarded = player.Hand.Discard(tile);
            player.HandState.AddDiscard(discarded);
            player.AddDiscard(discarded);
            _lastDiscardedTile = discarded;

            UpdateFuriten(player);
            CheckSuufuurenda(player, discarded);

            Phase = TurnPhase.AwaitingReactions;
            return discarded;
        }
        /// <summary>
        /// 直前の捨て牌に対して各プレイヤーが宣言できる選択肢を列挙する
        /// ロンは役の有無・フリテンまで確認したうえで候補に含める
        /// </summary>
        /// <exception cref="ArgumentNullException">discardedTile が null の場合</exception>
        /// <exception cref="ArgumentException">discarderIndex が現在の打牌者と一致しない場合</exception>
        public IReadOnlyList<CallOption> GetAvailableCalls(Tile discardedTile, int discarderIndex)
        {
            if (discardedTile == null)
            {
                throw new ArgumentNullException(nameof(discardedTile), "discardedTile が null です");
            }

            if (discarderIndex != CurrentPlayerIndex)
            {
                throw new ArgumentException("discarderIndex が現在の打牌者と一致しません", nameof(discarderIndex));
            }

            RequireNoPendingAbortiveDraw();
            RequirePhase(TurnPhase.AwaitingReactions);

            var options = new List<CallOption>();

            for (var i = 0; i < Settings.PlayerCount; i++)
            {
                if (i == discarderIndex)
                {
                    continue;
                }

                var player = Players[i];

                if (CanWin(player, discardedTile, isTsumo: false))
                {
                    options.Add(new CallOption(i, CallType.Ron, new List<IReadOnlyList<Tile>> { new List<Tile> { discardedTile } }));
                }

                var matching = player.Hand.GetClosedTiles().Where(t => t.IsSameType(discardedTile)).ToList();

                if (matching.Count >= 3)
                {
                    options.Add(new CallOption(i, CallType.Kan, new List<IReadOnlyList<Tile>> { matching.Take(3).ToList() }));
                }

                if (matching.Count >= 2)
                {
                    options.Add(new CallOption(i, CallType.Pon, new List<IReadOnlyList<Tile>> { matching.Take(2).ToList() }));
                }

                var isKamichaOfI = discarderIndex == (i - 1 + Settings.PlayerCount) % Settings.PlayerCount;
                var chiAllowed = Settings.PlayerCount == 4 || Settings.AllowChiInThreePlayer;

                if (isKamichaOfI && chiAllowed && discardedTile.Suit != TileSuit.Jihai)
                {
                    var chiCandidates = FindChiCandidates(player, discardedTile);

                    if (chiCandidates.Count > 0)
                    {
                        options.Add(new CallOption(i, CallType.Chi, chiCandidates));
                    }
                }
            }

            return options;
        }
        /// <summary>
        /// 収集した宣言をもとに、優先順位（ロン &gt; ポン・カン &gt; チー）に従って結果を確定する
        /// ロンが複数人いる場合は個別に精算し、四人麻雀で3人以上なら三家和として打ち切る
        /// 副露・カンが確定した場合は手番をその宣言者に移す
        /// </summary>
        /// <returns>局が終了した場合は結果を返す。継続する場合は null</returns>
        /// <exception cref="ArgumentNullException">declarations が null の場合</exception>
        public RoundResult ResolveCalls(IReadOnlyList<DeclaredCall> declarations)
        {
            if (declarations == null)
            {
                throw new ArgumentNullException(nameof(declarations), "declarations が null です");
            }

            RequireNoPendingAbortiveDraw();
            RequirePhase(TurnPhase.AwaitingReactions);

            var discarderIndex = CurrentPlayerIndex;
            var discardedTile = _lastDiscardedTile;

            var ronDeclarers = declarations.Where(d => d.Type == CallType.Ron).Select(d => d.PlayerIndex).ToList();

            if (ronDeclarers.Count > 0)
            {
                return ResolveRon(ronDeclarers, discarderIndex, discardedTile);
            }

            var meldDeclaration = ChooseMeldDeclaration(declarations, discarderIndex);

            if (meldDeclaration == null)
            {
                CurrentPlayerIndex = (discarderIndex + 1) % Settings.PlayerCount;
                Phase = TurnPhase.AwaitingDraw;
                return null;
            }

            ApplyMeld(meldDeclaration, discarderIndex, discardedTile);
            _noCallsYet = false;
            CancelAllIppatsu();
            CurrentPlayerIndex = meldDeclaration.PlayerIndex;

            if (meldDeclaration.Type == CallType.Kan)
            {
                if (RegisterKanAndCheckAbort(meldDeclaration.PlayerIndex))
                {
                    return TriggerAbortiveDraw(AbortiveDrawReason.SuuKaiSanra);
                }

                DrawRinshanTileForCurrentPlayer(countsAsRinshanKaihou: true);
                return null;
            }

            Phase = TurnPhase.AwaitingDiscard;
            return null;
        }


        // ========================================
        // プライベートメソッド（和了判定・点数計算）
        // ========================================
        /// <summary>
        /// 形の成立と役の有無の両方を確認する（役なし和了は不可）
        /// </summary>
        private bool CanWin(PlayerState player, Tile winningTile, bool isTsumo)
        {
            if (!isTsumo && player.HandState.IsFuriten)
            {
                return false;
            }

            var agari = AgariChecker.CheckWin(player.Hand, winningTile, isTsumo);

            if (!agari.IsWin)
            {
                return false;
            }

            var context = BuildWinContext(player, isTsumo);
            var yaku = YakuEvaluator.Evaluate(player.Hand, winningTile, agari, context);
            return yaku.IsYakuman || yaku.TotalHan > 0;
        }
        /// <summary>
        /// 状況フラグを現在のRound状態から組み立てる
        /// </summary>
        private WinContext BuildWinContext(PlayerState player, bool isTsumo)
        {
            var isDealer = player.PlayerIndex == DealerIndex;

            return new WinContext(
                isTsumo: isTsumo,
                isRiichi: player.HandState.IsRiichi,
                isIppatsu: player.HandState.IppatsuAvailable,
                isHaitei: isTsumo && _lastDrawWasHaitei,
                isHoutei: !isTsumo && _wall.IsEmpty,
                isRinshan: isTsumo && _lastDrawWasRinshan,
                isChankan: !isTsumo && _isChankanInProgress,
                isTenhou: isTsumo && isDealer && _lastDrawWasFirstDrawForCurrentPlayer && _noCallsYet,
                isChiihou: isTsumo && !isDealer && _lastDrawWasFirstDrawForCurrentPlayer && _noCallsYet,
                seatWind: player.SeatWind,
                roundWind: RoundWind,
                isDealer: isDealer);
        }
        /// <summary>
        /// 和了点を計算する（ドラ・裏ドラ・北抜きボーナスを合算して ScoreCalculator に渡す）
        /// </summary>
        private ScoreResult ComputeWinScore(PlayerState player, Tile winningTile, bool isTsumo, int riichiSticksForThisWinner)
        {
            var context = BuildWinContext(player, isTsumo);
            var agari = AgariChecker.CheckWin(player.Hand, winningTile, isTsumo);
            var yaku = YakuEvaluator.Evaluate(player.Hand, winningTile, agari, context);

            var allTiles = player.Hand.GetAllTiles();

            if (!isTsumo)
            {
                allTiles.Add(winningTile);
            }

            var bonusHan = CountDora(allTiles, GetActiveDoraIndicators());

            if (player.HandState.IsRiichi)
            {
                bonusHan += CountDora(allTiles, GetActiveUraDoraIndicators());
            }

            bonusHan += player.KitaCount;

            return ScoreCalculator.Calculate(
                player.Hand, winningTile, yaku, context, HonbaCount, riichiSticksForThisWinner, bonusHan, Settings.PlayerCount);
        }
        /// <summary>
        /// ロンを解決する（三家和・ダブロンの個別精算を含む）
        /// リーチ棒の供託は打牌者から見て手番が近いプレイヤー1名のみが受け取る
        /// </summary>
        private RoundResult ResolveRon(List<int> winnerIndices, int discarderIndex, Tile winningTile)
        {
            if (Settings.PlayerCount == 4 && winnerIndices.Count >= 3)
            {
                return TriggerAbortiveDraw(AbortiveDrawReason.Sanchaho);
            }

            var ordered = winnerIndices
                .OrderBy(idx => (idx - discarderIndex + Settings.PlayerCount) % Settings.PlayerCount)
                .ToList();

            var wins = new List<WinOutcome>();
            var deltas = new int[Settings.PlayerCount];
            var dealerContinues = false;

            for (var i = 0; i < ordered.Count; i++)
            {
                var winnerIndex = ordered[i];
                var player = Players[winnerIndex];
                var riichiSticksForThisWinner = i == 0 ? RiichiStickCount : 0;
                var score = ComputeWinScore(player, winningTile, isTsumo: false, riichiSticksForThisWinner);

                wins.Add(new WinOutcome(winnerIndex, discarderIndex, score));

                var winnerDeltas = BuildScoreDeltas(winnerIndex, discarderIndex, score.Payment);

                for (var p = 0; p < Settings.PlayerCount; p++)
                {
                    deltas[p] += winnerDeltas[p];
                }

                if (winnerIndex == DealerIndex)
                {
                    dealerContinues = true;
                }
            }

            ApplyDeltas(deltas);
            RiichiStickCount = 0;
            Phase = TurnPhase.Ended;

            return new RoundResult(RoundEndReason.Ron, wins, deltas, dealerContinues);
        }
        /// <summary>
        /// 役割ベースの支払い内訳（PaymentBreakdown）を席順ベースの点数増減に変換する
        /// </summary>
        private int[] BuildScoreDeltas(int winnerIndex, int? discarderIndex, PaymentBreakdown payment)
        {
            var deltas = new int[Settings.PlayerCount];
            deltas[winnerIndex] += payment.TotalWinnerGain;

            if (!payment.IsTsumo)
            {
                deltas[discarderIndex.Value] -= payment.DiscarderAmount;
                return deltas;
            }

            if (winnerIndex == DealerIndex)
            {
                for (var i = 0; i < Settings.PlayerCount; i++)
                {
                    if (i != winnerIndex)
                    {
                        deltas[i] -= payment.NonDealerPaymentAmount;
                    }
                }
            }
            else
            {
                deltas[DealerIndex] -= payment.DealerPaymentAmount;

                for (var i = 0; i < Settings.PlayerCount; i++)
                {
                    if (i != winnerIndex && i != DealerIndex)
                    {
                        deltas[i] -= payment.NonDealerPaymentAmount;
                    }
                }
            }

            return deltas;
        }
        /// <summary>
        /// 点数増減を各プレイヤーの持ち点に反映する
        /// </summary>
        private void ApplyDeltas(IReadOnlyList<int> deltas)
        {
            for (var i = 0; i < Settings.PlayerCount; i++)
            {
                Players[i].AddScore(deltas[i]);
            }
        }


        // ========================================
        // プライベートメソッド（副露の解決）
        // ========================================
        /// <summary>
        /// ロン以外の宣言から、優先順位（カン・ポン &gt; チー）と打牌者からの近さで採用する1件を選ぶ
        /// </summary>
        private DeclaredCall ChooseMeldDeclaration(IReadOnlyList<DeclaredCall> declarations, int discarderIndex)
        {
            foreach (var type in new[] { CallType.Kan, CallType.Pon, CallType.Chi })
            {
                var candidates = declarations.Where(d => d.Type == type).ToList();

                if (candidates.Count == 0)
                {
                    continue;
                }

                return candidates
                    .OrderBy(d => (d.PlayerIndex - discarderIndex + Settings.PlayerCount) % Settings.PlayerCount)
                    .First();
            }

            return null;
        }
        /// <summary>
        /// 採用された宣言から Meld を組み立て、宣言者の手牌に追加する
        /// </summary>
        private void ApplyMeld(DeclaredCall declaration, int discarderIndex, Tile discardedTile)
        {
            var player = Players[declaration.PlayerIndex];
            var fromWind = Players[discarderIndex].SeatWind;

            var meldType = declaration.Type switch
            {
                CallType.Chi => MeldType.Chi,
                CallType.Pon => MeldType.Pon,
                CallType.Kan => MeldType.DaiMinKan,
                _ => throw new InvalidOperationException($"副露として扱えない CallType です: {declaration.Type}"),
            };

            var tiles = new List<Tile>(declaration.SelectedTiles) { discardedTile };
            player.Hand.AddMeld(new Meld(meldType, tiles, discardedTile, fromWind));
        }
        /// <summary>
        /// チーの候補パターン（0〜3通り）を列挙する
        /// </summary>
        private static List<IReadOnlyList<Tile>> FindChiCandidates(PlayerState player, Tile discardedTile)
        {
            var candidates = new List<IReadOnlyList<Tile>>();
            var closed = player.Hand.GetClosedTiles();
            var n = discardedTile.Number;

            foreach (var pattern in new[] { new[] { n - 2, n - 1 }, new[] { n - 1, n + 1 }, new[] { n + 1, n + 2 } })
            {
                if (pattern[0] < 1 || pattern[1] > 9)
                {
                    continue;
                }

                var first = closed.FirstOrDefault(t => t.Suit == discardedTile.Suit && t.Number == pattern[0]);

                if (first == null)
                {
                    continue;
                }

                var second = closed.FirstOrDefault(t =>
                    t.Suit == discardedTile.Suit && t.Number == pattern[1] && !ReferenceEquals(t, first));

                if (second == null)
                {
                    continue;
                }

                candidates.Add(new List<Tile> { first, second });
            }

            return candidates;
        }


        // ========================================
        // プライベートメソッド（カン・嶺上牌・ドラ）
        // ========================================
        /// <summary>
        /// カンの成立を記録し、ドラ表示牌を1枚追加公開する
        /// </summary>
        /// <returns>四槓散了（4回のカンが2人以上にまたがった）が成立したかどうか</returns>
        private bool RegisterKanAndCheckAbort(int playerIndex)
        {
            _kanDeclarers.Add(playerIndex);
            _revealedDoraCount = Math.Min(_revealedDoraCount + 1, MAX_DORA_INDICATORS);
            _noCallsYet = false;

            return _kanDeclarers.Count >= MAX_KAN_COUNT && _kanDeclarers.Distinct().Count() > 1;
        }
        /// <summary>
        /// 現在のプレイヤーが嶺上牌をツモる
        /// </summary>
        private void DrawRinshanTileForCurrentPlayer(bool countsAsRinshanKaihou)
        {
            var player = Players[CurrentPlayerIndex];
            var tile = _wall.DrawRinshan();
            player.Hand.Draw(tile);

            _lastDrawWasHaitei = false;
            _lastDrawWasRinshan = countsAsRinshanKaihou;
            _lastDrawWasFirstDrawForCurrentPlayer = false;

            Phase = TurnPhase.AwaitingDiscard;
        }
        /// <summary>
        /// 現在公開されているドラ表示牌を返す
        /// </summary>
        private IReadOnlyList<Tile> GetActiveDoraIndicators()
        {
            var indicators = new List<Tile>();

            for (var i = 0; i < _revealedDoraCount; i++)
            {
                indicators.Add(_wall.DeadWall[_wall.DeadWall.Count - 1 - i * 2]);
            }

            return indicators;
        }
        /// <summary>
        /// 現在公開されている裏ドラ表示牌を返す（表ドラと同数、ペアで隣接した位置から取る）
        /// </summary>
        private IReadOnlyList<Tile> GetActiveUraDoraIndicators()
        {
            var indicators = new List<Tile>();

            for (var i = 0; i < _revealedDoraCount; i++)
            {
                indicators.Add(_wall.DeadWall[_wall.DeadWall.Count - 1 - (i * 2 + 1)]);
            }

            return indicators;
        }
        /// <summary>
        /// 指定した表示牌群がもたらすドラの合計枚数を数える
        /// </summary>
        private int CountDora(IReadOnlyList<Tile> handTiles, IReadOnlyList<Tile> indicators)
        {
            var total = 0;

            foreach (var indicator in indicators)
            {
                var doraTile = GetIndicatedDoraTile(indicator);
                total += handTiles.Count(t => t.IsSameType(doraTile));
            }

            return total;
        }
        /// <summary>
        /// ドラ表示牌から実際のドラ牌を求める（数牌は+1、字牌は東南西北・白發中の順で循環）
        /// 三人麻雀は萬子の2〜8が存在しないため、1と9の間で折り返す
        /// </summary>
        private Tile GetIndicatedDoraTile(Tile indicator)
        {
            if (indicator.Suit == TileSuit.Jihai)
            {
                if (TileClassification.IsWind(indicator))
                {
                    var nextWind = indicator.Id switch
                    {
                        TileId.East => TileId.South,
                        TileId.South => TileId.West,
                        TileId.West => TileId.North,
                        TileId.North => TileId.East,
                        _ => throw new InvalidOperationException($"風牌ではない TileId です: {indicator.Id}"),
                    };
                    return new Tile(nextWind, TileSuit.Jihai, 0);
                }

                var nextDragon = indicator.Id switch
                {
                    TileId.Haku => TileId.Hatsu,
                    TileId.Hatsu => TileId.Chun,
                    TileId.Chun => TileId.Haku,
                    _ => throw new InvalidOperationException($"三元牌ではない TileId です: {indicator.Id}"),
                };
                return new Tile(nextDragon, TileSuit.Jihai, 0);
            }

            // 三人麻雀の萬子は1と9のみが存在するため、1↔9で折り返す
            if (Settings.PlayerCount == 3 && indicator.Suit == TileSuit.Manzu)
            {
                return CreateSuitedTile(indicator.Suit, indicator.Number == 1 ? 9 : 1);
            }

            var nextNumber = indicator.Number == 9 ? 1 : indicator.Number + 1;
            return CreateSuitedTile(indicator.Suit, nextNumber);
        }
        /// <summary>
        /// スートと数字から通常牌（赤ドラではない）の Tile を生成する
        /// </summary>
        private static Tile CreateSuitedTile(TileSuit suit, int number)
        {
            var id = suit switch
            {
                TileSuit.Manzu => number switch
                {
                    1 => TileId.Manzu1, 2 => TileId.Manzu2, 3 => TileId.Manzu3, 4 => TileId.Manzu4, 5 => TileId.Manzu5,
                    6 => TileId.Manzu6, 7 => TileId.Manzu7, 8 => TileId.Manzu8, 9 => TileId.Manzu9,
                    _ => throw new ArgumentOutOfRangeException(nameof(number), $"数字として不正な値です: {number}"),
                },
                TileSuit.Pinzu => number switch
                {
                    1 => TileId.Pinzu1, 2 => TileId.Pinzu2, 3 => TileId.Pinzu3, 4 => TileId.Pinzu4, 5 => TileId.Pinzu5,
                    6 => TileId.Pinzu6, 7 => TileId.Pinzu7, 8 => TileId.Pinzu8, 9 => TileId.Pinzu9,
                    _ => throw new ArgumentOutOfRangeException(nameof(number), $"数字として不正な値です: {number}"),
                },
                TileSuit.Souzu => number switch
                {
                    1 => TileId.Souzu1, 2 => TileId.Souzu2, 3 => TileId.Souzu3, 4 => TileId.Souzu4, 5 => TileId.Souzu5,
                    6 => TileId.Souzu6, 7 => TileId.Souzu7, 8 => TileId.Souzu8, 9 => TileId.Souzu9,
                    _ => throw new ArgumentOutOfRangeException(nameof(number), $"数字として不正な値です: {number}"),
                },
                _ => throw new ArgumentException($"数牌ではない TileSuit です: {suit}", nameof(suit)),
            };

            return new Tile(id, suit, number);
        }


        // ========================================
        // プライベートメソッド（配牌・自風）
        // ========================================
        /// <summary>
        /// 親を東として、席順に沿って自風を割り当てる
        /// </summary>
        private void AssignSeatWinds()
        {
            var order = Settings.PlayerCount == 3
                ? new[] { Wind.East, Wind.South, Wind.West }
                : new[] { Wind.East, Wind.South, Wind.West, Wind.North };

            for (var offset = 0; offset < Settings.PlayerCount; offset++)
            {
                var playerIndex = (DealerIndex + offset) % Settings.PlayerCount;
                Players[playerIndex].AssignSeatWind(order[offset]);
            }
        }
        /// <summary>
        /// 全プレイヤーに配牌する
        /// </summary>
        private void DealInitialHands()
        {
            foreach (var player in Players)
            {
                player.ResetForNewRound();
                var tiles = new List<Tile>();

                for (var i = 0; i < INITIAL_HAND_SIZE; i++)
                {
                    tiles.Add(_wall.Draw());
                }

                player.Hand.SetInitialTiles(tiles);
            }
        }


        // ========================================
        // プライベートメソッド（テンパイ・フリテン・途中流局の自動検出）
        // ========================================
        /// <summary>
        /// 打牌後にフリテン状態を更新する（副露済みの手牌にも対応した独自の待ち判定を使用する）
        /// </summary>
        private void UpdateFuriten(PlayerState player)
        {
            player.HandState.UpdateFuriten(FindTenpaiWaits(player.Hand));
        }
        /// <summary>
        /// 手牌の待ち牌を列挙する
        /// WaitingTileFinder は門前13枚専用のため、副露済みの手牌にも対応できるようここで独自に実装する
        /// </summary>
        private static IReadOnlyList<Tile> FindTenpaiWaits(Hand hand)
        {
            var expectedClosedCount = INITIAL_HAND_SIZE - hand.Melds.Count * MELD_BLOCK_SIZE;

            if (hand.GetClosedTiles().Count != expectedClosedCount)
            {
                return Array.Empty<Tile>();
            }

            var waits = new List<Tile>();

            for (var kindIndex = 0; kindIndex < TileKind.KIND_COUNT; kindIndex++)
            {
                var candidate = TileKind.CreateRepresentativeTile(kindIndex);

                if (AgariChecker.CheckWin(hand, candidate, isTsumo: false).IsWin)
                {
                    waits.Add(candidate);
                }
            }

            return waits;
        }
        /// <summary>
        /// 現在の14枚（手牌+ツモ牌）のいずれかを切ればテンパイを維持できるかどうか
        /// </summary>
        private static bool HasDiscardThatKeepsTenpai(Hand hand)
        {
            var allTiles = hand.GetClosedTiles();

            var distinctByKind = allTiles
                .GroupBy(t => t.IsJihai ? (object)t.Id : (t.Suit, t.Number))
                .Select(g => g.First());

            foreach (var candidate in distinctByKind)
            {
                var remaining = new List<Tile>(allTiles);
                remaining.Remove(candidate);

                var tempHand = new Hand();
                tempHand.SetInitialTiles(remaining);

                if (ShantenCalculator.Calculate(tempHand) == 0)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 全プレイヤーの一発を消す（副露・カンの成立時に呼ぶ）
        /// </summary>
        private void CancelAllIppatsu()
        {
            foreach (var player in Players)
            {
                if (player.HandState.IppatsuAvailable)
                {
                    player.HandState.CancelIppatsu();
                }
            }
        }
        /// <summary>
        /// 四風連打（誰も鳴かないまま、全員の最初の打牌が同じ風牌）を検出する
        /// </summary>
        private void CheckSuufuurenda(PlayerState player, Tile discarded)
        {
            if (!_noCallsYet || player.Discards.Count != 1 || !TileClassification.IsWind(discarded))
            {
                return;
            }

            _openingDiscards.Add(discarded);

            if (_openingDiscards.Count == Settings.PlayerCount && _openingDiscards.All(t => t.IsSameType(_openingDiscards[0])))
            {
                PendingAbortiveDraw = AbortiveDrawReason.SuufuRenda;
            }
        }
        /// <summary>
        /// 途中流局を確定させ、局を終了する（点数の授受はなく、親は必ず連荘する）
        /// </summary>
        private RoundResult TriggerAbortiveDraw(AbortiveDrawReason reason)
        {
            Phase = TurnPhase.Ended;
            PendingAbortiveDraw = null;

            return new RoundResult(
                RoundEndReason.AbortiveDraw, wins: null, scoreDeltas: new int[Settings.PlayerCount],
                dealerContinues: true, abortiveReason: reason);
        }


        // ========================================
        // プライベートメソッド（フェーズ制御）
        // ========================================
        /// <summary>
        /// 現在のフェーズが期待値と一致するか検証する
        /// </summary>
        private void RequirePhase(TurnPhase expected)
        {
            if (Phase != expected)
            {
                throw new InvalidOperationException($"この操作は {expected} フェーズでのみ可能です。現在のフェーズ: {Phase}");
            }
        }
        /// <summary>
        /// 途中流局が確定していないか検証する
        /// </summary>
        private void RequireNoPendingAbortiveDraw()
        {
            if (PendingAbortiveDraw != null)
            {
                throw new InvalidOperationException("途中流局が確定しています。FinalizeAbortiveDraw を呼んでください");
            }
        }
    }
}
