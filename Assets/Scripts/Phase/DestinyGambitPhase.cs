// ============================================================
// DestinyGambitPhase.cs — 天命赌场相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents, Config/BalanceConfig
// 说明: 倍率从BalanceConfig读取 (Crit=1.5x, Counter=1.5x, ConquerHeaven=1.3x)
// ============================================================

using System;
using RacingCardGame.Core;
using RacingCardGame.Config;

namespace RacingCardGame.Phase
{
    public class DestinyGambitPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.DestinyGambit;
        public override string DisplayName => "天命赌场";
        public override string Description => "天命的力量被放大！赢家压中天命获得暴击奖励，防守方压中天命将遭受反杀惩罚。平局双方压中天命触发胜天半子！";

        private readonly Random _random;

        public DestinyGambitPhase() : this(new Random()) { }

        public DestinyGambitPhase(Random random)
        {
            _random = random;
        }

        public override DestinyMatchType ResolveDestinyMatch(
            DuelOutcome outcome, CardType initiatorCard, CardType defenderCard, CardType destinyCard)
        {
            if (outcome == DuelOutcome.Draw)
            {
                if (initiatorCard == destinyCard && defenderCard == destinyCard)
                    return DestinyMatchType.BothMatchedDraw;
                return DestinyMatchType.None;
            }
            return base.ResolveDestinyMatch(outcome, initiatorCard, defenderCard, destinyCard);
        }

        public override (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            var config = BalanceConfig.Current;
            float reward = 1.0f;
            float penalty = 1.0f;

            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched:
                    reward = config.CritMultiplier;
                    GameEvents.RaisePhaseRuleTriggered(
                        $"天命赌场: 暴击! 赢家压中天命牌，奖励{config.CritMultiplier}x!");
                    break;
                case DestinyMatchType.DefenderMatched:
                    penalty = config.CounterMultiplier;
                    GameEvents.RaisePhaseRuleTriggered(
                        $"天命赌场: 反杀! 防守方压中天命牌，惩罚{config.CounterMultiplier}x!");
                    break;
                case DestinyMatchType.LoserMatched:
                    GameEvents.RaisePhaseRuleTriggered(
                        "天命赌场: 无效运气! 输家压中天命牌，无法挽回败局。DEFEAT!");
                    break;
                case DestinyMatchType.BothMatchedDraw:
                    reward = config.ConquerHeavenMultiplier;
                    GameEvents.RaisePhaseRuleTriggered(
                        $"天命赌场: 胜天半子! 平局且双方压中天命，双方获得 {config.ConquerHeavenMultiplier}x 奖励!");
                    break;
            }

            return (reward, penalty);
        }
    }
}
