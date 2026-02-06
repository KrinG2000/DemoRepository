// ============================================================
// InfiniteFirepowerPhase.cs — 无限火力相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 无限火力相位规则:
//       - 充能速度大幅提升 (2倍)
//       - 发起者技能过热: 发起对决后技能格进入过热冷却
//       - 受害者幽灵保护: 防守方输了对决后受到的惩罚减半
//       - 鼓励频繁对决但增加发起者的风险
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class InfiniteFirepowerPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.InfiniteFirepower;
        public override string DisplayName => "无限火力";
        public override string Description => "火力全开！充能速度翻倍，但发起者技能过热，防守方拥有幽灵保护。";

        /// <summary>
        /// 充能速度倍率 (2倍)
        /// </summary>
        public const float ChargeMultiplier = 2.0f;

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
        /// 对决后处理: 发起者技能过热
        /// </summary>
        public override void PostDuelEffect(DuelResultData result)
        {
            // 标记发起者技能过热 (由GameManager读取并应用到SkillSlotManager)
            result.PhaseEffectDescription = "无限火力: 发起者技能过热！需要冷却后才能再次充能。防守方获得幽灵保护。";
            GameEvents.RaisePhaseRuleTriggered(result.PhaseEffectDescription);
        }
    }
}
