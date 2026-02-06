// ============================================================
// DestinyGambitPhase.cs — 天命赌场相位
// 架构层级: Phase (功能模块层)
// 依赖: Phase/PhaseBase, Core/GameEnums, Core/GameEvents
// 说明: 天命赌场相位规则:
//       - 赢家压中天命牌 -> 暴击奖励 (2倍奖励)
//       - 防守方压中天命牌 -> 反杀惩罚 (2倍惩罚)
//       - 强化了天命牌的影响力,鼓励冒险出牌
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public class DestinyGambitPhase : PhaseBase
    {
        public override PhaseType Type => PhaseType.DestinyGambit;
        public override string DisplayName => "天命赌场";
        public override string Description => "天命的力量被放大！赢家压中天命获得暴击奖励，防守方压中天命将遭受反杀惩罚。";

        /// <summary>
        /// 天命赌场的核心: 大幅提升天命牌的奖惩倍率
        /// </summary>
        public override (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            float reward = 1.0f;
            float penalty = 1.0f;

            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched:
                    // 暴击奖励: 赢家压中天命,奖励翻倍
                    reward = 2.0f;
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 暴击! 赢家压中天命牌，奖励翻倍!");
                    break;
                case DestinyMatchType.DefenderMatched:
                    // 反杀惩罚: 防守方压中天命,惩罚翻倍
                    penalty = 2.0f;
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 反杀! 防守方压中天命牌，惩罚翻倍!");
                    break;
                case DestinyMatchType.LoserMatched:
                    // 输家压中天命 -> 依然无效运气
                    GameEvents.RaisePhaseRuleTriggered("天命赌场: 输家压中天命牌，但运气无法挽回败局。");
                    break;
            }

            return (reward, penalty);
        }
    }
}
