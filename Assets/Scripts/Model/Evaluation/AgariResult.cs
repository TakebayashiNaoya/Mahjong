using System.Collections.Generic;

namespace Mahjong.Model.Evaluation
{
    /// <summary>
    /// 和了判定の結果
    /// </summary>
    public sealed class AgariResult
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 和了形として成立しているかどうか
        /// </summary>
        public bool IsWin { get; }
        /// <summary>
        /// 成立している分解パターンの一覧
        /// 標準形は複数の解釈がありうるため、役判定側で最良のものを選ぶ
        /// </summary>
        public IReadOnlyList<HandDecomposition> Decompositions { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        public AgariResult(bool isWin, IReadOnlyList<HandDecomposition> decompositions)
        {
            IsWin = isWin;
            Decompositions = decompositions;
        }
    }
}
