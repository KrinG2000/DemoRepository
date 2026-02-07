// ============================================================
// DuelLog.cs — 统一对决日志
// 架构层级: Debug
// 说明: 每次对决打6条关键日志(Trigger/Validate/ConsumeTicket/
//       Lock/PhaseEvent/ResultApply),格式统一可grep。
//       禁止散落Debug.Log,必须走此统一入口。
// ============================================================

using System.Collections.Generic;
using RacingCardGame.Core;

namespace RacingCardGame.Debugging
{
    public static class DuelLog
    {
        public static bool Enabled = true;
        private static readonly List<string> _logBuffer = new List<string>();

        public static IReadOnlyList<string> LogBuffer => _logBuffer.AsReadOnly();

        /// <summary>
        /// 核心日志方法 — 所有对决日志必须走此入口
        /// 格式: [DUEL][Stage=X][Phase=Y][A=1][D=2] details...
        /// </summary>
        public static void Log(string stage, string phase, int attackerId, int defenderId, string details)
        {
            if (!Enabled) return;
            string entry = $"[DUEL][Stage={stage}][Phase={phase}][A={attackerId}][D={defenderId}] {details}";
            _logBuffer.Add(entry);
        }

        // ---- 1) Trigger: 命中谁 ----
        public static void LogTrigger(int attackerId, int defenderId, string phase)
        {
            Log("Trigger", phase, attackerId, defenderId,
                "Attacker hit defender, initiating subspace duel");
        }

        // ---- 2) Validate: 前置检查 ----
        public static void LogValidate(int attackerId, int defenderId, string phase,
            bool ticketOk, bool immunityOk, bool overheatOk, bool ghostOk, string failReason)
        {
            string status = (ticketOk && immunityOk && overheatOk && ghostOk) ? "PASS" : "FAIL";
            Log("Validate", phase, attackerId, defenderId,
                $"Result={status} Ticket={ticketOk} Immunity={immunityOk} Overheat={overheatOk} Ghost={ghostOk}" +
                (failReason != null ? $" Reason={failReason}" : ""));
        }

        // ---- 3) ConsumeTicket: 门票消耗 ----
        public static void LogConsumeTicket(int attackerId, int defenderId, string phase,
            int ticketId, CardType ticketType, int remainingDarkCards)
        {
            Log("ConsumeTicket", phase, attackerId, defenderId,
                $"TicketId={ticketId} TicketType={ticketType} RemainingDarkCards={remainingDarkCards}");
        }

        // ---- 4) Lock: 最终出牌 ----
        public static void LogLock(int attackerId, int defenderId, string phase,
            CardType aCard, CardType dCard, CardType origACard, CardType origDCard)
        {
            string origInfo = "";
            if (origACard != aCard) origInfo += $" OrigACard={origACard}";
            if (origDCard != dCard) origInfo += $" OrigDCard={origDCard}";
            Log("Lock", phase, attackerId, defenderId,
                $"ACard={aCard} DCard={dCard}{origInfo}");
        }

        // ---- 5) PhaseEvent: 相位特殊事件 ----
        public static void LogPhaseEvent(int attackerId, int defenderId, string phase, string eventDetails)
        {
            Log("PhaseEvent", phase, attackerId, defenderId, eventDetails);
        }

        // ---- 6) ResultApply: 结算应用 ----
        public static void LogResultApply(int attackerId, int defenderId, string phase,
            DuelOutcome outcome, float rewardMul, float penaltyMul, string bannerType)
        {
            Log("ResultApply", phase, attackerId, defenderId,
                $"Outcome={outcome} RewardMul={rewardMul:F2} PenaltyMul={penaltyMul:F2} Banner={bannerType}");
        }

        /// <summary>
        /// 清空日志缓冲区
        /// </summary>
        public static void Clear()
        {
            _logBuffer.Clear();
        }

        /// <summary>
        /// 获取包含指定关键词的日志条目
        /// </summary>
        public static List<string> Filter(string keyword)
        {
            var results = new List<string>();
            foreach (var entry in _logBuffer)
            {
                if (entry.Contains(keyword))
                    results.Add(entry);
            }
            return results;
        }
    }
}
