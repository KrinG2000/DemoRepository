// ============================================================
// DuelFlowSystem.cs — 对决流程系统实现
// 架构层级: DuelFlow
// 说明: 完整对决流程编排:
//   Validate → ConsumeTicket → Enter → Pick → Lock →
//   PhaseEvent → Judge → Apply → Exit
//   使用现有 GameManager/SubspaceDuelManager 做核心判定,
//   新增: 车辆控制冻结/恢复, 状态效果施加, 全局锁, Banner入队。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Manager;
using RacingCardGame.Vehicle;
using RacingCardGame.Charge;
using RacingCardGame.Inventory;
using RacingCardGame.Presentation;
using RacingCardGame.UI;
using RacingCardGame.Debugging;

namespace RacingCardGame.DuelFlow
{
    public class DuelFlowSystem : IDuelSystem
    {
        public bool IsDuelActive { get; private set; }
        public int CurrentAttackerId { get; private set; } = -1;
        public int CurrentDefenderId { get; private set; } = -1;

        /// <summary>
        /// 最近一次对决的结果上下文 (供DebugHUD使用)
        /// </summary>
        public DuelResultContext LastResultContext { get; private set; }
        public DuelResultData LastResultData { get; private set; }

        private readonly GameManager _gameManager;
        private readonly IDuelPresentation _presentation;
        private readonly DuelUIEventQueue _uiEventQueue;
        private readonly Dictionary<int, ICarController> _cars;
        private readonly Dictionary<int, IChargeSystem> _charges;
        private readonly Dictionary<int, ICardInventorySystem> _inventories;
        private readonly HashSet<int> _humanPlayerIds;

        public DuelFlowSystem(
            GameManager gameManager,
            IDuelPresentation presentation,
            DuelUIEventQueue uiEventQueue,
            Dictionary<int, ICarController> cars,
            Dictionary<int, IChargeSystem> charges,
            Dictionary<int, ICardInventorySystem> inventories,
            HashSet<int> humanPlayerIds)
        {
            _gameManager = gameManager;
            _presentation = presentation;
            _uiEventQueue = uiEventQueue;
            _cars = cars;
            _charges = charges;
            _inventories = inventories;
            _humanPlayerIds = humanPlayerIds;
        }

        public DuelFailReason TryStartDuel(int attackerId, int defenderId)
        {
            // F1: 全局锁 — 同一时刻只能有一场对决
            if (IsDuelActive)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Duel Busy");
                return DuelFailReason.DuelBusy;
            }

            // F7: 攻击自己忽略
            if (attackerId == defenderId)
                return DuelFailReason.InvalidTarget;

            var attackerSession = _gameManager.GetPlayerSession(attackerId);
            var defenderSession = _gameManager.GetPlayerSession(defenderId);

            // Validate 1: 技能格满
            if (!attackerSession.SkillSlots.IsFullyCharged)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Skill Not Ready");
                return DuelFailReason.SkillNotReady;
            }

