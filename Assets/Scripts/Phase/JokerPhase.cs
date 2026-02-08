// ============================================================
// JokerPhase.cs — 小丑相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents, Config/BalanceConfig
// 说明: 触发概率从BalanceConfig读取 (默认20%)
// ============================================================

using System;
using RacingCardGame.Core;
using RacingCardGame.Config;

namespace RacingCardGame.Phase
{
    public class JokerPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.Joker;
        public override string DisplayName => "小丑相位";
        public override string Description => "混乱降临！每次对决前有概率互换双方的出牌。";

        private readonly Random _random;

        public JokerPhase() : this(new Random()) { }

        public JokerPhase(Random random)
        {
            _random = random;
        }

        public override void PreDuelModify(ref CardType initiatorCard, ref CardType defenderCard, out bool swapped)
        {
            float roll = (float)_random.NextDouble();
            float chance = BalanceConfig.Current.JesterTriggerChance;

            if (roll < chance)
            {
                CardType temp = initiatorCard;
                initiatorCard = defenderCard;
                defenderCard = temp;
                swapped = true;
                GameEvents.RaisePhaseRuleTriggered(
                    $"小丑相位: 混乱触发! 双方出牌被互换! (骰子: {roll:F2})");
            }
            else
            {
                swapped = false;
            }
        }
    }
}
