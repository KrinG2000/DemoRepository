// ============================================================
// DuelResultAdapter.cs — 对决结果数据适配器
// 架构层级: UI
// 说明: 将Duel层的DuelResultData转换为UI层的DuelResultContext,
//       解耦对决逻辑和UI展示
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.UI
{
    public static class DuelResultAdapter
    {
        /// <summary>
        /// 将PhaseType转换为DuelPhase (UI层命名)
        /// </summary>
        public static DuelPhase ConvertPhase(PhaseType phaseType)
        {
            switch (phaseType)
            {
                case PhaseType.DestinyGambit:      return DuelPhase.Casino;
                case PhaseType.Joker:              return DuelPhase.Jester;
                case PhaseType.InfiniteFirepower:   return DuelPhase.Overload;
                case PhaseType.Ceasefire:           return DuelPhase.Peace;
                default:                           return DuelPhase.Standard;
            }
        }

        /// <summary>
        /// 将DuelResultData转换为DuelResultContext
        /// </summary>
        public static DuelResultContext Convert(DuelResultData data)
        {
            var ctx = new DuelResultContext();

            // 基础信息
            ctx.Phase = ConvertPhase(data.ActivePhase);
            ctx.AttackerId = data.InitiatorId;
            ctx.DefenderId = data.DefenderId;
            ctx.AttackerPlayed = data.InitiatorCard;
            ctx.DefenderPlayed = data.DefenderCard;
            ctx.IsDraw = (data.Outcome == DuelOutcome.Draw);

            // 胜负判定
            switch (data.Outcome)
            {
                case DuelOutcome.Win:
                    ctx.WinnerId = data.InitiatorId;
                    ctx.LoserId = data.DefenderId;
                    break;
                case DuelOutcome.Lose:
                    ctx.WinnerId = data.DefenderId;
                    ctx.LoserId = data.InitiatorId;
                    break;
                default:
                    ctx.WinnerId = null;
                    ctx.LoserId = null;
                    break;
            }

            // Casino (天命赌场)
            ctx.CasinoHouseCard = data.DestinyCard;
            ctx.CasinoAttackerHitHouse = (data.InitiatorCard == data.DestinyCard);
            ctx.CasinoDefenderHitHouse = (data.DefenderCard == data.DestinyCard);

            if (ctx.LoserId.HasValue)
            {
                CardType loserCard = (ctx.LoserId.Value == data.InitiatorId)
                    ? data.InitiatorCard
                    : data.DefenderCard;
                ctx.CasinoLoserHitHouse = (loserCard == data.DestinyCard);
            }
            else
            {
                ctx.CasinoLoserHitHouse = false;
            }

            ctx.CasinoConquerHeavenTriggered = data.IsShengTianBanZi;

            // Jester (小丑相位)
            ctx.JesterSwapTriggered = data.CardsSwapped;

            // Peace (止戈相位)
            ctx.PeaceRuleApplied = data.ScissorsConverted;

            // Overload (无限火力)
            ctx.OverloadAttackerOverheatApplied = (data.OverheatDuration > 0);

            // 全局
            ctx.VictimGhostTriggered = data.GhostProtectionGranted;
            ctx.VictimImmunityApplied = false;

            // 倍率
            ctx.MultiplierAppliedToWinner = data.RewardMultiplier;
            ctx.MultiplierAppliedToLoser = data.PenaltyMultiplier;

            return ctx;
        }
    }
}
