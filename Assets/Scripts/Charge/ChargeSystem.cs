// ============================================================
// ChargeSystem.cs — 漂移充能系统实现
// 架构层级: Charge
// 说明: 包装 SkillSlotManager, 提供 Tick 驱动的充能接口。
//       漂移充能速率来自 BalanceConfig.DriftChargeBaseRate。
//       Overload相位: AddCharge 自动乘以 chargeRateMultiplier(2.5x)。
// ============================================================

using RacingCardGame.Config;
using RacingCardGame.Skill;

namespace RacingCardGame.Charge
{
    public class ChargeSystem : IChargeSystem
    {
        public int PlayerId => _skillSlots.PlayerId;
        public int SkillSlots => _skillSlots.FilledSlots;
        public int MaxSkillSlots => SkillSlotManager.MaxSlots;
        public float CurrentCharge => _skillSlots.CurrentCharge;
        public bool CanCastDuelSkill => _skillSlots.IsFullyCharged && !_skillSlots.IsOverheated;
        public bool IsOverheated => _skillSlots.IsOverheated;
        public float OverheatRemainingTime => _skillSlots.OverheatRemainingTime;

        private readonly SkillSlotManager _skillSlots;

        /// <summary>
        /// 直接访问底层 SkillSlotManager (供 DuelSystem 使用)
        /// </summary>
        public SkillSlotManager InternalSlotManager => _skillSlots;

        public ChargeSystem(SkillSlotManager skillSlots)
        {
            _skillSlots = skillSlots;
        }

        public void AddCharge(float amount)
        {
            _skillSlots.AddCharge(amount);
        }

        public void ConsumeAllSlots()
        {
            _skillSlots.TryActivateSkill();
        }

        /// <summary>
        /// 每帧更新: 如果正在漂移, 按基础速率充能
        /// </summary>
        public void Tick(float deltaTime, bool isDrifting)
        {
            if (isDrifting && !_skillSlots.IsFullyCharged)
            {
                float chargeAmount = BalanceConfig.Current.DriftChargeBaseRate * deltaTime;
                _skillSlots.AddCharge(chargeAmount);
            }
        }
    }
}
