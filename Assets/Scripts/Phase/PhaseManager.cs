// ============================================================
// PhaseManager.cs — 相位管理器
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents, Config/BalanceConfig
// 说明: 加权随机选择 (Casino:50, Peace:20, Jester:15, Overload:15)
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Config;

namespace RacingCardGame.Phase
{
    public class PhaseManager
    {
        private readonly Dictionary<PhaseType, PhaseBase> _phases = new Dictionary<PhaseType, PhaseBase>();

        public PhaseBase ActivePhase { get; private set; }
        public PhaseType? ActivePhaseType => ActivePhase?.Type;
        public bool IsLocked { get; private set; }

        private readonly Random _random;

        public PhaseManager() : this(new Random()) { }

        public PhaseManager(Random random)
        {
            _random = random;
            RegisterDefaultPhases();
        }

        private void RegisterDefaultPhases()
        {
            RegisterPhase(new DestinyGambitPhase(_random));
            RegisterPhase(new JokerPhase(_random));
            RegisterPhase(new CeasefirePhase());
            RegisterPhase(new InfiniteFirepowerPhase());
        }

        public void RegisterPhase(PhaseBase phase)
        {
            _phases[phase.Type] = phase;
        }

        /// <summary>
        /// 加权随机选择并锁定一个相位 (权重从BalanceConfig读取)
        /// </summary>
        public PhaseBase LockRandomPhase()
        {
            if (IsLocked)
                throw new InvalidOperationException("Phase is already locked for this session. Call Reset() first.");
            if (_phases.Count == 0)
                throw new InvalidOperationException("No phases registered.");

            var config = BalanceConfig.Current;
            var phaseTypes = new List<PhaseType>(_phases.Keys);

            int totalWeight = 0;
            foreach (var pt in phaseTypes)
                totalWeight += config.GetPhaseWeight(pt);

            if (totalWeight <= 0)
            {
                int index = _random.Next(phaseTypes.Count);
                return LockPhase(phaseTypes[index]);
            }

            int roll = _random.Next(totalWeight);
            int cumulative = 0;
            foreach (var pt in phaseTypes)
            {
                cumulative += config.GetPhaseWeight(pt);
                if (roll < cumulative)
                    return LockPhase(pt);
            }

            return LockPhase(phaseTypes[phaseTypes.Count - 1]);
        }

        public PhaseBase LockPhase(PhaseType type)
        {
            if (IsLocked)
                throw new InvalidOperationException("Phase is already locked for this session. Call Reset() first.");
            if (!_phases.ContainsKey(type))
                throw new ArgumentException($"Phase type {type} is not registered.");

            ActivePhase = _phases[type];
            IsLocked = true;
            GameEvents.RaisePhaseLocked(type);
            return ActivePhase;
        }

        public PhaseBase GetPhase(PhaseType type)
        {
            return _phases.ContainsKey(type) ? _phases[type] : null;
        }

        public IReadOnlyCollection<PhaseType> GetRegisteredPhaseTypes()
        {
            return _phases.Keys;
        }

        public void Reset()
        {
            ActivePhase = null;
            IsLocked = false;
        }
    }
}