            // Validate 2: 有门票
            if (!attackerSession.Cards.HasTicket)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Need Ticket");
                return DuelFailReason.NeedTicket;
            }

            // Validate 3: 发起者不在overheat
            if (attackerSession.SkillSlots.IsOverheated)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Weapon Overheat");
                return DuelFailReason.WeaponOverheat;
            }

            // Validate 4: 防守方不在P1免疫
            if (defenderSession.IsImmune)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Target Immune");
                return DuelFailReason.TargetImmune;
            }

            // Validate 5: 防守方不在ghost保护
            if (defenderSession.GhostTracker != null && defenderSession.GhostTracker.IsProtected)
            {
                GameEvents.RaiseDuelValidationFailed(attackerId, "Target Ghost Protected");
                return DuelFailReason.TargetGhostProtected;
            }

            // ---- 验证通过, 执行对决流程 ----
            IsDuelActive = true;
            CurrentAttackerId = attackerId;
            CurrentDefenderId = defenderId;

            ExecuteDuelFlow(attackerId, defenderId, attackerSession, defenderSession);

            return DuelFailReason.None;
        }

        private void ExecuteDuelFlow(
            int attackerId, int defenderId,
            PlayerSession attackerSession, PlayerSession defenderSession)
        {
            var cfg = BalanceConfig.Current;

            // ENTER: 冻结车辆控制
            if (_cars.ContainsKey(attackerId))
                _cars[attackerId].SetControlEnabled(false);
            if (_cars.ContainsKey(defenderId))
                _cars[defenderId].SetControlEnabled(false);

            _presentation.EnterSubspace(attackerId, defenderId);
            GameEvents.RaiseSubspaceEntered(attackerId, defenderId);

            // Note: Ghost pull recording is handled by GameManager.TryInitiateDuel
            // to avoid double-counting. Do NOT call RecordPull() here.

            // Clear overflow on subspace entry
            attackerSession.Cards.ClearOverflow();
            defenderSession.Cards.ClearOverflow();

            // PICK: 获取双方可选牌
            var attackerSlots = GetSlotCards(attackerSession);
            var defenderSlots = GetSlotCards(defenderSession);

            bool attackerHuman = _humanPlayerIds.Contains(attackerId);
            bool defenderHuman = _humanPlayerIds.Contains(defenderId);

            CardPickResult pick = _presentation.RequestCardPick(
                attackerSlots, defenderSlots,
                cfg.DuelPickTimeLimit, attackerHuman, defenderHuman);

            CardType attackerChoice = pick.AttackerChoice;
            CardType defenderChoice = pick.DefenderChoice;

            // LOCK + PhaseEvent + Judge: 调用 GameManager.TryInitiateDuel
            DuelResultData result = _gameManager.TryInitiateDuel(
                attackerId, defenderId,
                attackerChoice, defenderChoice);

            if (result != null)
            {
                LastResultData = result;

                // APPLY: 施加状态效果到车辆
                ApplyDuelEffects(result, cfg);

                // 转换为UI上下文
                var ctx = DuelResultAdapter.Convert(result);
                ctx.VictimImmunityApplied = true; // defender gets P1 immunity
                LastResultContext = ctx;

                // Banner入队 (Exit后展示)
                _uiEventQueue.Enqueue(ctx);
            }

            // EXIT: 恢复车辆控制
            _presentation.ExitSubspace();
            GameEvents.RaiseSubspaceExited(attackerId, defenderId);

            if (_cars.ContainsKey(attackerId))
                _cars[attackerId].SetControlEnabled(true);
            if (_cars.ContainsKey(defenderId))
                _cars[defenderId].SetControlEnabled(true);

            IsDuelActive = false;
            CurrentAttackerId = -1;
            CurrentDefenderId = -1;
        }

        /// <summary>
        /// 根据对决结果施加状态效果
        /// </summary>
        private void ApplyDuelEffects(DuelResultData result, BalanceConfig cfg)
        {
            if (result.Outcome == DuelOutcome.Draw)
                return; // 平局无效果

            int winnerId, loserId;
            CardType winnerCard;

            if (result.Outcome == DuelOutcome.Win)
            {
                winnerId = result.InitiatorId;
                loserId = result.DefenderId;
                winnerCard = result.InitiatorCard;
            }
            else
            {
                winnerId = result.DefenderId;
                loserId = result.InitiatorId;
                winnerCard = result.DefenderCard;
            }

            // ---- 胜者奖励 ----
            if (_cars.ContainsKey(winnerId))
            {
                var winnerCar = _cars[winnerId];
                switch (winnerCard)
                {
                    case CardType.Scissors:
                        winnerCar.ApplyStatusEffect(StatusEffect.CreateSpeedMultiplier(
                            cfg.ScissorsWinSpeedMultiplier * result.RewardMultiplier,
                            cfg.ScissorsWinDuration, loserId));
                        break;
                    case CardType.Paper:
                        winnerCar.ApplyStatusEffect(StatusEffect.CreateSpeedMultiplier(
                            cfg.PaperWinSpeedMultiplier * result.RewardMultiplier,
                            cfg.PaperWinDuration, loserId));
                        break;
                    case CardType.Rock:
                        winnerCar.ApplyStatusEffect(StatusEffect.CreateSuperArmor(
                            cfg.RockWinSuperArmorDuration * result.RewardMultiplier,
                            loserId));
                        break;
                }
            }

            // ---- 败者惩罚 ----
            if (_cars.ContainsKey(loserId))
            {
                var loserCar = _cars[loserId];
                // PenaltyMultiplier already includes Counter (1.5x) from phase layer
                // Do NOT re-multiply here to avoid double-apply
                float penaltyMul = result.PenaltyMultiplier;

                switch (winnerCard)
                {
                    case CardType.Scissors:
                        // 打滑 + 限速
                        loserCar.ApplyStatusEffect(StatusEffect.CreateLowFriction(
                            cfg.ScissorsLoseFrictionMultiplier,
                            cfg.ScissorsLoseFrictionDuration * penaltyMul, winnerId));
                        loserCar.ApplyStatusEffect(StatusEffect.CreateSpeedCap(
                            cfg.ScissorsLoseSpeedCap,
                            cfg.ScissorsLoseCapDuration * penaltyMul, winnerId));
                        break;
                    case CardType.Paper:
                        // 微减速
                        loserCar.ApplyStatusEffect(StatusEffect.CreateMinorSlow(
                            cfg.PaperLoseSlowMultiplier,
                            cfg.PaperLoseSlowDuration * penaltyMul, winnerId));
                        break;
                    case CardType.Rock:
                        // 石头赢: 无惩罚
                        break;
                }
            }
        }

        private CardType[] GetSlotCards(PlayerSession session)
        {
            var hand = session.Cards.Hand;
            var cards = new CardType[hand.Count];
            for (int i = 0; i < hand.Count; i++)
                cards[i] = hand[i].Type;
            return cards;
        }

        public void Tick(float deltaTime)
        {
            // MVP: 对决流程是同步的, 无异步Tick需求
            // 未来可扩展为异步选牌倒计时
        }
    }
}
