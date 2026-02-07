// ============================================================
// PhaseBase.cs — 相位基类 (抽象)
// 架构层级: Phase (功能模块层)
// 依赖: Core/GameEnums, Core/GameEvents, Config/BalanceConfig
// 说明: 所有相位的抽象基类。使用策略模式。
//       默认倍率从BalanceConfig读取。
// ============================================================

using RacingCardGame.Core;
using RacingCardGame.Config;

namespace RacingCardGame.Phase
{
    public abstract class PhaseBase
    {
        public abstract PhaseType Type { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        public virtual void PreDuelModify(ref CardType initiatorCard, ref CardType defenderCard, out bool swapped)
        {
            swapped = false;
        }

        public virtual DuelOutcome ResolveDuel(CardType initiatorCard, CardType defenderCard)
        {
            if (initiatorCard == defenderCard)
                return DuelOutcome.Draw;

            bool initiatorWins =
                (initiatorCard == CardType.Rock && defenderCard == CardType.Scissors) ||
                (initiatorCard == CardType.Scissors && defenderCard == CardType.Paper) ||
                (initiatorCard == CardType.Paper && defenderCard == CardType.Rock);

            return initiatorWins ? DuelOutcome.Win : DuelOutcome.Lose;
        }

        public virtual DestinyMatchType ResolveDestinyMatch(
            DuelOutcome outcome, CardType initiatorCard, CardType defenderCard, CardType destinyCard)
        {
            if (outcome == DuelOutcome.Draw)
                return DestinyMatchType.None;

            CardType winnerCard = outcome == DuelOutcome.Win ? initiatorCard : defenderCard;
            CardType loserCard = outcome == DuelOutcome.Win ? defenderCard : initiatorCard;

            if (winnerCard == destinyCard)
                return DestinyMatchType.WinnerMatched;
            if (defenderCard == destinyCard)
                return DestinyMatchType.DefenderMatched;
            if (loserCard == destinyCard)
                return DestinyMatchType.LoserMatched;

            return DestinyMatchType.None;
        }

        public virtual DestinyEffectType ResolveDestinyEffect(DestinyMatchType destinyMatch)
        {
            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched: return DestinyEffectType.Crit;
                case DestinyMatchType.DefenderMatched: return DestinyEffectType.Counter;
                case DestinyMatchType.LoserMatched: return DestinyEffectType.BadLuck;
                case DestinyMatchType.BothMatchedDraw: return DestinyEffectType.ShengTianBanZi;
                default: return DestinyEffectType.None;
            }
        }

        public virtual (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            var config = BalanceConfig.Current;
            float reward = 1.0f;
            float penalty = 1.0f;

            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched:
                    reward = config.CritMultiplier;
                    break;
                case DestinyMatchType.DefenderMatched:
                    penalty = config.CounterMultiplier;
                    break;
            }

            return (reward, penalty);
        }

        public virtual float GetChargeSpeedMultiplier()
        {
            return 1.0f;
        }

        public virtual void PostDuelEffect(DuelResultData result)
        {
        }

        public virtual bool IsCardPlayable(CardType cardType)
        {
            return true;
        }
    }
}
