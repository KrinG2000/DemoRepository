// ============================================================
// UnityTimeProvider.cs — Unity 落地用 ITimeProvider
// 架构层级: Unity Bridge
// 说明: 直接返回 UnityEngine.Time.time, 替代测试用的
//       ManualTimeProvider / SystemTimeProvider。
//       所有模块 (CarController, GhostTracker, DuelUI 等)
//       共用同一个实例即可。
// ============================================================

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using RacingCardGame.Skill;

namespace RacingCardGame.Unity
{
    /// <summary>
    /// Unity 落地用 ITimeProvider — 基于 Time.time
    /// </summary>
    public class UnityTimeProvider : ITimeProvider
    {
        public float CurrentTime => Time.time;
    }
}
#endif
