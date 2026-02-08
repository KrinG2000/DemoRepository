// ============================================================
// SubspaceDuelManager.cs — 子空间对决管理器
// 架构层级: Duel (功能模块层)
// 依赖: Core/*, Card/*, Phase/*, Skill/*
// 说明: 子空间对决的完整流程管理器。DuelLog集成在GameManager层。
// ============================================================

using System;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Phase;
using RacingCardGame.Skill;

namespace RacingCardGame.Duel
{
    public class SubspaceDuelManager
    {
        private readonly PhaseManager _phaseManager;
        private readonly Random _random;

        public SubspaceDuelManager(PhaseManager phaseManager) : this(phaseManager, new Random()) { }

        public SubspaceDuelManager(PhaseManager phaseManager, Random random)
        {
            _phaseManager = phaseManager ?? throw new ArgumentNullException(nameof(phaseManager));
            _random = random;
        }

        public (bool canInitiate, string reason) ValidateDuelConditions(
            SkillSlotManager initiatorSkill, CardManager initiatorCards)
        {
            if (!initiatorSkill.IsFullyCharged)
                return (false, "技能格未满,无法发起子空间对决。");
            if (initiatorSkill.IsOverheated)
                return (false, "技能过热中,无法发起子空间对决。");
            if (!initiatorCards.HasTicket)
                return (false, "没有暗牌可消耗为门票,无法进入子空间。");
            if (!_phaseManager.IsLocked)
                return (false, "相位未锁定,游戏状态异常。");
            return (true, null);
        }

        public (bool canPull, string reason) ValidateDefenderPullable(GhostProtectionTracker defenderGhostTracker)
        {
            if (defenderGhostTracker != null && defenderGhostTracker.IsProtected)
                return (false, $"防守方处于幽灵保护中 (剩余{defenderGhostTracker.RemainingProtectionTime:F1}秒),无法被拉入子空间。");
            return (true, null);
        }

        public (bool valid, string reason) ValidateCardChoice(CardType cardType)
        {
            PhaseBase phase = _phaseManager.ActivePhase;
            if (phase != null && !phase.IsCardPlayable(cardType))
                return (false, $"当前相位 [{phase.DisplayName}] 禁止使用 {cardType}。");
            return (true, null);
        }

        public DuelResultData ExecuteDuel(
            int initiatorId,
            int defenderId,
            CardManager initiatorCards,
            SkillSlotManager initiatorSkill,
            CardType initiatorChoice,
            CardType defenderChoice)
        {
            PhaseBase activePhase = _phaseManager.ActivePhase;

            // Step 1: Consume skill slots
            initiatorSkill.TryActivateSkill();

            // Step 2: Consume ticket (oldest dark card)
            initiatorCards.ConsumeTicket();

            // Step 3: Generate destiny card
            CardType destinyCard = GenerateDestinyCard();

            // Step 4: Record original cards
            CardType originalInitiatorCard = initiatorChoice;
            CardType originalDefenderCard = defenderChoice;
            CardType finalInitiatorCard = initiatorChoice;
            CardType finalDefenderCard = defenderChoice;

            // Step 5: Phase pre-processing
            bool cardsSwapped = false;
            bool scissorsConverted = false;

            if (activePhase != null)
            {
                activePhase.PreDuelModify(ref finalInitiatorCard, ref finalDefenderCard, out cardsSwapped);

                if (activePhase.Type == PhaseType.Ceasefire)
                {
                    scissorsConverted = (originalInitiatorCard == CardType.Scissors && finalInitiatorCard == CardType.Rock)
                                     || (originalDefenderCard == CardType.Scissors && finalDefenderCard == CardType.Rock);
                }
            }

            // Step 6: Resolve outcome
            DuelOutcome outcome;
            if (activePhase != null)
                outcome = activePhase.ResolveDuel(finalInitiatorCard, finalDefenderCard);
            else
                outcome = ResolveStandardDuel(finalInitiatorCard, finalDefenderCard);

            // Step 7: Destiny match
            DestinyMatchType destinyMatch;
            if (activePhase != null)
                destinyMatch = activePhase.ResolveDestinyMatch(outcome, finalInitiatorCard, finalDefenderCard, destinyCard);
            else
                destinyMatch = ResolveStandardDestinyMatch(outcome, finalInitiatorCard, finalDefenderCard, destinyCard);

            // Step 8: Destiny effect
            DestinyEffectType destinyEffect;
            if (activePhase != null)
                destinyEffect = activePhase.ResolveDestinyEffect(destinyMatch);
            else
                destinyEffect = DestinyEffectType.None;

            // Step 9: Calculate multipliers
            float rewardMultiplier = 1.0f;
            float penaltyMultiplier = 1.0f;
            if (activePhase != null)
                (rewardMultiplier, penaltyMultiplier) = activePhase.CalculateMultipliers(destinyMatch);

            // Step 10: Build result
            var result = new DuelResultData
            {
                InitiatorId = initiatorId,
                DefenderId = defenderId,
                InitiatorCard = finalInitiatorCard,
                DefenderCard = finalDefenderCard,
                OriginalInitiatorCard = originalInitiatorCard,
                OriginalDefenderCard = originalDefenderCard,
                DestinyCard = destinyCard,
                Outcome = outcome,
                DestinyMatch = destinyMatch,
                DestinyEffect = destinyEffect,
                ActivePhase = activePhase?.Type ?? PhaseType.DestinyGambit,
                RewardMultiplier = rewardMultiplier,
                PenaltyMultiplier = penaltyMultiplier,
                CardsSwapped = cardsSwapped,
                ScissorsConverted = scissorsConverted,
                IsShengTianBanZi = (destinyMatch == DestinyMatchType.BothMatchedDraw)
            };

            // Step 11: Post-duel effects
            if (activePhase != null)
                activePhase.PostDuelEffect(result);

            // Step 12: Publish result
            GameEvents.RaiseDuelResolved(result);

            return result;
        }

        public CardType GenerateDestinyCard()
        {
            int roll = _random.Next(3);
            return (CardType)roll;
        }

        private DuelOutcome ResolveStandardDuel(CardType initiator, CardType defender)
        {
            if (initiator == defender)
                return DuelOutcome.Draw;

            bool initiatorWins =
                (initiator == CardType.Rock && defender == CardType.Scissors) ||
                (initiator == CardType.Scissors && defender == CardType.Paper) ||
                (initiator == CardType.Paper && defender == CardType.Rock);

            return initiatorWins ? DuelOutcome.Win : DuelOutcome.Lose;
        }

        private DestinyMatchType ResolveStandardDestinyMatch(
            DuelOutcome outcome, CardType initiator, CardType defender, CardType destiny)
        {
            if (outcome == DuelOutcome.Draw)
                return DestinyMatchType.None;

            CardType winnerCard = outcome == DuelOutcome.Win ? initiator : defender;
            CardType loserCard = outcome == DuelOutcome.Win ? defender : initiator;

            if (winnerCard == destiny)
                return DestinyMatchType.WinnerMatched;
            if (defender == destiny)
                return DestinyMatchType.DefenderMatched;
            if (loserCard == destiny)
                return DestinyMatchType.LoserMatched;

            return DestinyMatchType.None;
        }
    }
}
