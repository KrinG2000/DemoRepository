// ============================================================
// DebugHUD.cs — 运行时调试面板
// 架构层级: Debug
// 说明: 屏幕角落显示关键对决状态。可通过bool/F1键开关。
//       不依赖美术资源,纯文本渲染。
//       在Unity中可用OnGUI或UI Toolkit实现。
// ============================================================

using System.Text;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Manager;
using RacingCardGame.UI;

namespace RacingCardGame.Debugging
{
    public class DebugHUD
    {
        /// <summary>
        /// HUD显示开关 (可通过F1切换)
        /// </summary>
        public bool IsVisible { get; set; }

        private readonly GameManager _gameManager;
        private DuelResultContext _lastDuelContext;
        private UIBannerType? _lastBannerType;

        public DebugHUD(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// 记录最近一次对决结果 (DuelUIEventQueue回调时调用)
        /// </summary>
        public void SetLastDuelResult(DuelResultContext ctx, UIBannerType banner)
        {
            _lastDuelContext = ctx;
            _lastBannerType = banner;
        }

        /// <summary>
        /// 切换显示/隐藏
        /// </summary>
        public void Toggle()
        {
            IsVisible = !IsVisible;
        }

        /// <summary>
        /// 渲染单个玩家状态
        /// </summary>
        public string RenderPlayerState(int playerId)
        {
            var sb = new StringBuilder();
            try
            {
                var session = _gameManager.GetPlayerSession(playerId);
                var phase = _gameManager.PhaseManager.ActivePhaseType;

                sb.AppendLine($"--- Player {playerId} ---");
                sb.AppendLine($"Phase: {phase?.ToString() ?? "Standard"}");
                sb.AppendLine($"SkillSlots: {session.SkillSlots.FilledSlots}/{BalanceConfig.Current.MaxSkillSlots}");

                // Hand cards (Slot1/Slot2)
                sb.AppendLine($"HandCards: {session.Cards.Hand.Count}/{BalanceConfig.Current.MaxHandSlots}");
                for (int i = 0; i < session.Cards.Hand.Count; i++)
                    sb.AppendLine($"  Slot{i + 1}: {session.Cards.Hand[i].Type}");

                // Overflow
                var overflow = session.Cards.OverflowCard;
                if (overflow != null)
                    sb.AppendLine($"Overflow: {overflow.Type} ({session.Cards.OverflowRemainingTime:F1}s)");
                else
                    sb.AppendLine("Overflow: None");

                // P1 Immunity
                sb.AppendLine($"P1Immunity: {session.ImmunityRemainingTime:F1}s");

                // Overheat
                sb.AppendLine($"Overheat: {session.SkillSlots.OverheatRemainingTime:F1}s");

                // Ghost
                if (session.GhostTracker != null)
                    sb.AppendLine($"Ghost: {session.GhostTracker.RemainingProtectionTime:F1}s");
                else
                    sb.AppendLine("Ghost: N/A");
            }
            catch (System.Exception)
            {
                sb.AppendLine($"Player {playerId}: Not in session");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 渲染最近一次对决信息
        /// </summary>
        public string RenderLastDuel()
        {
            if (_lastDuelContext == null)
                return "No duel yet.\n";

            var ctx = _lastDuelContext;
            var sb = new StringBuilder();
            sb.AppendLine("--- Last Duel ---");
            sb.AppendLine($"phase: {ctx.Phase}");
            sb.AppendLine($"attackerPlayed: {ctx.AttackerPlayed} (id={ctx.AttackerId})");
            sb.AppendLine($"defenderPlayed: {ctx.DefenderPlayed} (id={ctx.DefenderId})");
            sb.AppendLine($"jester_swapTriggered: {ctx.JesterSwapTriggered}");
            sb.AppendLine($"casino_houseCard: {(ctx.CasinoHouseCard.HasValue ? ctx.CasinoHouseCard.Value.ToString() : "N/A")}");
            sb.AppendLine($"casino_conquerHeavenTriggered: {ctx.CasinoConquerHeavenTriggered}");
            sb.AppendLine($"winner: {(ctx.WinnerId.HasValue ? ctx.WinnerId.Value.ToString() : "Draw")}");
            sb.AppendLine($"loser: {(ctx.LoserId.HasValue ? ctx.LoserId.Value.ToString() : "N/A")}");
            sb.AppendLine($"multiplierAppliedToWinner: {ctx.MultiplierAppliedToWinner:F2}");
            sb.AppendLine($"multiplierAppliedToLoser: {ctx.MultiplierAppliedToLoser:F2}");
            sb.AppendLine($"PickedBannerType: {(_lastBannerType.HasValue ? _lastBannerType.Value.ToString() : "None")}");
            sb.AppendLine($"ConfigVersion: v{BalanceConfig.Current.ConfigVersion}");

            return sb.ToString();
        }

        /// <summary>
        /// 渲染完整HUD (所有信息)
        /// </summary>
        public string Render(params int[] playerIds)
        {
            if (!IsVisible) return "";

            var sb = new StringBuilder();
            sb.AppendLine("========== DEBUG HUD (F1 toggle) ==========");

            foreach (int pid in playerIds)
                sb.Append(RenderPlayerState(pid));

            sb.Append(RenderLastDuel());
            sb.AppendLine("=============================================");
            return sb.ToString();
        }
    }
}
