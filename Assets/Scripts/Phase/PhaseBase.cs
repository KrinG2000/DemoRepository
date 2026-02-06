// ============================================================
// PhaseBase.cs — 相位基类 (抽象)
// 架构层级: Phase (功能模块层)
// 依赖: Core/GameEnums, Core/GameEvents
// 说明: 所有相位的抽象基类。使用策略模式,每个相位子类
//       独立实现自己的规则逻辑。PhaseManager持有当前
//       激活相位的引用,对决系统通过基类接口调用相位规则。
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Phase
{
    public abstract class PhaseBase
    {
        /// <summary>
        /// 相位类型标识
        /// </summary>
        public abstract PhaseType Type { get; }

        /// <summary>
        /// 相位名称 (中文显示名)
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 相位描述 (简短规则说明)
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 对决前处理 — 在双方出牌后、判定胜负前调用
        /// 可修改双方出牌 (如小丑相位互换)
        /// </summary>
        /// <param name="initiatorCard">发起者出牌 (可被修改)</param>
        /// <param name="defenderCard">防守方出牌 (可被修改)</param>
        /// <param name="swapped">是否发生了牌面互换</param>
        public virtual void PreDuelModify(ref CardType initiatorCard, ref CardType defenderCard, out bool swapped)
        {
            swapped = false;
            // 默认不修改出牌
        }

        /// <summary>
        /// 判定对决胜负 — 可被子类覆写以改变胜负规则 (如止戈相位)
        /// </summary>
        /// <param name="initiatorCard">发起者出牌</param>
        /// <param name="defenderCard">防守方出牌</param>
        /// <returns>对决结果</returns>
        public virtual DuelOutcome ResolveDuel(CardType initiatorCard, CardType defenderCard)
        {
            // 标准剪刀石头布规则
            if (initiatorCard == defenderCard)
                return DuelOutcome.Draw;

            bool initiatorWins =
                (initiatorCard == CardType.Rock && defenderCard == CardType.Scissors) ||
                (initiatorCard == CardType.Scissors && defenderCard == CardType.Paper) ||
                (initiatorCard == CardType.Paper && defenderCard == CardType.Rock);

            return initiatorWins ? DuelOutcome.Win : DuelOutcome.Lose;
        }

        /// <summary>
        /// 判定天命牌匹配 — 可被子类覆写以改变天命牌效果
        /// </summary>
        /// <param name="outcome">对决结果</param>
        /// <param name="initiatorCard">发起者出牌</param>
        /// <param name="defenderCard">防守方出牌</param>
        /// <param name="destinyCard">天命牌</param>
        /// <returns>天命牌匹配类型</returns>
        public virtual DestinyMatchType ResolveDestinyMatch(
            DuelOutcome outcome, CardType initiatorCard, CardType defenderCard, CardType destinyCard)
        {
            CardType winnerCard;
            CardType loserCard;

            if (outcome == DuelOutcome.Draw)
                return DestinyMatchType.None;

            if (outcome == DuelOutcome.Win)
            {
                winnerCard = initiatorCard;
                loserCard = defenderCard;
            }
            else
            {
                winnerCard = defenderCard;
                loserCard = initiatorCard;
            }

            // 赢家压中天命牌 -> 奖励加成
            if (winnerCard == destinyCard)
                return DestinyMatchType.WinnerMatched;

            // 防守方压中天命牌 -> 惩罚加成 (防守方不一定是输家)
            if (defenderCard == destinyCard)
                return DestinyMatchType.DefenderMatched;

            // 输家压中天命牌 -> 无效运气
            if (loserCard == destinyCard)
                return DestinyMatchType.LoserMatched;

            return DestinyMatchType.None;
        }

        /// <summary>
        /// 计算奖励倍率 — 子类可覆写以调整奖惩
        /// </summary>
        /// <param name="destinyMatch">天命牌匹配结果</param>
        /// <returns>(奖励倍率, 惩罚倍率)</returns>
        public virtual (float reward, float penalty) CalculateMultipliers(DestinyMatchType destinyMatch)
        {
            float reward = 1.0f;
            float penalty = 1.0f;

            switch (destinyMatch)
            {
                case DestinyMatchType.WinnerMatched:
                    reward = 1.5f;  // 赢家压中天命,奖励加成50%
                    break;
                case DestinyMatchType.DefenderMatched:
                    penalty = 1.5f; // 防守方压中天命,惩罚加成50%
                    break;
                case DestinyMatchType.LoserMatched:
                    // 输家压中天命 -> 无效运气,无减免
                    break;
            }

            return (reward, penalty);
        }

        /// <summary>
        /// 获取充能速度倍率 — 相位对充能速度的影响
        /// </summary>
        public virtual float GetChargeSpeedMultiplier()
        {
            return 1.0f; // 默认无修改
        }

        /// <summary>
        /// 对决后处理 — 在判定结束后调用,用于相位特殊效果
        /// </summary>
        /// <param name="result">完整对决结果</param>
        public virtual void PostDuelEffect(DuelResultData result)
        {
            // 默认无后处理
        }

        /// <summary>
        /// 验证出牌是否合法 — 某些相位可能禁用特定牌型
        /// </summary>
        /// <param name="cardType">要出的牌</param>
        /// <returns>是否合法</returns>
        public virtual bool IsCardPlayable(CardType cardType)
        {
            return true; // 默认所有牌可用
        }
    }
}
