using System;
using System.Collections.Generic;
using System.Linq;
using Mahjong.Model.Common;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 対局全体（東風戦・半荘戦）の進行を担う
    /// Round を1局ずつ生成・消費し、親交代・連荘・本場・供託の繰り越し・ゲーム終了判定を行う
    /// </summary>
    public class MahjongGame
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 対局設定
        /// </summary>
        public GameSettings Settings { get; }
        /// <summary>
        /// 参加プレイヤー（席順固定。Round と同じインスタンスを共有する）
        /// </summary>
        public IReadOnlyList<PlayerState> Players { get; }
        /// <summary>
        /// 現在の場風
        /// </summary>
        public Wind CurrentRoundWind { get; private set; }
        /// <summary>
        /// 現在の局数（東1局なら1）
        /// </summary>
        public int CurrentRoundNumber { get; private set; }
        /// <summary>
        /// 現在の親の席順
        /// </summary>
        public int DealerIndex { get; private set; }
        /// <summary>
        /// 本場数
        /// </summary>
        public int HonbaCount { get; private set; }
        /// <summary>
        /// 供託されているリーチ棒の本数
        /// </summary>
        public int RiichiStickCount { get; private set; }
        /// <summary>
        /// ゲームが終了したかどうか
        /// </summary>
        public bool IsGameOver { get; private set; }
        /// <summary>
        /// 現在進行中の局。ApplyRoundResult を呼ぶまでは同じ局を指し続ける
        /// </summary>
        public Round CurrentRound { get; private set; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 対局を初期化する。起家はサイコロの代わりに random で決定する
        /// </summary>
        /// <exception cref="ArgumentNullException">settings が null の場合</exception>
        public MahjongGame(GameSettings settings, Random random = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "settings が null です");
            }

            Settings = settings;

            var players = new List<PlayerState>();
            for (var i = 0; i < settings.PlayerCount; i++)
            {
                players.Add(new PlayerState(i, settings.InitialScore));
            }
            Players = players;

            CurrentRoundWind = Wind.East;
            CurrentRoundNumber = 1;
            DealerIndex = (random ?? new Random()).Next(settings.PlayerCount);
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 現在の場風・局数・親・本場・供託から次の局を開始する
        /// </summary>
        /// <exception cref="InvalidOperationException">ゲームが終了している場合、または現在の局がまだ終了していない場合</exception>
        public Round StartNextRound(Random random = null)
        {
            if (IsGameOver)
            {
                throw new InvalidOperationException("ゲームが終了しています");
            }

            if (CurrentRound != null && CurrentRound.Phase != TurnPhase.Ended)
            {
                throw new InvalidOperationException("現在の局がまだ終了していません");
            }

            CurrentRound = new Round(
                Settings, Players, CurrentRoundWind, CurrentRoundNumber, DealerIndex, HonbaCount, RiichiStickCount, random);
            return CurrentRound;
        }
        /// <summary>
        /// 局の結果を対局全体に反映する
        /// 持ち点は Round が既に PlayerState へ反映済みのため、ここでは再適用しない
        /// 親交代・連荘・本場・供託の繰り越し・場風の進行・ゲーム終了判定のみを行う
        /// </summary>
        /// <exception cref="ArgumentNullException">result が null の場合</exception>
        /// <exception cref="InvalidOperationException">進行中の局がない場合</exception>
        public void ApplyRoundResult(RoundResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "result が null です");
            }

            if (CurrentRound == null)
            {
                throw new InvalidOperationException("進行中の局がありません");
            }

            RiichiStickCount = CurrentRound.RiichiStickCount;

            if (result.DealerContinues)
            {
                HonbaCount++;
            }
            else
            {
                HonbaCount = 0;
                DealerIndex = (DealerIndex + 1) % Settings.PlayerCount;
                AdvanceRoundNumber();
            }

            CheckGameEnd();
        }


        // ========================================
        // プライベートメソッド
        // ========================================
        /// <summary>
        /// 局数を進め、必要なら場風を進行させる（東場が終われば半荘戦のみ南場へ）
        /// </summary>
        private void AdvanceRoundNumber()
        {
            CurrentRoundNumber++;

            // 場風1つあたりの局数はプレイヤー人数と一致する（全員が1回ずつ親を務める）
            var roundsPerWind = Settings.PlayerCount;

            if (CurrentRoundNumber <= roundsPerWind)
            {
                return;
            }

            CurrentRoundNumber = 1;

            if (CurrentRoundWind == Wind.East && Settings.GameLength == GameLengthType.HalfGame)
            {
                CurrentRoundWind = Wind.South;
            }
            else
            {
                IsGameOver = true;
            }
        }
        /// <summary>
        /// 飛び（持ち点0点未満）によるゲーム終了を判定する
        /// </summary>
        private void CheckGameEnd()
        {
            if (Settings.EnableTobi && Players.Any(p => p.Score < 0))
            {
                IsGameOver = true;
            }
        }
    }
}
