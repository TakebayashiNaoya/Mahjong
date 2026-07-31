namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する役1つ分の表示用データ（役名＋翻数）
    /// </summary>
    public readonly struct YakuLineView
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 役の日本語表示名
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 翻数（役満の場合は倍率）
        /// </summary>
        public int Han { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        internal YakuLineView(string name, int han)
        {
            Name = name;
            Han = han;
        }
    }
}
