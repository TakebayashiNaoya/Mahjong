using System.Collections.Generic;
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
