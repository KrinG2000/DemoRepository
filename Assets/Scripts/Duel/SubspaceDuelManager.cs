// ============================================================
// SubspaceDuelManager.cs — 子空间对决管理器
// 架构层级: Duel (功能模块层)
// 依赖: Core/*, Card/*, Phase/*, Skill/*
// 说明: 子空间对决的完整流程管理器,协调门票消耗、出牌、
//       天命牌生成、相位规则应用、胜负判定和奖惩计算。
//       这是整个对决系统的核心编排者。
//
// 对决流程:
//   1. 验证前置条件 (技能格满、有门票、防守方无幽灵保护)
//   2. 消耗技能格
//   3. 消耗门票 (最旧暗牌)
//   4. 生成天命牌 (随机)
//   5. 记录原始出牌
//   6. 相位预处理 (止戈: 剪刀→石头; 小丑: 互换)
//   7. 判定胜负 (可能被相位修改规则)
//   8. 天命牌匹配判定 (含胜天半子)
//   9. 转换为天命效果类型
//  10. 计算奖惩倍率 (含相位修正)
//  11. 构建结果数据 (含所有新字段)
//  12. 相位后处理 (过热/幽灵保护)
//  13. 发布对决结果事件
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
        /// 验证防守方是否可以被拉入对决 (检查幽灵保护)
        /// </summary>
        public (bool canPull, string reason) ValidateDefenderPullable(GhostProtectionTracker defenderGhostTracker)
        {
            if (defenderGhostTracker != null && defenderGhostTracker.IsProtected)
            {
                return (false, $"防守方处于幽灵保护中 (剩余{defenderGhostTracker.RemainingProtectionTime:F1}秒),无法被拉入子空间。");
            }
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
            initiatorCards.ConsumeTicket();
            // 门票消耗后不参与对决判定

            // === Step 3: 生成天命牌 ===
            CardType destinyCard = GenerateDestinyCard();

            // === Step 4: 记录原始出牌 ===
            CardType originalInitiatorCard = initiatorChoice;
            CardType originalDefenderCard = defenderChoice;
            CardType finalInitiatorCard = initiatorChoice;
            CardType finalDefenderCard = defenderChoice;

            // === Step 5: 相位预处理 ===
            bool cardsSwapped = false;
            bool scissorsConverted = false;

            if (activePhase != null)
            {
                // 先执行相位的PreDuelModify (止戈转换剪刀 / 小丑互换)
                activePhase.PreDuelModify(ref finalInitiatorCard, ref finalDefenderCard, out cardsSwapped);

                // 检测是否发生了剪刀→石头的转换
                if (activePhase.Type == PhaseType.Ceasefire)
                {
                    scissorsConverted = (originalInitiatorCard == CardType.Scissors && finalInitiatorCard == CardType.Rock)
                                     || (originalDefenderCard == CardType.Scissors && finalDefenderCard == CardType.Rock);
                }
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

            // === Step 8: 转换为天命效果类型 ===
            DestinyEffectType destinyEffect;
            if (activePhase != null)
            {
                destinyEffect = activePhase.ResolveDestinyEffect(destinyMatch);
            }
            else
            {
                destinyEffect = DestinyEffectType.None;
            }

            // === Step 9: 计算奖惩倍率 (含相位修正) ===
            float rewardMultiplier = 1.0f;
            float penaltyMultiplier = 1.0f;
            if (activePhase != null)
            {
                (rewardMultiplier, penaltyMultiplier) = activePhase.CalculateMultipliers(destinyMatch);
            }

            // === Step 10: 构建结果数据 ===
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

            // === Step 11: 相位后处理 ===
            if (activePhase != null)
            {
                activePhase.PostDuelEffect(result);
            }

            // === Step 12: 发布对决结果事件 ===
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
