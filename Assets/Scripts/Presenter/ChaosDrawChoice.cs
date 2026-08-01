using Mahjong.Model.Game;

namespace Mahjong.Presenter
{
    /// <summary>
    /// カオス麻雀の取得元選択で、人間プレイヤーに提示する選択肢1件
    /// 取得元は河と他家の手牌を合わせると100件を超えるため、
    /// 「どこから取るか」→「その中のどれを取るか」の2段階に分けて提示する
    /// View層はこのラッパー越しにしか扱わないため、Model型（ChaosDrawOption）を直接参照せずに済む
    /// </summary>
    public sealed class ChaosDrawChoice
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// ボタンに表示するラベル
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// 選ぶと確定する取得元（Presenter層内でのみ使用する）
        /// 絞り込みを開くだけの選択肢では null
        /// </summary>
        internal ChaosDrawOption Option { get; }
        /// <summary>
        /// 開く絞り込みの取得元種別（Option が null の場合のみ意味を持つ）
        /// </summary>
        internal ChaosDrawSource GroupSource { get; }
        /// <summary>
        /// 開く絞り込みの対象プレイヤー（Option が null の場合のみ意味を持つ）
        /// </summary>
        internal int GroupPlayerIndex { get; }
        /// <summary>
        /// 1段階目の選択に戻る選択肢かどうか
        /// </summary>
        internal bool IsBack { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        private ChaosDrawChoice(
            string label, ChaosDrawOption option, ChaosDrawSource groupSource, int groupPlayerIndex, bool isBack)
        {
            Label = label;
            Option = option;
            GroupSource = groupSource;
            GroupPlayerIndex = groupPlayerIndex;
            IsBack = isBack;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 選ぶと取得元が確定する選択肢を生成する
        /// </summary>
        internal static ChaosDrawChoice Confirm(string label, ChaosDrawOption option)
        {
            return new ChaosDrawChoice(label, option, ChaosDrawSource.Wall, ChaosDrawOption.NO_TARGET, isBack: false);
        }
        /// <summary>
        /// 選ぶと絞り込みの中身を開く選択肢を生成する
        /// </summary>
        internal static ChaosDrawChoice OpenGroup(string label, ChaosDrawSource source, int targetPlayerIndex)
        {
            return new ChaosDrawChoice(label, option: null, source, targetPlayerIndex, isBack: false);
        }
        /// <summary>
        /// 1段階目の選択に戻る選択肢を生成する
        /// </summary>
        internal static ChaosDrawChoice Back(string label)
        {
            return new ChaosDrawChoice(
                label, option: null, ChaosDrawSource.Wall, ChaosDrawOption.NO_TARGET, isBack: true);
        }
    }
}
