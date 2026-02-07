// ============================================================
// SimpleDuelPresentation.cs — 简易对决演出实现 (MVP)
// 架构层级: Presentation
// 说明: 纯C#占位实现。不做复杂动画,只保证流程正确:
//       Enter后冻结控制, Pick/Lock有倒计时, Exit后回主赛道。
//       AI使用简单随机策略; 本地玩家在MVP中也自动选择。
// ============================================================

using System;
using RacingCardGame.Core;

namespace RacingCardGame.Presentation
{
    public class SimpleDuelPresentation : IDuelPresentation
    {
        public bool IsInSubspace { get; private set; }

        private readonly Random _random;
        private int _currentAttackerId;
        private int _currentDefenderId;

        // 玩家手动选牌回调 (可由UI层设置)
        // 若为null则自动选第一张
        public Func<CardType[], CardType> HumanPickCallback { get; set; }

        public SimpleDuelPresentation(Random random = null)
        {
            _random = random ?? new Random();
        }

        public void EnterSubspace(int attackerId, int defenderId)
        {
            IsInSubspace = true;
            _currentAttackerId = attackerId;
            _currentDefenderId = defenderId;
        }

        public void ExitSubspace()
        {
            IsInSubspace = false;
        }

        public CardPickResult RequestCardPick(
            CardType[] attackerSlots, CardType[] defenderSlots,
            float timeLimit, bool attackerIsHuman, bool defenderIsHuman)
        {
            var result = new CardPickResult();

            // Attacker pick
            if (attackerIsHuman && HumanPickCallback != null)
                result.AttackerChoice = HumanPickCallback(attackerSlots);
            else if (attackerIsHuman && attackerSlots.Length > 0)
                result.AttackerChoice = attackerSlots[0]; // MVP: auto-pick first
            else
                result.AttackerChoice = AIPickCard(attackerSlots);

            // Defender pick
            if (defenderIsHuman && HumanPickCallback != null)
                result.DefenderChoice = HumanPickCallback(defenderSlots);
            else if (defenderIsHuman && defenderSlots.Length > 0)
                result.DefenderChoice = defenderSlots[0];
            else
                result.DefenderChoice = AIPickCard(defenderSlots);

            return result;
        }

        /// <summary>
        /// AI选牌策略: 60%随机, 20%偏向剪刀, 20%偏向石头
        /// </summary>
        private CardType AIPickCard(CardType[] availableSlots)
        {
            if (availableSlots == null || availableSlots.Length == 0)
                return (CardType)_random.Next(3); // fallback

            if (availableSlots.Length == 1)
                return availableSlots[0];

            float roll = (float)_random.NextDouble();
            if (roll < 0.6f)
            {
                // Random from available
                return availableSlots[_random.Next(availableSlots.Length)];
            }
            else if (roll < 0.8f)
            {
                // Prefer scissors
                foreach (var c in availableSlots)
                    if (c == CardType.Scissors) return c;
                return availableSlots[_random.Next(availableSlots.Length)];
            }
            else
            {
                // Prefer rock
                foreach (var c in availableSlots)
                    if (c == CardType.Rock) return c;
                return availableSlots[_random.Next(availableSlots.Length)];
            }
        }
    }
}
