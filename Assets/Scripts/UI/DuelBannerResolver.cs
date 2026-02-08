// ============================================================
// DuelBannerResolver.cs — Banner选择纯函数
// 架构层级: UI
// 说明: 根据DuelResultContext严格按P1-P7优先级选择唯一主Banner,
//       纯函数设计,方便测试。单局单相位互斥,不存在跨相位叠加。
// ============================================================

namespace RacingCardGame.UI
{
    public static class DuelBannerResolver
    {
        /// <summary>
        /// 根据对决结算上下文选择唯一主Banner (P1-P7优先级)
        /// </summary>
        public static UIBannerType PickBanner(DuelResultContext ctx)
        {
            // P1: ConquerHeaven (胜天半子, 仅Casino)
            if (ctx.Phase == DuelPhase.Casino && ctx.CasinoConquerHeavenTriggered)
                return UIBannerType.ConquerHeaven;

            // P2: JesterSwap (小丑惊魂, 仅Jester)
            if (ctx.Phase == DuelPhase.Jester && ctx.JesterSwapTriggered)
                return UIBannerType.JesterSwap;

            // P3: DestinyCrit (天命暴击, 仅Casino, 赢家=攻击者且压中天命)
            if (ctx.Phase == DuelPhase.Casino && ctx.WinnerId.HasValue)
            {
                bool winnerIsAttacker = (ctx.WinnerId.Value == ctx.AttackerId);
                bool winnerHitHouse = winnerIsAttacker
                    ? ctx.CasinoAttackerHitHouse
                    : ctx.CasinoDefenderHitHouse;

                if (winnerHitHouse && winnerIsAttacker)
                    return UIBannerType.DestinyCrit;

                // P4: DestinyCounter (天命反杀, 仅Casino, 赢家=防守方且压中天命)
                if (winnerHitHouse && !winnerIsAttacker)
                    return UIBannerType.DestinyCounter;
            }

            // P5: BadLuck (无效运气, 仅Casino, 败者压中天命)
            if (ctx.Phase == DuelPhase.Casino && ctx.LoserId.HasValue && ctx.CasinoLoserHitHouse)
                return UIBannerType.BadLuck;

            // P6: NormalWin (任意相位, 有赢家)
            if (ctx.WinnerId.HasValue)
                return UIBannerType.NormalWin;

            // P7: Draw (任意相位, 平局)
            return UIBannerType.Draw;
        }
    }
}
