// ============================================================
// SubspaceDuelManager.cs — 子空间对决管理器
// 架构层级: Duel (功能模块层)
// 依赖: Core/*, Card/*, Phase/*, Skill/*
// 说明: 子空间对决的完整流程管理器,协调门票消耗、出牌、
//       天命牌生成、相位规则应用、胜负判定和奖惩计算。
//       这是整个对决系统的核心编排者。
//
// 对决流程:
//   1. 验证前置条件 (技能格满、有门票)
//   2. 消耗门票 (最旧暗牌)
//   3. 生成天命牌 (随机)
//   4. 双方出牌
//   5. 相位预处理 (如小丑互换)
//   6. 判定胜负 (可能被相位修改规则)
//   7. 天命牌匹配判定
//   8. 计算奖惩倍率 (含相位修正)
//   9. 相位后处理
//  10. 发布对决结果事件
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

        /// <summary>
        /// 验证玩家是否满足发起对决的前置条件
        /// </summary>
        /// <param name="initiatorSkill">发起者技能格</param>
        /// <param name="initiatorCards">发起者手牌</param>
        /// <returns>(是否可发起, 失败原因)</returns>
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

        /// <summary>
        /// 验证出牌是否在当前相位下合法
        /// </summary>
        public (bool valid, string reason) ValidateCardChoice(CardType cardType)
        {
            PhaseBase phase = _phaseManager.ActivePhase;
            if (phase != null && !phase.IsCardPlayable(cardType))
            {
                return (false, $"当前相位 [{phase.DisplayName}] 禁止使用 {cardType}。");
            }
            return (true, null);
        }

        /// <summary>
        /// 执行完整的子空间对决流程
        /// </summary>
        /// <param name="initiatorId">发起者玩家ID</param>
        /// <param name="defenderId">防守方玩家ID</param>
        /// <param name="initiatorCards">发起者手牌管理器</param>
        /// <param name="initiatorSkill">发起者技能格</param>
        /// <param name="initiatorChoice">发起者出牌选择</param>
        /// <param name="defenderChoice">防守方出牌选择</param>
        /// <returns>完整的对决结果数据</returns>
        public DuelResultData ExecuteDuel(
            int initiatorId,
            int defenderId,
            CardManager initiatorCards,
            SkillSlotManager initiatorSkill,
            CardType initiatorChoice,
            CardType defenderChoice)
        {
            PhaseBase activePhase = _phaseManager.ActivePhase;

            // === Step 1: 消耗技能格 ===
            initiatorSkill.TryActivateSkill();

            // === Step 2: 消耗门票 (最旧暗牌) ===
            Card.Card ticket = initiatorCards.ConsumeTicket();
            // 门票消耗后不参与对决判定

            // === Step 3: 生成天命牌 ===
            CardType destinyCard = GenerateDestinyCard();

            // === Step 4: 记录原始出牌 ===
            CardType finalInitiatorCard = initiatorChoice;
            CardType finalDefenderCard = defenderChoice;

            // === Step 5: 相位预处理 (如小丑互换) ===
            bool cardsSwapped = false;
            if (activePhase != null)
            {
                activePhase.PreDuelModify(ref finalInitiatorCard, ref finalDefenderCard, out cardsSwapped);
            }

            // === Step 6: 判定胜负 ===
            DuelOutcome outcome;
            if (activePhase != null)
            {
                outcome = activePhase.ResolveDuel(finalInitiatorCard, finalDefenderCard);
            }
            else
            {
                outcome = ResolveStandardDuel(finalInitiatorCard, finalDefenderCard);
            }

            // === Step 7: 天命牌匹配判定 ===
            DestinyMatchType destinyMatch;
            if (activePhase != null)
            {
                destinyMatch = activePhase.ResolveDestinyMatch(outcome, finalInitiatorCard, finalDefenderCard, destinyCard);
            }
            else
            {
                destinyMatch = ResolveStandardDestinyMatch(outcome, finalInitiatorCard, finalDefenderCard, destinyCard);
            }

            // === Step 8: 计算奖惩倍率 (含相位修正) ===
            float rewardMultiplier = 1.0f;
            float penaltyMultiplier = 1.0f;
            if (activePhase != null)
            {
                (rewardMultiplier, penaltyMultiplier) = activePhase.CalculateMultipliers(destinyMatch);
            }

            // === Step 9: 构建结果数据 ===
            var result = new DuelResultData
            {
                InitiatorId = initiatorId,
                DefenderId = defenderId,
                InitiatorCard = finalInitiatorCard,
                DefenderCard = finalDefenderCard,
                DestinyCard = destinyCard,
                Outcome = outcome,
                DestinyMatch = destinyMatch,
                ActivePhase = activePhase?.Type ?? PhaseType.DestinyGambit,
                RewardMultiplier = rewardMultiplier,
                PenaltyMultiplier = penaltyMultiplier,
                CardsSwapped = cardsSwapped
            };

            // === Step 10: 相位后处理 ===
            if (activePhase != null)
            {
                activePhase.PostDuelEffect(result);
            }

            // === Step 11: 发布对决结果事件 ===
            GameEvents.RaiseDuelResolved(result);

            return result;
        }

        /// <summary>
        /// 生成天命牌 (随机从三种牌型中选择)
        /// </summary>
        public CardType GenerateDestinyCard()
        {
            int roll = _random.Next(3);
            return (CardType)roll;
        }

        /// <summary>
        /// 标准剪刀石头布判定 (无相位修正)
        /// </summary>
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

        /// <summary>
        /// 标准天命牌匹配判定 (无相位修正)
        /// </summary>
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
