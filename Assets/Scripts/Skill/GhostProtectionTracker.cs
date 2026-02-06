// ============================================================
// GhostProtectionTracker.cs — 幽灵保护追踪器
// 架构层级: Skill (功能模块层)
// 依赖: Core/GameEvents
// 说明: 追踪玩家被拉入子空间的次数和时间,判断是否
//       触发幽灵保护。仅在无限火力相位中使用。
//       规则: 45秒内被拉入子空间3次 -> 20秒完全免疫
//
//       使用可注入的时间源,便于测试时控制时间流逝。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;

namespace RacingCardGame.Skill
{
    /// <summary>
    /// 时间提供者接口 — 用于可测试的时间注入
    /// </summary>
    public interface ITimeProvider
    {
        float CurrentTime { get; }
    }

    /// <summary>
    /// 默认时间提供者 — 使用系统时间 (秒)
    /// 在Unity中可替换为Time.time
    /// </summary>
    public class SystemTimeProvider : ITimeProvider
    {
        private readonly DateTime _startTime = DateTime.UtcNow;
        public float CurrentTime => (float)(DateTime.UtcNow - _startTime).TotalSeconds;
    }

    /// <summary>
    /// 可控时间提供者 — 测试用,手动控制时间推进
    /// </summary>
    public class ManualTimeProvider : ITimeProvider
    {
        public float CurrentTime { get; set; }

        public void Advance(float seconds)
        {
            CurrentTime += seconds;
        }
    }

    /// <summary>
    /// 幽灵保护追踪器 — 每个玩家一个实例
    /// </summary>
    public class GhostProtectionTracker
    {
        /// <summary>
        /// 所属玩家ID
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// 被拉入子空间的时间记录 (用于窗口内计数)
        /// </summary>
        private readonly List<float> _pullTimestamps = new List<float>();

        /// <summary>
        /// 时间提供者
        /// </summary>
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// 幽灵保护激活时间 (-1 = 未激活)
        /// </summary>
        private float _protectionStartTime = -1f;

        // 配置参数 (从InfiniteFirepowerPhase读取)
        private readonly int _pullThreshold;
        private readonly float _timeWindow;
        private readonly float _protectionDuration;

        /// <summary>
        /// 幽灵保护是否正在生效
        /// </summary>
        public bool IsProtected
        {
            get
            {
                if (_protectionStartTime < 0f)
                    return false;
                float elapsed = _timeProvider.CurrentTime - _protectionStartTime;
                if (elapsed >= _protectionDuration)
                {
                    // 保护已到期
                    _protectionStartTime = -1f;
                    GameEvents.RaiseGhostProtectionExpired(PlayerId);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 幽灵保护剩余时间 (秒)
        /// </summary>
        public float RemainingProtectionTime
        {
            get
            {
                if (_protectionStartTime < 0f)
                    return 0f;
                float remaining = _protectionDuration - (_timeProvider.CurrentTime - _protectionStartTime);
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>
        /// 当前时间窗口内被拉入次数
        /// </summary>
        public int RecentPullCount
        {
            get
            {
                PruneOldTimestamps();
                return _pullTimestamps.Count;
            }
        }

        public GhostProtectionTracker(int playerId, ITimeProvider timeProvider,
            int pullThreshold = 3, float timeWindow = 45f, float protectionDuration = 20f)
        {
            PlayerId = playerId;
            _timeProvider = timeProvider;
            _pullThreshold = pullThreshold;
            _timeWindow = timeWindow;
            _protectionDuration = protectionDuration;
        }

        /// <summary>
        /// 记录一次被拉入子空间,并检查是否触发幽灵保护
        /// </summary>
        /// <returns>是否触发了幽灵保护</returns>
        public bool RecordPull()
        {
            // 如果已经在保护中,不需要再记录
            if (IsProtected)
                return false;

            float currentTime = _timeProvider.CurrentTime;
            _pullTimestamps.Add(currentTime);
            PruneOldTimestamps();

            // 检查是否达到阈值
            if (_pullTimestamps.Count >= _pullThreshold)
            {
                ActivateProtection();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 激活幽灵保护
        /// </summary>
        private void ActivateProtection()
        {
            _protectionStartTime = _timeProvider.CurrentTime;
            _pullTimestamps.Clear(); // 重置计数
            GameEvents.RaiseGhostProtectionActivated(PlayerId, _protectionDuration);
        }

        /// <summary>
        /// 清除超出时间窗口的旧记录
        /// </summary>
        private void PruneOldTimestamps()
        {
            float cutoff = _timeProvider.CurrentTime - _timeWindow;
            _pullTimestamps.RemoveAll(t => t < cutoff);
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void Reset()
        {
            _pullTimestamps.Clear();
            _protectionStartTime = -1f;
        }
    }
}
