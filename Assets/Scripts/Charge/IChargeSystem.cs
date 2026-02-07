// ============================================================
// IChargeSystem.cs — 漂移充能系统接口
// 架构层级: Charge (积木式接口)
// 说明: 封装漂移充能逻辑。只负责数值管理,
//       漂移状态检测由 CarController/Input 层判断后通知。
// ============================================================

namespace RacingCardGame.Charge
{
    public interface IChargeSystem
    {
        int PlayerId { get; }
        int SkillSlots { get; }
        int MaxSkillSlots { get; }
        float CurrentCharge { get; }
        bool CanCastDuelSkill { get; }
        bool IsOverheated { get; }
        float OverheatRemainingTime { get; }

        /// <summary>
        /// 添加充能 (由漂移事件触发)
        /// </summary>
        void AddCharge(float amount);

        /// <summary>
        /// 消耗所有技能格 (发起对决后)
        /// </summary>
        void ConsumeAllSlots();

        /// <summary>
        /// 每帧更新 (处理漂移充能)
        /// </summary>
        void Tick(float deltaTime, bool isDrifting);
    }
}
