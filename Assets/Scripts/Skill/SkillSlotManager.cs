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
        /// 使用时间计算: 只有过热到期后才会恢复
        /// </summary>
        public bool IsOverheated
        {
            get
            {
                if (_overheatEndTime < 0f)
                    return false;
                if (_timeProvider != null && _timeProvider.CurrentTime >= _overheatEndTime)
                {
                    ClearOverheat();
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 过热剩余时间 (秒)
        /// </summary>
        public float OverheatRemainingTime
        {
            get
            {
                if (_overheatEndTime < 0f || _timeProvider == null)
                    return 0f;
                float remaining = _overheatEndTime - _timeProvider.CurrentTime;
                return remaining > 0f ? remaining : 0f;
            }
        }

        private float _overheatEndTime = -1f;
        private ITimeProvider _timeProvider;

        public SkillSlotManager(int playerId) : this(playerId, null) { }

        public SkillSlotManager(int playerId, ITimeProvider timeProvider)
        {
            PlayerId = playerId;
            FilledSlots = 0;
            CurrentCharge = 0f;
            _overheatEndTime = -1f;
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// 设置时间提供者 (用于过热计时)
        /// </summary>
        public void SetTimeProvider(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// 启动过热状态 (持续指定秒数)
        /// </summary>
        /// <param name="duration">过热持续秒数</param>
        public void StartOverheat(float duration)
        {
            if (_timeProvider != null)
            {
                _overheatEndTime = _timeProvider.CurrentTime + duration;
                GameEvents.RaiseOverheatStarted(PlayerId, duration);
            }
            else
            {
                // 无时间提供者时,设置为永久过热 (向后兼容)
                _overheatEndTime = float.MaxValue;
            }
        }

        /// <summary>
        /// 强制清除过热状态
        /// </summary>
        public void ClearOverheat()
        {
            if (_overheatEndTime >= 0f)
            {
                _overheatEndTime = -1f;
                GameEvents.RaiseOverheatEnded(PlayerId);
            }
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
            _overheatEndTime = -1f;
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
