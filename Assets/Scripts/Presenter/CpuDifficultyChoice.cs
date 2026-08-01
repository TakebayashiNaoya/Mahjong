namespace Mahjong.Presenter
{
    /// <summary>
    /// View層に公開するCPU強度（Mahjong.Model.Cpu.CpuDifficulty の鏡写し）
    /// View層がModel型を直接参照せずに済むよう、Presenter層で独自に定義する
    /// </summary>
    public enum CpuDifficultyChoice
    {
        Easy,   // 弱：ランダムに捨て牌を選択、鳴きをほとんどしない
        Normal, // 普通：牌効率で打牌、テンパイ時は基本リーチ、和了に近づく場合のみ鳴く
    }
}
