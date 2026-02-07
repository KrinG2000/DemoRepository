// ============================================================
// CarController.cs — 车辆控制器实现
// 架构层级: Vehicle
// 说明: 简化赛车模拟 (1D赛道)。管理位置/速度/漂移/状态效果。
//       不依赖 Unity 物理, 纯 C# 可测。
//       DuelSystem 通过 ApplyStatusEffect 施加效果,
//       CarController 只负责应用和过期管理。
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Skill;

namespace RacingCardGame.Vehicle
{
    public class CarController : ICarController
    {
        public int PlayerId { get; private set; }
        public bool IsControlEnabled { get; private set; } = true;

        private float _position;
        private float _baseSpeed;
        private bool _isDrifting;
        private readonly List<StatusEffect> _activeEffects = new List<StatusEffect>();
        private readonly ITimeProvider _timeProvider;

        public bool IsDrifting => _isDrifting;

        public bool IsStunned
        {
            get { return HasEffect(StatusEffectType.Stun) && !HasSuperArmor; }
        }

        public bool HasSuperArmor
        {
            get { return HasEffect(StatusEffectType.SuperArmor); }
        }

        public IReadOnlyList<StatusEffect> ActiveEffects => _activeEffects.AsReadOnly();

        public CarController(int playerId, ITimeProvider timeProvider, float startPosition = 0f)
        {
            PlayerId = playerId;
            _timeProvider = timeProvider;
            _position = startPosition;
            _baseSpeed = BalanceConfig.Current.BaseCarSpeed;
        }

        public void SetControlEnabled(bool enabled)
        {
            IsControlEnabled = enabled;
        }

        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (effect == null) return;

            // SuperArmor blocks stun
            if (effect.Type == StatusEffectType.Stun && HasSuperArmor)
                return;

            effect.AppliedTime = _timeProvider.CurrentTime;
            _activeEffects.Add(effect);
            GameEvents.RaiseStatusEffectApplied(PlayerId, effect);
        }

        public bool HasEffect(StatusEffectType type)
        {
            float now = _timeProvider.CurrentTime;
            return _activeEffects.Any(e => e.Type == type && !e.IsExpired(now));
        }

        public float GetPosition() => _position;
        public float GetSpeed() => CalculateEffectiveSpeed();
        public float GetBaseSpeed() => _baseSpeed;

        public void SetDrifting(bool drifting)
        {
            _isDrifting = drifting;
        }

        /// <summary>
        /// 每帧更新: 移动车辆, 过期效果清理
        /// </summary>
        public void Tick(float deltaTime)
        {
            CleanExpiredEffects();

            float speed;
            if (!IsControlEnabled)
            {
                // 子空间中: 自动直行, 低速
                speed = _baseSpeed * 0.3f;
            }
            else if (IsStunned)
            {
                speed = 0f;
            }
            else
            {
                speed = CalculateEffectiveSpeed();
            }

            _position += speed * deltaTime;
        }

        /// <summary>
        /// 计算当前有效速度 (考虑所有活跃状态效果)
        /// </summary>
        private float CalculateEffectiveSpeed()
        {
            float now = _timeProvider.CurrentTime;
            float speed = _baseSpeed;

            // SpeedMultiplier: 取最高倍率
            float maxSpeedMul = 1f;
            foreach (var e in _activeEffects)
            {
                if (e.Type == StatusEffectType.SpeedMultiplier && !e.IsExpired(now))
                    maxSpeedMul = Math.Max(maxSpeedMul, e.Multiplier);
            }
            speed *= maxSpeedMul;

            // MinorSlow: 取最低倍率
            float minSlow = 1f;
            foreach (var e in _activeEffects)
            {
                if (e.Type == StatusEffectType.MinorSlow && !e.IsExpired(now))
                    minSlow = Math.Min(minSlow, e.Multiplier);
            }
            speed *= minSlow;

            // LowFriction: 取最低倍率 (减速)
            float minFriction = 1f;
            foreach (var e in _activeEffects)
            {
                if (e.Type == StatusEffectType.LowFriction && !e.IsExpired(now))
                    minFriction = Math.Min(minFriction, e.Multiplier);
            }
            speed *= minFriction;

            // SpeedCap: 取最低上限
            float minCap = float.MaxValue;
            foreach (var e in _activeEffects)
            {
                if (e.Type == StatusEffectType.SpeedCap && !e.IsExpired(now))
                    minCap = Math.Min(minCap, _baseSpeed * e.Multiplier);
            }
            if (minCap < float.MaxValue && speed > minCap)
                speed = minCap;

            return speed;
        }

        private void CleanExpiredEffects()
        {
            float now = _timeProvider.CurrentTime;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].IsExpired(now))
                {
                    var expired = _activeEffects[i];
                    _activeEffects.RemoveAt(i);
                    GameEvents.RaiseStatusEffectExpired(PlayerId, expired.Type);
                }
            }
        }
    }
}
