using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using R3;
using UnityEngine;

namespace Mahjong.Presenter
{
    /// <summary>
    /// タイトル→モード選択→設定→対局→ゲーム終了 の画面遷移をUniTaskで進行させる
    /// View層はこれを購読し、ボタン操作を SubmitXxx/ConfirmXxx で送り返す（HumanPlayerController と同じ設計）
    /// GamePresenter の生成・実行のみを担当し、InGameView 等View層のコンポーネントには一切触れない
    /// （それらの取り付けは AppFlowView が ActiveGame を購読して行う）
    /// </summary>
    public sealed class AppFlowPresenter : MonoBehaviour
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 人間が操作する席（席決めUIは今回のスコープ外のため固定）
        /// </summary>
        private const int HUMAN_PLAYER_INDEX = 0;


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 現在の画面状態
        /// </summary>
        public ReactiveProperty<AppScreen> CurrentScreen { get; } = new(AppScreen.Title);
        /// <summary>
        /// 対局中の GamePresenter
        /// View層はこれが非nullになったら対局用のView（InGameView等）を取り付け、
        /// nullに戻ったら対局が終了したものとして扱う
        /// </summary>
        public ReactiveProperty<GamePresenter> ActiveGame { get; } = new(null);
        /// <summary>
        /// 直近のゲーム終了時の最終順位
        /// </summary>
        public ReactiveProperty<GameOverSummaryView> FinalSummary { get; } = new(null);
        /// <summary>
        /// モード選択画面で選ばれた参加人数
        /// 設定画面で北抜き設定を表示するかどうかの判定に使う
        /// </summary>
        public ReactiveProperty<int> SelectedPlayerCount { get; } = new(4);
        /// <summary>
        /// 設定画面で選択中のCPU強度
        /// </summary>
        public ReactiveProperty<CpuDifficultyChoice> SettingsDifficulty { get; } = new(CpuDifficultyChoice.Normal);
        /// <summary>
        /// 設定画面で選択中の赤ドラ使用有無
        /// </summary>
        public ReactiveProperty<bool> SettingsUseRedDora { get; } = new(true);
        /// <summary>
        /// 設定画面で選択中の北抜き使用有無（三人麻雀のみ意味を持つ）
        /// </summary>
        public ReactiveProperty<bool> SettingsUseKitaNuki { get; } = new(false);


        // ========================================
        // フィールド
        // ========================================
        private UniTaskCompletionSource _titleStartSource;
        private UniTaskCompletionSource<(int PlayerCount, GameLengthChoice GameLength)> _modeSelectionSource;
        private UniTaskCompletionSource _settingsConfirmSource;
        private UniTaskCompletionSource _gameOverAckSource;


        // ========================================
        // Unityライフサイクル
        // ========================================
        private void Start()
        {
            RunFlowAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }


        // ========================================
        // プライベートメソッド（進行ループ）
        // ========================================
        /// <summary>
        /// タイトルからゲーム終了までの1周を、アプリが終了するまで繰り返す
        /// </summary>
        private async UniTaskVoid RunFlowAsync(CancellationToken ct)
        {
            while (true)
            {
                CurrentScreen.Value = AppScreen.Title;
                _titleStartSource = new UniTaskCompletionSource();
                await _titleStartSource.Task;

                CurrentScreen.Value = AppScreen.ModeSelect;
                _modeSelectionSource = new UniTaskCompletionSource<(int, GameLengthChoice)>();
                var (playerCount, gameLength) = await _modeSelectionSource.Task;
                SelectedPlayerCount.Value = playerCount;

                CurrentScreen.Value = AppScreen.Settings;
                _settingsConfirmSource = new UniTaskCompletionSource();
                await _settingsConfirmSource.Task;

                var settings = GameSettings.CreateDefault(
                    playerCount, ToModelGameLength(gameLength), SettingsUseRedDora.Value, SettingsUseKitaNuki.Value);

                CurrentScreen.Value = AppScreen.InGame;

                var gameContainer = new GameObject("Game");
                gameContainer.transform.SetParent(transform, false);
                var gamePresenter = gameContainer.AddComponent<GamePresenter>();
                ActiveGame.Value = gamePresenter;

                FinalSummary.Value = await gamePresenter.RunAsync(
                    settings, ToModelDifficulty(SettingsDifficulty.Value), HUMAN_PLAYER_INDEX, ct);

                ActiveGame.Value = null;
                Destroy(gameContainer);

                CurrentScreen.Value = AppScreen.GameOver;
                _gameOverAckSource = new UniTaskCompletionSource();
                await _gameOverAckSource.Task;
            }
        }
        /// <summary>
        /// View層向けの対局種別をModel層の GameLengthType に変換する
        /// </summary>
        private static GameLengthType ToModelGameLength(GameLengthChoice choice)
        {
            return choice switch
            {
                GameLengthChoice.EastOnly => GameLengthType.EastOnly,
                GameLengthChoice.HalfGame => GameLengthType.HalfGame,
                _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, $"未対応の GameLengthChoice です: {choice}"),
            };
        }
        /// <summary>
        /// View層向けのCPU強度をModel層の CpuDifficulty に変換する
        /// </summary>
        private static CpuDifficulty ToModelDifficulty(CpuDifficultyChoice choice)
        {
            return choice switch
            {
                CpuDifficultyChoice.Easy => CpuDifficulty.Easy,
                CpuDifficultyChoice.Normal => CpuDifficulty.Normal,
                _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, $"未対応の CpuDifficultyChoice です: {choice}"),
            };
        }


        // ========================================
        // パブリックメソッド（Viewからの入力受付）
        // ========================================
        /// <summary>
        /// タイトル画面のスタートボタンが押されたことを受け取る
        /// </summary>
        public void SubmitTitleStart()
        {
            _titleStartSource?.TrySetResult();
        }
        /// <summary>
        /// モード選択画面で選ばれた人数・対局種別を受け取る
        /// </summary>
        public void SubmitModeSelection(int playerCount, GameLengthChoice gameLength)
        {
            _modeSelectionSource?.TrySetResult((playerCount, gameLength));
        }
        /// <summary>
        /// 設定画面のCPU強度選択を受け取る
        /// </summary>
        public void SetSettingsDifficulty(CpuDifficultyChoice difficulty)
        {
            SettingsDifficulty.Value = difficulty;
        }
        /// <summary>
        /// 設定画面の赤ドラ使用有無を受け取る
        /// </summary>
        public void SetSettingsUseRedDora(bool useRedDora)
        {
            SettingsUseRedDora.Value = useRedDora;
        }
        /// <summary>
        /// 設定画面の北抜き使用有無を受け取る
        /// </summary>
        public void SetSettingsUseKitaNuki(bool useKitaNuki)
        {
            SettingsUseKitaNuki.Value = useKitaNuki;
        }
        /// <summary>
        /// 設定画面の確定（対局開始）ボタンが押されたことを受け取る
        /// </summary>
        public void ConfirmSettings()
        {
            _settingsConfirmSource?.TrySetResult();
        }
        /// <summary>
        /// ゲーム終了画面の確認（タイトルへ戻る）ボタンが押されたことを受け取る
        /// </summary>
        public void AcknowledgeGameOver()
        {
            _gameOverAckSource?.TrySetResult();
        }
    }
}
