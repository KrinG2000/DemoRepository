// ============================================================
// AIDriver.cs — AI赛车驾驶员
// 架构层级: AI
// 说明: 最小AI实现:
//   - 沿赛道直行 (不漂移, 匀速)
//   - 周期性"模拟漂移"充能
//   - 技能格满后随机尝试发起对决
//   - 子空间选牌用简单随机策略
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Vehicle;
using RacingCardGame.Charge;
using RacingCardGame.DuelFlow;

namespace RacingCardGame.AI
{
    public class AIDriver
    {
        public int PlayerId { get; private set; }

        private readonly ICarController _car;
        private readonly IChargeSystem _charge;
        private readonly IDuelSystem _duelSystem;
        private readonly Random _random;
        private readonly Func<int, float> _getCarPosition; // 查询其他车辆位置

        private float _duelCheckTimer;
        private float _driftSimTimer;
        private bool _simulatedDrifting;

        public AIDriver(
            int playerId,
            ICarController car,
            IChargeSystem charge,
            IDuelSystem duelSystem,
            Random random,
            Func<int, float> getCarPosition)
        {
            PlayerId = playerId;
            _car = car;
            _charge = charge;
            _duelSystem = duelSystem;
            _random = random;
            _getCarPosition = getCarPosition;
            _duelCheckTimer = 2f + (float)_random.NextDouble() * 3f; // stagger first check
        }

        /// <summary>
        /// AI每帧更新
        /// </summary>
        public void Tick(float deltaTime, List<int> allPlayerIds)
        {
            var cfg = BalanceConfig.Current;

            // 模拟漂移充能: AI每隔一段时间"漂移"2秒
            _driftSimTimer += deltaTime;
            if (!_simulatedDrifting && _driftSimTimer > 3f)
            {
                _simulatedDrifting = true;
                _driftSimTimer = 0f;
            }
            else if (_simulatedDrifting && _driftSimTimer > 2f)
            {
                _simulatedDrifting = false;
                _driftSimTimer = 0f;
            }

            // 漂移充能
            if (_simulatedDrifting)
            {
                _charge.Tick(deltaTime, true);
                if (_car is CarController cc)
                    cc.SetDrifting(true);
            }
            else
            {
                if (_car is CarController cc)
                    cc.SetDrifting(false);
            }

            // 技能格满后: 尝试发起对决
            if (_charge.CanCastDuelSkill && _car.IsControlEnabled)
            {
                _duelCheckTimer -= deltaTime;
                if (_duelCheckTimer <= 0f)
                {
                    _duelCheckTimer = cfg.AIDuelCheckInterval;

                    if (_random.NextDouble() < cfg.AIDuelChance)
                    {
                        TryFindAndDuel(allPlayerIds, cfg);
                    }
                }
            }
        }

        private void TryFindAndDuel(List<int> allPlayerIds, BalanceConfig cfg)
        {
            float myPos = _car.GetPosition();
            int bestTarget = -1;
            float bestDist = float.MaxValue;

            foreach (int pid in allPlayerIds)
            {
                if (pid == PlayerId) continue;

                float otherPos = _getCarPosition(pid);
                float dist = Math.Abs(otherPos - myPos);

                if (dist >= cfg.AIMinDuelDistance && dist <= cfg.AIMaxDuelDistance && dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = pid;
                }
            }

            if (bestTarget >= 0)
            {
                _duelSystem.TryStartDuel(PlayerId, bestTarget);
            }
        }
    }
}
