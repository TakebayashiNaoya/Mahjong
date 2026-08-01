namespace Mahjong.Model.Game
{
    /// <summary>
    /// カオス麻雀ルールで、自分のツモ番に選べる取得元1件（仕様書16.3）
    /// 他家の手牌から取る場合も牌そのものは保持しない
    /// 何を引いたかは実行するまで伏せたままにする必要があるため
    /// </summary>
    public class ChaosDrawOption
    {
        // ========================================
        // 定数
        // ========================================
        /// <summary>
        /// 対象を持たない場合のインデックス
        /// </summary>
        public const int NO_TARGET = -1;


        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 牌を取る場所
        /// </summary>
        public ChaosDrawSource Source { get; }
        /// <summary>
        /// 対象プレイヤーの席順（山からのツモは NO_TARGET）
        /// </summary>
        public int TargetPlayerIndex { get; }
        /// <summary>
        /// 対象の位置（河・手牌・副露それぞれの中でのインデックス。山からのツモは NO_TARGET）
        /// </summary>
        public int TargetIndex { get; }
        /// <summary>
        /// 副露の中で取る牌の位置（副露を対象にしない場合は NO_TARGET）
        /// 自分の副露を戻す場合は副露ごと戻すため NO_TARGET になる
        /// </summary>
        public int MeldTileIndex { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 取得元を生成する
        /// 種類ごとに必要な値が決まっているため、直接呼ばずに各ファクトリメソッドを使う
        /// </summary>
        private ChaosDrawOption(ChaosDrawSource source, int targetPlayerIndex, int targetIndex, int meldTileIndex)
        {
            Source = source;
            TargetPlayerIndex = targetPlayerIndex;
            TargetIndex = targetIndex;
            MeldTileIndex = meldTileIndex;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 山からツモる（通常ルールと同じ取得元）
        /// </summary>
        public static ChaosDrawOption FromWall()
        {
            return new ChaosDrawOption(ChaosDrawSource.Wall, NO_TARGET, NO_TARGET, NO_TARGET);
        }
        /// <summary>
        /// 誰かの河から1枚取る
        /// </summary>
        /// <param name="targetPlayerIndex">河の持ち主の席順（自分でもよい）</param>
        /// <param name="discardIndex">河の中での位置</param>
        public static ChaosDrawOption FromDiscardPile(int targetPlayerIndex, int discardIndex)
        {
            return new ChaosDrawOption(ChaosDrawSource.DiscardPile, targetPlayerIndex, discardIndex, NO_TARGET);
        }
        /// <summary>
        /// 他家の手牌から1枚取る
        /// </summary>
        /// <param name="targetPlayerIndex">奪う相手の席順</param>
        /// <param name="tileIndex">相手の手牌の中での位置（中身は伏せたまま）</param>
        public static ChaosDrawOption FromOpponentHand(int targetPlayerIndex, int tileIndex)
        {
            return new ChaosDrawOption(ChaosDrawSource.OpponentHand, targetPlayerIndex, tileIndex, NO_TARGET);
        }
        /// <summary>
        /// 他家の副露から1枚取る（残りの牌は相手の手牌に戻り、その副露は消滅する）
        /// </summary>
        /// <param name="targetPlayerIndex">奪う相手の席順</param>
        /// <param name="meldIndex">相手の副露の中での位置</param>
        /// <param name="meldTileIndex">その副露の中で取る牌の位置</param>
        public static ChaosDrawOption FromOpponentMeld(int targetPlayerIndex, int meldIndex, int meldTileIndex)
        {
            return new ChaosDrawOption(ChaosDrawSource.OpponentMeld, targetPlayerIndex, meldIndex, meldTileIndex);
        }
        /// <summary>
        /// 自分の副露を1組そのまま手牌に戻す
        /// </summary>
        /// <param name="meldIndex">自分の副露の中での位置</param>
        public static ChaosDrawOption FromOwnMeld(int meldIndex)
        {
            return new ChaosDrawOption(ChaosDrawSource.OwnMeld, NO_TARGET, meldIndex, NO_TARGET);
        }
        /// <summary>
        /// 取得元の文字列表現を返す
        /// </summary>
        public override string ToString()
        {
            return $"[{Source}: P{TargetPlayerIndex} #{TargetIndex}/{MeldTileIndex}]";
        }
    }
}
