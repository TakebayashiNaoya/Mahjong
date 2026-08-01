namespace Mahjong.Presenter
{
    /// <summary>
    /// アプリ全体の画面状態
    /// </summary>
    public enum AppScreen
    {
        Title,      // タイトル画面
        ModeSelect, // モード選択画面
        Settings,   // 設定画面
        InGame,     // ゲームメイン画面（対局中）
        GameOver,   // ゲーム終了（簡易順位表示）
    }
}
