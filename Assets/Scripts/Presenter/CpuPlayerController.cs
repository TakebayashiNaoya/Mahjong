using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Cpu;
using Mahjong.Model.Game;
using Mahjong.Model.Tiles;

namespace Mahjong.Presenter
{
    /// <summary>
    /// CpuStrategy（Model/CPU層）への同期的な委譲だけを行う IPlayerController 実装
    /// </summary>
    public sealed class CpuPlayerController : IPlayerController
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// このマイルストーンで使用するCPU強度
        /// </summary>
        private const CpuDifficulty DIFFICULTY = CpuDifficulty.Normal;


        // ========================================
        // フィールド
        // ========================================
        /// <summary>
        /// 打牌選択のタイブレークに使う乱数生成器
        /// </summary>
        private readonly System.Random _random = new();


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// CPUはカオス操作を行わず、常に山からツモる（仕様書16.9）
        /// 山が尽きている手番では GamePresenter が先に荒牌平局を宣言するため、山の選択肢は必ず含まれる
        /// </summary>
        public UniTask<ChaosDrawOption> ChooseChaosDrawAsync(
            Round round, int playerIndex, IReadOnlyList<ChaosDrawOption> options, CancellationToken ct)
        {
            var wallOption = options.FirstOrDefault(o => o.Source == ChaosDrawSource.Wall);
            return UniTask.FromResult(wallOption ?? options[0]);
        }

        public UniTask<bool> ShouldDeclareRiichiAsync(Round round, int playerIndex, CancellationToken ct)
        {
            return UniTask.FromResult(CpuStrategy.ShouldDeclareRiichi(round, playerIndex, DIFFICULTY));
        }

        public UniTask<Tile> ChooseDiscardAsync(Round round, int playerIndex, CancellationToken ct)
        {
            return UniTask.FromResult(CpuStrategy.ChooseDiscard(round, playerIndex, DIFFICULTY, _random));
        }

        public UniTask<DeclaredCall> ChooseCallAsync(Round round, int playerIndex, IReadOnlyList<CallOption> myOptions, CancellationToken ct)
        {
            return UniTask.FromResult(CpuStrategy.ChooseCall(round, playerIndex, myOptions, DIFFICULTY));
        }
    }
}
