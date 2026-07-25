using Mahjong.Model.Game;

namespace Mahjong.Presenter
{
    /// <summary>
    /// 人間プレイヤーに提示する宣言候補（ポン・チー・カン・スルー）
    /// View層はこのラッパー越しにしか宣言内容を扱わないため、Model型（DeclaredCall・CallOption）を直接参照せずに済む
    /// </summary>
    public sealed class CallChoice
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// ボタンに表示するラベル
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// 実際に宣言する内容（Presenter層内でのみ使用する）
        /// スルーを表す場合は null
        /// </summary>
        internal DeclaredCall Call { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        internal CallChoice(string label, DeclaredCall call)
        {
            Label = label;
            Call = call;
        }
    }
}
