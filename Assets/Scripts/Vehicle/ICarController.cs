// ============================================================
// ICarController.cs — 车辆控制器接口
// 架构层级: Vehicle (积木式接口)
// 说明: 定义车辆控制器的最小接口。DuelSystem 不直接操控车辆输入,
//       只通过 ApplyStatusEffect 发出效果给 CarController。
//       CarController 不知道相位规则, 只接收效果。
// ============================================================

using System.Collections.Generic;
using RacingCardGame.Core;

namespace RacingCardGame.Vehicle
{
    public interface ICarController
    {
        int PlayerId { get; }

        // ---- 控制 ----
        void SetControlEnabled(bool enabled);
        bool IsControlEnabled { get; }

        // ---- 状态效果 ----
        void ApplyStatusEffect(StatusEffect effect);
        IReadOnlyList<StatusEffect> ActiveEffects { get; }
        bool HasEffect(StatusEffectType type);

        // ---- 查询 ----
        float GetPosition();       // 赛道上的位置 (1D, 米)
        float GetSpeed();           // 当前速度 (米/秒)
        float GetBaseSpeed();       // 基础速度
        bool IsDrifting { get; }    // 是否漂移中
        bool IsStunned { get; }     // 是否晕眩中
        bool HasSuperArmor { get; } // 是否有霸体

        // ---- 更新 ----
        void Tick(float deltaTime);
    }
}
