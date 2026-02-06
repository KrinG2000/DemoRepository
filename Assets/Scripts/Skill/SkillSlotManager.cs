// ============================================================
// SkillSlotManager.cs — 技能格管理器
// 架构层级: Skill (功能模块层)
// 依赖: Core/GameEnums, Core/GameEvents
// 说明: 管理玩家的技能格充能与消耗。玩家通过漂移积攒
//       技能槽,满3格时可释放技能(触发子空间对决)。
//       每个玩家实例拥有独立的SkillSlotManager。
// ============================================================

using System;
using RacingCardGame.Core;

namespace RacingCardGame.Skill
{
    public class SkillSlotManager
    {
        /// <summary>
        /// 最大技能格数量
        /// </summary>
        public const int MaxSlots = 3;

        /// <summary>
        /// 每个技能格所需充能量
        /// </summary>
        public const float ChargePerSlot = 100f;

        /// <summary>
        /// 所属玩家ID
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// 当前已充满的技能格数量 (0 ~ MaxSlots)
        /// </summary>
        public int FilledSlots { get; private set; }

        /// <summary>
        /// 当前技能格的充能进度 (0 ~ ChargePerSlot)
        /// </summary>
        public float CurrentCharge { get; private set; }

        /// <summary>
        /// 是否所有技能格已满,可以释放技能
        /// </summary>
        public bool IsFullyCharged => FilledSlots >= MaxSlots;

        /// <summary>
        /// 充能速度倍率 (相位可修改此值, 如无限火力相位)
        /// </summary>
        public float ChargeSpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 是否处于过热状态 (无限火力相位: 发起者技能过热)
        /// </summary>
        public bool IsOverheated { get; set; }

        public SkillSlotManager(int playerId)
        {
            PlayerId = playerId;
            FilledSlots = 0;
            CurrentCharge = 0f;
            IsOverheated = false;
        }

        /// <summary>
        /// 漂移充能 — 由赛车系统在玩家漂移时调用
        /// </summary>
        /// <param name="rawAmount">原始充能量 (漂移强度决定)</param>
        /// <returns>充能后是否达到满格状态</returns>
        public bool AddCharge(float rawAmount)
        {
            if (IsFullyCharged || rawAmount <= 0f)
                return IsFullyCharged;

            float effectiveAmount = rawAmount * ChargeSpeedMultiplier;
            CurrentCharge += effectiveAmount;

            // 检查是否填满了一个技能格
            while (CurrentCharge >= ChargePerSlot && FilledSlots < MaxSlots)
            {
                CurrentCharge -= ChargePerSlot;
                FilledSlots++;
                GameEvents.RaiseSkillSlotChanged(FilledSlots);
            }

            // 防止溢出: 如果已满,归零剩余充能
            if (FilledSlots >= MaxSlots)
            {
                CurrentCharge = 0f;
            }

            return IsFullyCharged;
        }

        /// <summary>
        /// 尝试释放技能 (消耗所有技能格)
        /// </summary>
        /// <returns>是否成功释放</returns>
        public bool TryActivateSkill()
        {
            if (!IsFullyCharged)
                return false;

            if (IsOverheated)
                return false;

            // 消耗所有技能格
            FilledSlots = 0;
            CurrentCharge = 0f;

            GameEvents.RaiseSkillSlotChanged(0);
            GameEvents.RaiseSkillActivated();

            return true;
        }

        /// <summary>
        /// 消耗指定数量的技能格 (某些相位规则可能只消耗部分)
        /// </summary>
        /// <param name="count">要消耗的格数</param>
        /// <returns>实际消耗的格数</returns>
        public int ConsumeSlots(int count)
        {
            int consumed = Math.Min(count, FilledSlots);
            FilledSlots -= consumed;
            CurrentCharge = 0f;

            GameEvents.RaiseSkillSlotChanged(FilledSlots);
            return consumed;
        }

        /// <summary>
        /// 重置技能格状态 (游戏重新开始时调用)
        /// </summary>
        public void Reset()
        {
            FilledSlots = 0;
            CurrentCharge = 0f;
            ChargeSpeedMultiplier = 1.0f;
            IsOverheated = false;
        }

        /// <summary>
        /// 获取总充能进度百分比 (0.0 ~ 1.0)
        /// </summary>
        public float GetTotalProgress()
        {
            float totalCapacity = MaxSlots * ChargePerSlot;
            float totalFilled = FilledSlots * ChargePerSlot + CurrentCharge;
            return totalFilled / totalCapacity;
        }
    }
}
