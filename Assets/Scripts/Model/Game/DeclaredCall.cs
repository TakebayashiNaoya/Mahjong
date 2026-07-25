using System;
using System.Collections.Generic;
using Mahjong.Model.Tiles;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// プレイヤーが実際に選択した宣言
    /// </summary>
    public class DeclaredCall
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 宣言したプレイヤーの席順
        /// </summary>
        public int PlayerIndex { get; }
        /// <summary>
        /// 宣言の種類
        /// </summary>
        public CallType Type { get; }
        /// <summary>
        /// 副露に使う手牌側の牌（Ron の場合は空リスト）
        /// </summary>
        public IReadOnlyList<Tile> SelectedTiles { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 宣言を生成する
        /// </summary>
        /// <exception cref="ArgumentNullException">selectedTiles が null の場合</exception>
        public DeclaredCall(int playerIndex, CallType type, IReadOnlyList<Tile> selectedTiles)
        {
            if (selectedTiles == null)
            {
                throw new ArgumentNullException(nameof(selectedTiles), "selectedTiles が null です");
            }

            PlayerIndex = playerIndex;
            Type = type;
            SelectedTiles = selectedTiles;
        }
    }
}
