using System;

namespace Mahjong.Model.Game
{
    /// <summary>
    /// 対局全体の設定
    /// </summary>
    public class GameSettings
    {
        // ========================================
        // プロパティ
        // ========================================
        /// <summary>
        /// 参加人数（三人麻雀は3、四人麻雀は4）
        /// </summary>
        public int PlayerCount { get; }
        /// <summary>
        /// 東風戦か半荘戦か
        /// </summary>
        public GameLengthType GameLength { get; }
        /// <summary>
        /// 初期持ち点
        /// </summary>
        public int InitialScore { get; }
        /// <summary>
        /// 返し点（順位計算の基準点。順位計算自体は今回のマイルストーンでは扱わない）
        /// </summary>
        public int ReturnScore { get; }
        /// <summary>
        /// 赤ドラを使用するかどうか
        /// </summary>
        public bool UseRedDora { get; }
        /// <summary>
        /// 北抜きを使用するかどうか
        /// 三人麻雀以外では常に false 扱いにする
        /// </summary>
        public bool UseKitaNuki { get; }
        /// <summary>
        /// 三人麻雀でチーを許可するかどうか
        /// </summary>
        public bool AllowChiInThreePlayer { get; }
        /// <summary>
        /// 飛び（持ち点0点以下）でゲームを終了させるかどうか
        /// </summary>
        public bool EnableTobi { get; }


        // ========================================
        // コンストラクタ
        // ========================================
        /// <summary>
        /// 対局設定を初期化する
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">playerCount が3・4以外の場合</exception>
        public GameSettings(
            int playerCount,
            GameLengthType gameLength,
            int initialScore,
            int returnScore,
            bool useRedDora = true,
            bool useKitaNuki = false,
            bool allowChiInThreePlayer = false,
            bool enableTobi = true)
        {
            if (playerCount != 3 && playerCount != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), $"playerCount は3または4である必要があります: {playerCount}");
            }

            PlayerCount = playerCount;
            GameLength = gameLength;
            InitialScore = initialScore;
            ReturnScore = returnScore;
            UseRedDora = useRedDora;
            UseKitaNuki = useKitaNuki && playerCount == 3;
            AllowChiInThreePlayer = allowChiInThreePlayer;
            EnableTobi = enableTobi;
        }


        // ========================================
        // パブリックメソッド
        // ========================================
        /// <summary>
        /// 仕様書8.5準拠の標準設定を生成する
        /// 四人麻雀：初期25000点・返し30000点／三人麻雀：初期35000点・返し40000点
        /// </summary>
        /// <param name="playerCount">参加人数（3または4）</param>
        /// <param name="gameLength">東風戦か半荘戦か</param>
        /// <param name="useRedDora">赤ドラを使用するかどうか</param>
        /// <param name="useKitaNuki">北抜きを使用するかどうか（三人麻雀以外では無視される）</param>
        public static GameSettings CreateDefault(
            int playerCount, GameLengthType gameLength, bool useRedDora = true, bool useKitaNuki = false)
        {
            var initialScore = playerCount == 3 ? 35000 : 25000;
            var returnScore = playerCount == 3 ? 40000 : 30000;
            return new GameSettings(playerCount, gameLength, initialScore, returnScore, useRedDora, useKitaNuki);
        }
    }
}
