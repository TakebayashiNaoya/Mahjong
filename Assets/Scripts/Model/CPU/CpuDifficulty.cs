namespace Mahjong.Model.Cpu
{
    /// <summary>
    /// CPUの強度レベル（仕様書10.1）
    /// 「強」はMVP後拡張のため、今回は Easy・Normal の2段階のみ用意する
    /// </summary>
    public enum CpuDifficulty
    {
        Easy,   // 弱：ランダムに捨て牌を選択、鳴きをほとんどしない
        Normal, // 普通：牌効率で打牌、テンパイ時は基本リーチ、和了に近づく場合のみ鳴く
    }
}
