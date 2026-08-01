using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mahjong.Model.Game;
using Mahjong.Model.Tiles;

namespace Mahjong.Presenter
{
    /// <summary>
    /// 1プレイヤー分の意思決定を担う窓口
    /// CPU（CpuPlayerController）・人間（HumanPlayerController）のどちらも同じ形で GamePresenter から呼べるようにする
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        /// カオス麻雀ルールで、この手番にどこから牌を取るかを選ぶ（仕様書16.3）
        /// カオスルールが無効の局では呼ばれない
        /// </summary>
        /// <param name="options">Round が列挙した取得元。必ず1件以上ある</param>
        UniTask<ChaosDrawOption> ChooseChaosDrawAsync(
            Round round, int playerIndex, IReadOnlyList<ChaosDrawOption> options, CancellationToken ct);
        /// <summary>
        /// リーチを宣言するかどうかを判断する
        /// </summary>
        UniTask<bool> ShouldDeclareRiichiAsync(Round round, int playerIndex, CancellationToken ct);
        /// <summary>
        /// 打牌する牌を選ぶ
        /// </summary>
        UniTask<Tile> ChooseDiscardAsync(Round round, int playerIndex, CancellationToken ct);
        /// <summary>
        /// 捨て牌に対する宣言（ポン・チー・カン・スルー）を選ぶ
        /// </summary>
        /// <param name="myOptions">このプレイヤー自身の選択肢のみ</param>
        /// <returns>宣言する内容。見送る場合は null</returns>
        UniTask<DeclaredCall> ChooseCallAsync(Round round, int playerIndex, IReadOnlyList<CallOption> myOptions, CancellationToken ct);
    }
}
