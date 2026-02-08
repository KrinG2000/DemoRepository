// ============================================================
// InfiniteFirepowerPhase.cs — 无限火力相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents, Config/BalanceConfig
// 说明: 所有参数从BalanceConfig读取,支持热更
// ============================================================

using RacingCardGame.Core;
using RacingCardGame.Config;

namespace RacingCardGame.Phase
{
    public class InfiniteFirepowerPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.InfiniteFirepower;
        public override string DisplayName => "无限火力";
        public override string Description => "火力全开！充能速度提升，但发起者会武器过热。被频繁拉入子空间的玩家将获得幽灵保护。";

        public static float ChargeMultiplier => BalanceConfig.Current.OverloadChargeMultiplier;
        public static float OverheatDuration => BalanceConfig.Current.AttackerOverheatTime;
        public static int GhostProtectionPullThreshold => BalanceConfig.Current.GhostTriggerCount;
        public static float GhostProtectionTimeWindow => BalanceConfig.Current.GhostCheckWindow;
        public static float GhostProtectionDuration => BalanceConfig.Current.GhostDuration;
        public static float GhostProtectionMultiplier => BalanceConfig.Current.GhostPenaltyReduction;

        public override float GetChargeSpeedMultiplier()
        {
            return ChargeMultiplier;
        }

        public override (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            var (reward, penalty) = base.CalculateMultipliers(destinyMatch);
            penalty *= GhostProtectionMultiplier;
            return (reward, penalty);
        }

        public override void PostDuelEffect(DuelResultData result)
        {
            result.OverheatDuration = OverheatDuration;
            result.PhaseEffectDescription =
                $"无限火力: 发起者武器过热{OverheatDuration}秒！防守方获得惩罚减免。";
            GameEvents.RaisePhaseRuleTriggered(result.PhaseEffectDescription);
        }
    }
}
