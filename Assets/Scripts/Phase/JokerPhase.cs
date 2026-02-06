// ============================================================
// JokerPhase.cs — 小丑相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 小丑相位规则:
//       - 每次对决前有20%概率互换双方出牌
//       - 增加不确定性和混乱感
//       - 玩家出牌策略需要考虑被互换的可能性
// ============================================================

using System;
using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class JokerPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.Joker;
        public override string DisplayName => "小丑相位";
        public override string Description => "混乱降临！每次对决前有20%的概率互换双方的出牌。";

        /// <summary>
        /// 互换触发概率 (20%)
        /// </summary>
        public const float SwapChance = 0.2f;

        /// <summary>
        /// 随机数生成器 (可注入用于测试)
        /// </summary>
        private readonly Random _random;

        public JokerPhase() : this(new Random()) { }

        public JokerPhase(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// 小丑相位的核心: 对决前可能互换双方出牌
        /// </summary>
        public override void PreDuelModify(ref CardType initiatorCard, ref CardType defenderCard, out bool swapped)
        {
            float roll = (float)_random.NextDouble();

            if (roll < SwapChance)
            {
                // 互换双方出牌
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
