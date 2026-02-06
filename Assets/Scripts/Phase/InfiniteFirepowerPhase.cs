// ============================================================
// InfiniteFirepowerPhase.cs — 无限火力相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 无限火力相位规则:
//       - 充能速度大幅提升 (2.5倍)
//       - 发起者技能过热: 发起对决后5秒内无法再次发起
//       - 受害者幽灵保护: 45秒内被拉入子空间3次 -> 20秒完全免疫
//       - 鼓励频繁对决但增加发起者的风险和受害者保护
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class InfiniteFirepowerPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.InfiniteFirepower;
        public override string DisplayName => "无限火力";
        public override string Description => "火力全开！充能速度2.5倍，但发起者会武器过热5秒。被频繁拉入子空间的玩家将获得幽灵保护。";

        /// <summary>
        /// 充能速度倍率 (2.5倍)
        /// </summary>
        public const float ChargeMultiplier = 2.5f;

        /// <summary>
        /// 过热持续时间 (秒)
        /// </summary>
        public const float OverheatDuration = 5.0f;

        /// <summary>
        /// 幽灵保护触发阈值: 在时间窗口内被拉入次数
        /// </summary>
        public const int GhostProtectionPullThreshold = 3;

        /// <summary>
        /// 幽灵保护计数时间窗口 (秒)
        /// </summary>
        public const float GhostProtectionTimeWindow = 45.0f;

        /// <summary>
        /// 幽灵保护持续时间 (秒)
        /// </summary>
        public const float GhostProtectionDuration = 20.0f;

        /// <summary>
        /// 幽灵保护: 防守方惩罚减免倍率 (惩罚减半)
        /// </summary>
        public const float GhostProtectionMultiplier = 0.5f;

        /// <summary>
        /// 无限火力的充能加速
        /// </summary>
        public override float GetChargeSpeedMultiplier()
        {
            return ChargeMultiplier;
        }

        /// <summary>
        /// 无限火力的奖惩修正:
        /// 防守方(受害者)拥有幽灵保护,惩罚减半
        /// </summary>
        public override (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            // 先获取基础倍率
            var (reward, penalty) = base.CalculateMultipliers(destinyMatch);

            // 幽灵保护: 防守方受到的惩罚减半
            penalty *= GhostProtectionMultiplier;

            return (reward, penalty);
        }

        /// <summary>
        /// 对决后处理: 标记发起者过热和幽灵保护信息
        /// GameManager会读取result中的数据并应用具体的计时效果
        /// </summary>
        public override void PostDuelEffect(DuelResultData result)
        {
            // 设置过热持续时间 (由GameManager读取并应用到SkillSlotManager)
            result.OverheatDuration = OverheatDuration;
            result.PhaseEffectDescription =
                $"无限火力: 发起者武器过热{OverheatDuration}秒！防守方获得惩罚减免。";
            GameEvents.RaisePhaseRuleTriggered(result.PhaseEffectDescription);
        }
    }
}
