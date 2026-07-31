namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開する対局種別（Mahjong.Model.Game.GameLengthType の鏡写し）
    /// View層がModel型を直接参照せずに済むよう、Presenter層で独自に定義する
    /// </summary>
    public enum GameLengthChoice
    {
        EastOnly, // 東風戦（東場のみ）
        HalfGame, // 半荘戦（東場＋南場）
    }
}
