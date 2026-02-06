// ============================================================
// PhaseManager.cs — 相位管理器
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase 及其子类, Core/GameEnums, Core/GameEvents
// 说明: 负责相位的随机选择与互斥锁定。每局游戏开始时
//       随机选择一个相位并锁定,整局只有该相位生效。
//       提供当前激活相位的访问接口。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class PhaseManager
    {
        /// <summary>
        /// 所有可用相位的注册表
        /// </summary>
        private readonly Dictionary<PhaseType, PhaseBase> _phases = new Dictionary<PhaseType, PhaseBase>();

        /// <summary>
        /// 当前锁定的相位 (每局只有一个)
        /// </summary>
        public PhaseBase ActivePhase { get; private set; }

        /// <summary>
        /// 当前锁定的相位类型
        /// </summary>
        public PhaseType? ActivePhaseType => ActivePhase?.Type;

        /// <summary>
        /// 相位是否已锁定
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// 随机数生成器 (可注入用于测试)
        /// </summary>
        private readonly Random _random;

        public PhaseManager() : this(new Random()) { }

        public PhaseManager(Random random)
        {
            _random = random;
            RegisterDefaultPhases();
        }

        /// <summary>
        /// 注册所有默认相位
        /// </summary>
        private void RegisterDefaultPhases()
        {
            RegisterPhase(new DestinyGambitPhase(_random));
            RegisterPhase(new JokerPhase(_random));
            RegisterPhase(new CeasefirePhase());
            RegisterPhase(new InfiniteFirepowerPhase());
        }

        /// <summary>
        /// 注册一个相位到注册表
        /// </summary>
        public void RegisterPhase(PhaseBase phase)
        {
            _phases[phase.Type] = phase;
        }

        /// <summary>
        /// 随机选择并锁定一个相位 (每局开始时调用)
        /// 相位互斥: 一旦锁定,本局不可更改
        /// </summary>
        /// <returns>锁定的相位</returns>
        public PhaseBase LockRandomPhase()
        {
            if (IsLocked)
                throw new InvalidOperationException("Phase is already locked for this session. Call Reset() first.");

            if (_phases.Count == 0)
                throw new InvalidOperationException("No phases registered.");

            // 获取所有相位类型
            var phaseTypes = new List<PhaseType>(_phases.Keys);

            // 随机选择
            int index = _random.Next(phaseTypes.Count);
            PhaseType selectedType = phaseTypes[index];

            return LockPhase(selectedType);
        }

        /// <summary>
        /// 锁定指定相位 (用于测试或特殊模式)
        /// </summary>
        /// <param name="type">要锁定的相位类型</param>
        /// <returns>锁定的相位</returns>
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

        /// <summary>
        /// 获取指定类型的相位实例 (不影响锁定状态)
        /// </summary>
        public PhaseBase GetPhase(PhaseType type)
        {
            return _phases.ContainsKey(type) ? _phases[type] : null;
        }

        /// <summary>
        /// 获取所有已注册的相位类型
        /// </summary>
        public IReadOnlyCollection<PhaseType> GetRegisteredPhaseTypes()
        {
            return _phases.Keys;
        }

        /// <summary>
        /// 重置相位管理器 (新一局开始前调用)
        /// </summary>
        public void Reset()
        {
            ActivePhase = null;
            IsLocked = false;
        }
    }
}
