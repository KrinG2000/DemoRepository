// ============================================================
// CeasefirePhase.cs — 止戈相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 止戈相位规则:
//       - 剪刀被强制转化为石头 ("剪视为石")
//       - 石头克制布 (克制关系反转: 石头撞晕布)
//       - 玩家仍然可以选择剪刀出牌,但系统会自动转为石头
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class CeasefirePhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.Ceasefire;
        public override string DisplayName => "止戈相位";
        public override string Description => "和平降临！剪刀被强制转化为石头(剪视为石)，石头可以撞晕布。";

        /// <summary>
        /// 止戈相位不再阻止出剪刀,而是允许出牌后强制转换
        /// 所有牌都可以选择,但剪刀会在PreDuelModify中被转换为石头
        /// </summary>
        public override bool IsCardPlayable(CardType cardType)
        {
            return true; // 允许选择任何牌,剪刀会被自动转换
        }

        /// <summary>
        /// 止戈相位的预处理: 将剪刀强制转化为石头 ("剪视为石")
        /// </summary>
        public override void PreDuelModify(ref CardType initiatorCard, ref CardType defenderCard, out bool swapped)
        {
            swapped = false;
            bool converted = false;

            if (initiatorCard == CardType.Scissors)
            {
                initiatorCard = CardType.Rock;
                converted = true;
            }

            if (defenderCard == CardType.Scissors)
            {
                defenderCard = CardType.Rock;
                converted = true;
            }

            if (converted)
            {
                GameEvents.RaisePhaseRuleTriggered("止戈相位: 剪视为石！剪刀被强制转化为石头。");
            }
        }

        /// <summary>
        /// 止戈相位的核心: 改变克制关系
        /// 石头 > 布 (石头撞晕布), 布不再克石头
        /// 由于剪刀已被转换为石头, 实际对决只有石头和布
        /// </summary>
        public override DuelOutcome ResolveDuel(CardType initiatorCard, CardType defenderCard)
        {
            // 相同牌型 -> 平局
            if (initiatorCard == defenderCard)
                return DuelOutcome.Draw;

            // 止戈规则: 石头克制布 (撞晕)
            bool initiatorWins;

            if (initiatorCard == CardType.Rock && defenderCard == CardType.Paper)
            {
                // 石头撞晕布 (止戈特殊规则)
                initiatorWins = true;
                GameEvents.RaisePhaseRuleTriggered("止戈相位: 石头撞晕布！规则已被改变！");
            }
            else if (initiatorCard == CardType.Paper && defenderCard == CardType.Rock)
            {
                // 布被石头撞晕 (止戈特殊规则)
                initiatorWins = false;
                GameEvents.RaisePhaseRuleTriggered("止戈相位: 石头撞晕布！规则已被改变！");
            }
            else
            {
                // 理论上不应出现其他组合 (剪刀已被转为石头)
                // 但做防御处理,回退到标准规则
                return base.ResolveDuel(initiatorCard, defenderCard);
            }

            return initiatorWins ? DuelOutcome.Win : DuelOutcome.Lose;
        }
    }
}
