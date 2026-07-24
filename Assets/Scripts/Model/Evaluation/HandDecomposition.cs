using System.Collections.Generic;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 和了形の1つの解釈（分解パターン）
    /// 1つの手牌に対して複数の HandDecomposition が存在しうる
    /// </summary>
    public sealed class HandDecomposition
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 和了形の種別
        /// </summary>
        public WinningForm Form { get; }
        /// <summary>
        /// この解釈におけるグループ一覧
        /// 標準形: 面子4つ＋雀頭 / 七対子: 対子7つ / 国士無双: 意味のあるサブグループが無いため空
        /// </summary>
        public IReadOnlyList<HandGroup> Groups { get; }
        /// <summary>
        /// 和了牌を含むグループ
        /// 国士無双の場合は null
        /// </summary>
        public HandGroup WinningGroup { get; }
        /// <summary>
        /// 待ちの形（標準形のみ意味を持つ）
        /// </summary>
        public WaitType WaitType { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public HandDecomposition(WinningForm form, IReadOnlyList<HandGroup> groups, HandGroup winningGroup, WaitType waitType)
        {
            Form = form;
            Groups = groups;
            WinningGroup = winningGroup;
            WaitType = waitType;
        }
    }
}
