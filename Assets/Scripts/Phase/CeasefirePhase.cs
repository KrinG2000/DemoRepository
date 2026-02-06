// ============================================================
// CeasefirePhase.cs — 止戈相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 止戈相位规则:
//       - 禁用剪刀牌 (不可出剪刀)
//       - 石头击败布 (克制关系反转: 石头>布, 布不克石头)
//       - 改变了基本的剪刀石头布规则,要求玩家适应新策略
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class CeasefirePhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.Ceasefire;
        public override string DisplayName => "止戈相位";
        public override string Description => "和平降临！禁用剪刀，石头克制布。只有石头和布可以使用。";

        /// <summary>
        /// 止戈相位禁用剪刀
        /// </summary>
        public override bool IsCardPlayable(CardType cardType)
        {
            if (cardType == CardType.Scissors)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 止戈相位的核心: 改变克制关系
        /// 石头 > 布 (反转), 剪刀被禁用
        /// 实际对决只有石头和布, 石头胜布
        /// </summary>
        public override DuelOutcome ResolveDuel(CardType initiatorCard, CardType defenderCard)
        {
            // 相同牌型 -> 平局
            if (initiatorCard == defenderCard)
                return DuelOutcome.Draw;

            // 止戈规则: 石头克制布
            bool initiatorWins;

            if (initiatorCard == CardType.Rock && defenderCard == CardType.Paper)
            {
                // 石头克布 (止戈特殊规则)
                initiatorWins = true;
                GameEvents.RaisePhaseRuleTriggered("止戈相位: 石头克制布！规则已被改变！");
            }
            else if (initiatorCard == CardType.Paper && defenderCard == CardType.Rock)
            {
                // 布被石头克 (止戈特殊规则)
                initiatorWins = false;
                GameEvents.RaisePhaseRuleTriggered("止戈相位: 石头克制布！规则已被改变！");
            }
            else
            {
                // 如果有剪刀参与 (不应发生, 但做防御处理)
                // 回退到标准规则
                return base.ResolveDuel(initiatorCard, defenderCard);
            }

            return initiatorWins ? DuelOutcome.Win : DuelOutcome.Lose;
        }
    }
}
