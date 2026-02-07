// ============================================================
// UIBannerType.cs — 主Banner类型枚举
// 架构层级: UI
// 说明: 定义7种主Banner类型,每次对决结算只允许出现1个主Banner
// ============================================================

namespace RacingCardGame.UI
{
    /// <summary>
    /// 主Banner类型 — 按优先级从高到低排列
    /// </summary>
    public enum UIBannerType
    {
        ConquerHeaven,      // P1: 胜天半子 (仅Casino)
        JesterSwap,         // P2: 小丑惊魂 (仅Jester)
        DestinyCrit,        // P3: 天命暴击 (仅Casino, 赢家=攻击者且压中天命)
        DestinyCounter,     // P4: 天命反杀 (仅Casino, 赢家=防守方且压中天命)
        BadLuck,            // P5: 无效运气 (仅Casino, 败者压中天命)
        NormalWin,          // P6: 普通胜利 (任意相位, 有赢家)
        Draw                // P7: 平局 (任意相位)
    }
}
