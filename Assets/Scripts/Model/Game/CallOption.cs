using System;
using System.Collections.Generic;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 捨て牌に対して宣言可能な選択肢
    /// </summary>
    public class CallOption
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 宣言できるプレイヤーの席順
        /// </summary>
        public int PlayerIndex { get; }
        /// <summary>
        /// 宣言の種類
        /// </summary>
        public CallType Type { get; }
        /// <summary>
        /// 副露に使う手牌側の候補
        /// チーは複数パターンあり得るため候補のリストとして持つ。ポン・カン・ロンは候補が1通りのみ
        /// </summary>
        public IReadOnlyList<IReadOnlyList<Tile>> Candidates { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 宣言可能な選択肢を生成する
        /// </summary>
        /// <exception cref="ArgumentNullException">candidates が null の場合</exception>
        public CallOption(int playerIndex, CallType type, IReadOnlyList<IReadOnlyList<Tile>> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates), "candidates が null です");
            }

            PlayerIndex = playerIndex;
            Type = type;
            Candidates = candidates;
        }
    }
}
