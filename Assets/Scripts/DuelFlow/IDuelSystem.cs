// ============================================================
// IDuelSystem.cs — 对决流程系统接口
// 架构层级: DuelFlow (积木式接口)
// 说明: 完整对决流程: Validate→ConsumeTicket→Enter→Pick→Lock→
//       PhaseEvent→Judge→Apply→Exit。
//       DuelSystem 不直接操控车辆输入, 只发出 ApplyEffect。
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.DuelFlow
{
    /// <summary>
    /// 对决验证失败原因
    /// </summary>
    public enum DuelFailReason
    {
        None,
        NeedTicket,
        TargetImmune,
        WeaponOverheat,
        DuelBusy,
        SkillNotReady,
        TargetGhostProtected,
        InvalidTarget
    }

    /// <summary>
    /// 对决流程系统接口
    /// </summary>
    public interface IDuelSystem
    {
        /// <summary>
        /// 是否有正在进行的对决 (全局锁)
        /// </summary>
        bool IsDuelActive { get; }

        /// <summary>
        /// 尝试发起对决 (被E触发后调用)
        /// </summary>
        /// <returns>验证失败原因 (None=成功发起)</returns>
        DuelFailReason TryStartDuel(int attackerId, int defenderId);

        /// <summary>
        /// 对决Tick — 驱动子空间内的选牌倒计时和流程推进
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// 当前对决的攻击者ID (-1=无)
        /// </summary>
        int CurrentAttackerId { get; }

        /// <summary>
        /// 当前对决的防守方ID (-1=无)
        /// </summary>
        int CurrentDefenderId { get; }
    }
}
