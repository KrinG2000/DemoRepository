// ============================================================
// StatusEffect.cs — 状态效果数据模型
// 架构层级: Core (基础层, 无依赖)
// 说明: 定义赛车状态效果类型和数据结构。
//       CarController 接收 StatusEffect 并应用到车辆。
//       DuelSystem 只发出效果, 不直接操控车辆。
// ============================================================

namespace RacingCardGame.Core
{
    /// <summary>
    /// 状态效果类型
    /// </summary>
    public enum StatusEffectType
    {
        SpeedMultiplier,    // 速度倍率 (multiplier, duration) — 胜者加速
        SpeedCap,           // 极速上限 (capMultiplier, duration) — 败者限速
        LowFriction,        // 低摩擦/打滑 (frictionMultiplier, duration)
        SuperArmor,         // 霸体 (duration) — 免控
        Stun,               // 晕眩/瞬停 (duration) — 禁止操控
        MinorSlow           // 微减速 (multiplier, duration)
    }

    /// <summary>
    /// 状态效果数据 — 由 DuelSystem 生成, 由 CarController 消费
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType Type;
        public float Multiplier;    // 倍率参数 (SpeedMultiplier/SpeedCap/LowFriction/MinorSlow)
        public float Duration;      // 持续时间 (秒)
        public double AppliedTime;  // 被应用时的时间戳 (由CarController设置)
        public int SourcePlayerId;  // 效果来源玩家ID (用于日志)

        /// <summary>
        /// 检查效果是否已过期
        /// </summary>
        public bool IsExpired(float currentTime)
        {
            return currentTime >= AppliedTime + Duration;
        }

        /// <summary>
        /// 剩余时间
        /// </summary>
        public float RemainingTime(float currentTime)
        {
            float remaining = (float)(AppliedTime + Duration) - currentTime;
            return remaining > 0f ? remaining : 0f;
        }

        public override string ToString()
        {
            return $"{Type}(mul={Multiplier:F2}, dur={Duration:F1}s)";
        }

        // ---- 工厂方法 ----

        public static StatusEffect CreateSpeedMultiplier(float multiplier, float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.SpeedMultiplier, Multiplier = multiplier, Duration = duration, SourcePlayerId = sourceId };
        }

        public static StatusEffect CreateSpeedCap(float capMultiplier, float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.SpeedCap, Multiplier = capMultiplier, Duration = duration, SourcePlayerId = sourceId };
        }

        public static StatusEffect CreateLowFriction(float frictionMultiplier, float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.LowFriction, Multiplier = frictionMultiplier, Duration = duration, SourcePlayerId = sourceId };
        }

        public static StatusEffect CreateSuperArmor(float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.SuperArmor, Multiplier = 1f, Duration = duration, SourcePlayerId = sourceId };
        }

        public static StatusEffect CreateStun(float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.Stun, Multiplier = 0f, Duration = duration, SourcePlayerId = sourceId };
        }

        public static StatusEffect CreateMinorSlow(float multiplier, float duration, int sourceId = -1)
        {
            return new StatusEffect { Type = StatusEffectType.MinorSlow, Multiplier = multiplier, Duration = duration, SourcePlayerId = sourceId };
        }
    }
}
