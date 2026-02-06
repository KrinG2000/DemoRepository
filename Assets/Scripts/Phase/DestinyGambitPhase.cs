// ============================================================
// DestinyGambitPhase.cs — 天命赌场相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 天命赌场相位规则:
//       - 赢家压中天命牌 -> 天命暴击 (2倍奖励)
//       - 防守方压中天命牌 -> 天命反杀 (2倍惩罚)
//       - 输家压中天命牌 -> 无效运气 (无减免,UI显示DEFEAT)
//       - 胜天半子: 平局+双方都压中天命 -> 双方1.25~1.30x奖励
// ============================================================

using System;
using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class DestinyGambitPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.DestinyGambit;
        public override string DisplayName => "天命赌场";
        public override string Description => "天命的力量被放大！赢家压中天命获得暴击奖励，防守方压中天命将遭受反杀惩罚。平局双方压中天命触发胜天半子！";

        /// <summary>
        /// 胜天半子奖励倍率下限
        /// </summary>
        public const float ShengTianBanZiMinMultiplier = 1.25f;

        /// <summary>
        /// 胜天半子奖励倍率上限
        /// </summary>
        public const float ShengTianBanZiMaxMultiplier = 1.30f;

        private readonly Random _random;

        public DestinyGambitPhase() : this(new Random()) { }

        public DestinyGambitPhase(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// 天命赌场覆写天命匹配判定 — 增加胜天半子判定
        /// 平局时,如果双方出牌都与天命牌相同 (即三方同牌),触发胜天半子
        /// </summary>
        public override DestinyMatchType ResolveDestinyMatch(
            DuelOutcome outcome, CardType initiatorCard, CardType defenderCard, CardType destinyCard)
        {
            // 胜天半子: 平局 + 双方都压中天命牌
            if (outcome == DuelOutcome.Draw)
            {
                bool initiatorMatchesDestiny = (initiatorCard == destinyCard);
                bool defenderMatchesDestiny = (defenderCard == destinyCard);

                if (initiatorMatchesDestiny && defenderMatchesDestiny)
                {
                    return DestinyMatchType.BothMatchedDraw;
                }

                // 平局但未双方都压中 -> 无天命效果
                return DestinyMatchType.None;
            }

            // 非平局走标准判定
            return base.ResolveDestinyMatch(outcome, initiatorCard, defenderCard, destinyCard);
        }

        /// <summary>
        /// 天命赌场的核心: 大幅提升天命牌的奖惩倍率 + 胜天半子
        /// </summary>
        public override (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            float reward = 1.0f;
            float penalty = 1.0f;

            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched:
                    // 天命暴击: 赢家压中天命,奖励翻倍
                    reward = 2.0f;
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 暴击! 赢家压中天命牌，奖励翻倍!");
                    break;

                case DestinyMatchType.DefenderMatched:
                    // 天命反杀: 防守方压中天命,惩罚翻倍
                    penalty = 2.0f;
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 反杀! 防守方压中天命牌，惩罚翻倍!");
                    break;

                case DestinyMatchType.LoserMatched:
                    // 无效运气: 输家压中天命,无减免
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 无效运气! 输家压中天命牌，无法挽回败局。DEFEAT!");
                    break;

                case DestinyMatchType.BothMatchedDraw:
                    // 胜天半子: 双方都获得1.25~1.30x奖励
                    float range = ShengTianBanZiMaxMultiplier - ShengTianBanZiMinMultiplier;
                    reward = ShengTianBanZiMinMultiplier + (float)_random.NextDouble() * range;
                    GameEvents.RaisePhaseRuleTriggered(
                        $"天命赌场: 胜天半子! 平局且双方压中天命，双方获得 {reward:F2}x 奖励!");
                    break;
            }

            return (reward, penalty);
        }
    }
}
