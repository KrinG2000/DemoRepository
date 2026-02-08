// ============================================================
// GameManager.cs — 游戏管理器
// 架构层级: Manager (顶层协调层)
// 依赖: Core/*, Card/*, Skill/*, Phase/*, Duel/*, Config/*, Debug/*
// 说明: P1 Immunity (7s免疫), Overflow清空, DuelLog集成
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Skill;
using RacingCardGame.Phase;
using RacingCardGame.Duel;
using RacingCardGame.Config;
using RacingCardGame.Debugging;

namespace RacingCardGame.Manager
{
    public class PlayerSession
    {
        public int PlayerId;
        public SkillSlotManager SkillSlots;
        public CardManager Cards;
        public GhostProtectionTracker GhostTracker;

        // P1 Immunity: 被拉入子空间后7s内免疫再次被拉入
        private float _immunityEndTime = -1f;
        private readonly ITimeProvider _timeProvider;

        public bool IsImmune
        {
            get
            {
                if (_immunityEndTime < 0f || _timeProvider == null)
                    return false;
                if (_timeProvider.CurrentTime >= _immunityEndTime)
                {
                    _immunityEndTime = -1f;
                    return false;
                }
                return true;
            }
        }

        public float ImmunityRemainingTime
        {
            get
            {
                if (_immunityEndTime < 0f || _timeProvider == null)
                    return 0f;
                float remaining = _immunityEndTime - _timeProvider.CurrentTime;
                return remaining > 0f ? remaining : 0f;
            }
        }

        public void SetImmunity(float duration)
        {
            if (_timeProvider != null)
                _immunityEndTime = _timeProvider.CurrentTime + duration;
        }

        public PlayerSession(int playerId, ITimeProvider timeProvider)
        {
            PlayerId = playerId;
            _timeProvider = timeProvider;
            SkillSlots = new SkillSlotManager(playerId, timeProvider);
            Cards = new CardManager(playerId, timeProvider);
        }
    }

    public class GameManager
    {
        private readonly Dictionary<int, PlayerSession> _players = new Dictionary<int, PlayerSession>();

        public PhaseManager PhaseManager { get; private set; }
        public SubspaceDuelManager DuelManager { get; private set; }
        public bool IsGameActive { get; private set; }
        public ITimeProvider TimeProvider { get; private set; }

        private readonly Random _random;

        public GameManager() : this(new Random()) { }

        public GameManager(Random random) : this(random, new SystemTimeProvider()) { }

        public GameManager(Random random, ITimeProvider timeProvider)
        {
            _random = random;
            TimeProvider = timeProvider;
            PhaseManager = new PhaseManager(_random);
            DuelManager = new SubspaceDuelManager(PhaseManager, _random);

            GameEvents.OnDuelResolved += HandleDuelResult;
        }

        public void InitializeGame(IEnumerable<int> playerIds)
        {
            if (IsGameActive)
                throw new InvalidOperationException("Game is already active. Call EndGame() first.");

            _players.Clear();
            PhaseManager.Reset();

            foreach (int id in playerIds)
                _players[id] = new PlayerSession(id, TimeProvider);

            PhaseBase lockedPhase = PhaseManager.LockRandomPhase();
            ApplyPhaseInitialization(lockedPhase);
            DealInitialCards();

            IsGameActive = true;
            GameEvents.RaiseGameStart();
        }

        public void InitializeGameWithPhase(IEnumerable<int> playerIds, PhaseType phaseType)
        {
            if (IsGameActive)
                throw new InvalidOperationException("Game is already active. Call EndGame() first.");

            _players.Clear();
            PhaseManager.Reset();

            foreach (int id in playerIds)
                _players[id] = new PlayerSession(id, TimeProvider);

            PhaseBase lockedPhase = PhaseManager.LockPhase(phaseType);
            ApplyPhaseInitialization(lockedPhase);
            DealInitialCards();

            IsGameActive = true;
            GameEvents.RaiseGameStart();
        }

        private void ApplyPhaseInitialization(PhaseBase phase)
        {
            float chargeMultiplier = phase.GetChargeSpeedMultiplier();

            foreach (var session in _players.Values)
            {
                session.SkillSlots.ChargeSpeedMultiplier = chargeMultiplier;

                if (phase.Type == PhaseType.InfiniteFirepower)
                {
                    session.GhostTracker = new GhostProtectionTracker(
                        session.PlayerId,
                        TimeProvider,
                        InfiniteFirepowerPhase.GhostProtectionPullThreshold,
                        InfiniteFirepowerPhase.GhostProtectionTimeWindow,
                        InfiniteFirepowerPhase.GhostProtectionDuration);
                }
            }
        }

        /// <summary>
        /// 发初始手牌 — 2张明牌 + 2张暗牌 (MaxHandSlots=2)
        /// </summary>
        private void DealInitialCards()
        {
            foreach (var session in _players.Values)
            {
                var initialCards = new List<Card.Card>
                {
                    new Card.Card(CardType.Rock, false),
                    new Card.Card(CardType.Scissors, false),
                    new Card.Card((CardType)_random.Next(3), true),
                    new Card.Card((CardType)_random.Next(3), true)
                };

                session.Cards.DealInitialHand(initialCards);
            }
        }

        public bool HandleDrift(int playerId, float driftAmount)
        {
            ValidateGameActive();
            var session = GetPlayerSession(playerId);
            bool isFull = session.SkillSlots.AddCharge(driftAmount);
            GameEvents.RaiseDriftCharge(driftAmount);
            return isFull;
        }

        public DuelResultData TryInitiateDuel(
            int initiatorId, int defenderId,
            CardType initiatorChoice, CardType defenderChoice)
        {
            ValidateGameActive();
            var initiator = GetPlayerSession(initiatorId);
            var defender = GetPlayerSession(defenderId);
            string phaseName = PhaseManager.ActivePhaseType?.ToString() ?? "Standard";

            // Log: Trigger
            DuelLog.LogTrigger(initiatorId, defenderId, phaseName);

            // Validate
            bool ticketOk = true, immunityOk = true, overheatOk = true, ghostOk = true;
            string failReason = null;

            var (canInitiate, reason) = DuelManager.ValidateDuelConditions(
                initiator.SkillSlots, initiator.Cards);
            if (!canInitiate)
            {
                ticketOk = initiator.Cards.HasTicket;
                overheatOk = !initiator.SkillSlots.IsOverheated;
                failReason = reason;
                DuelLog.LogValidate(initiatorId, defenderId, phaseName,
                    ticketOk, immunityOk, overheatOk, ghostOk, failReason);
                return null;
            }

            // P1 Immunity check
            if (defender.IsImmune)
            {
                immunityOk = false;
                failReason = "Defender has P1 immunity";
                DuelLog.LogValidate(initiatorId, defenderId, phaseName,
                    ticketOk, immunityOk, overheatOk, ghostOk, failReason);
                return null;
            }

            // Ghost check
            if (defender.GhostTracker != null)
            {
                var (canPull, pullReason) = DuelManager.ValidateDefenderPullable(defender.GhostTracker);
                if (!canPull)
                {
                    ghostOk = false;
                    failReason = pullReason;
                    DuelLog.LogValidate(initiatorId, defenderId, phaseName,
                        ticketOk, immunityOk, overheatOk, ghostOk, failReason);
                    return null;
                }
            }

            DuelLog.LogValidate(initiatorId, defenderId, phaseName,
                ticketOk, immunityOk, overheatOk, ghostOk, null);

            // Ghost pull recording
            if (defender.GhostTracker != null)
                defender.GhostTracker.RecordPull();

            // Clear Overflow on subspace entry
            initiator.Cards.ClearOverflow();
            defender.Cards.ClearOverflow();

            // Log: ConsumeTicket (peek before consuming)
            var ticketCard = initiator.Cards.PeekOldestDarkCard();
            int ticketId = ticketCard != null ? ticketCard.Id : -1;
            CardType ticketType = ticketCard != null ? ticketCard.Type : CardType.Rock;

            GameEvents.RaiseDuelInitiated(initiatorId, defenderId);

            DuelResultData result = DuelManager.ExecuteDuel(
                initiatorId, defenderId,
                initiator.Cards, initiator.SkillSlots,
                initiatorChoice, defenderChoice);

            DuelLog.LogConsumeTicket(initiatorId, defenderId, phaseName,
                ticketId, ticketType, initiator.Cards.DarkCardCount);

            // Log: Lock
            DuelLog.LogLock(initiatorId, defenderId, phaseName,
                result.InitiatorCard, result.DefenderCard,
                result.OriginalInitiatorCard, result.OriginalDefenderCard);

            // Log: PhaseEvent
            string phaseEvent = "None";
            if (result.CardsSwapped) phaseEvent = "Swap=true";
            else if (result.IsShengTianBanZi) phaseEvent = "ConquerHeaven=true";
            else if (result.ScissorsConverted) phaseEvent = "ScissorsConverted=true";
            else if (result.DestinyMatch == DestinyMatchType.WinnerMatched) phaseEvent = $"HouseCard={result.DestinyCard} DestinyCrit";
            else if (result.DestinyMatch == DestinyMatchType.DefenderMatched) phaseEvent = $"HouseCard={result.DestinyCard} DestinyCounter";
            else if (result.DestinyMatch == DestinyMatchType.LoserMatched) phaseEvent = $"HouseCard={result.DestinyCard} BadLuck";
            DuelLog.LogPhaseEvent(initiatorId, defenderId, phaseName, phaseEvent);

            // Log: ResultApply
            string bannerType = "NormalWin";
            if (result.IsShengTianBanZi) bannerType = "ConquerHeaven";
            else if (result.CardsSwapped) bannerType = "JesterSwap";
            else if (result.Outcome == DuelOutcome.Draw) bannerType = "Draw";
            DuelLog.LogResultApply(initiatorId, defenderId, phaseName,
                result.Outcome, result.RewardMultiplier, result.PenaltyMultiplier, bannerType);

            // Apply P1 Immunity to defender
            defender.SetImmunity(BalanceConfig.Current.P1_ImmunityDuration);

            return result;
        }

        private void HandleDuelResult(DuelResultData result)
        {
            if (!_players.ContainsKey(result.InitiatorId) || !_players.ContainsKey(result.DefenderId))
                return;

            var initiator = _players[result.InitiatorId];

            if (result.ActivePhase == PhaseType.InfiniteFirepower && result.OverheatDuration > 0f)
                initiator.SkillSlots.StartOverheat(result.OverheatDuration);
        }

        public PlayerSession GetPlayerSession(int playerId)
        {
            if (!_players.ContainsKey(playerId))
                throw new ArgumentException($"Player {playerId} not found in this game session.");
            return _players[playerId];
        }

        public void EndGame()
        {
            IsGameActive = false;
            GameEvents.RaiseGameEnd();
        }

        public void Dispose()
        {
            GameEvents.OnDuelResolved -= HandleDuelResult;
            GameEvents.ClearAll();
        }

        private void ValidateGameActive()
        {
            if (!IsGameActive)
                throw new InvalidOperationException("No active game session.");
        }
    }
}
