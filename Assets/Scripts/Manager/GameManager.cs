// ============================================================
// GameManager.cs — 游戏管理器
// 架构层级: Manager (顶层协调层)
// 依赖: Core/*, Card/*, Skill/*, Phase/*, Duel/*
// 说明: 游戏会话的生命周期管理器,负责:
//       - 初始化所有子系统
//       - 协调游戏开局 (相位锁定、发牌)
//       - 管理玩家数据 (技能格、手牌、幽灵保护)
//       - 协调子空间对决的发起与结算
//       - 应用相位特殊效果 (过热计时、幽灵保护追踪)
//       这是整个系统的入口点和协调者。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Skill;
using RacingCardGame.Phase;
using RacingCardGame.Duel;

namespace RacingCardGame.Manager
{
    /// <summary>
    /// 玩家会话数据 — 封装单个玩家在一局中的所有运行时数据
    /// </summary>
    public class PlayerSession
    {
        public int PlayerId;
        public SkillSlotManager SkillSlots;
        public CardManager Cards;
        public GhostProtectionTracker GhostTracker; // 幽灵保护追踪器 (无限火力相位)

        public PlayerSession(int playerId, ITimeProvider timeProvider)
        {
            PlayerId = playerId;
            SkillSlots = new SkillSlotManager(playerId, timeProvider);
            Cards = new CardManager(playerId);
            // GhostTracker在无限火力相位时初始化
        }
    }

    public class GameManager
    {
        /// <summary>
        /// 所有玩家会话
        /// </summary>
        private readonly Dictionary<int, PlayerSession> _players = new Dictionary<int, PlayerSession>();

        /// <summary>
        /// 相位管理器
        /// </summary>
        public PhaseManager PhaseManager { get; private set; }

        /// <summary>
        /// 子空间对决管理器
        /// </summary>
        public SubspaceDuelManager DuelManager { get; private set; }

        /// <summary>
        /// 游戏是否正在进行
        /// </summary>
        public bool IsGameActive { get; private set; }

        /// <summary>
        /// 时间提供者 (用于过热/幽灵保护计时)
        /// </summary>
        public ITimeProvider TimeProvider { get; private set; }

        /// <summary>
        /// 随机数生成器
        /// </summary>
        private readonly Random _random;

        public GameManager() : this(new Random()) { }

        public GameManager(Random random) : this(random, new SystemTimeProvider()) { }

        public GameManager(Random random, ITimeProvider timeProvider)
        {
            _random = random;
            TimeProvider = timeProvider;
            PhaseManager = new PhaseManager(_random);
            DuelManager = new SubspaceDuelManager(PhaseManager, _random);

            // 订阅对决结果事件,应用后处理效果
            GameEvents.OnDuelResolved += HandleDuelResult;
        }

        /// <summary>
        /// 初始化游戏会话
        /// </summary>
        public void InitializeGame(IEnumerable<int> playerIds)
        {
            if (IsGameActive)
                throw new InvalidOperationException("Game is already active. Call EndGame() first.");

            // 重置状态
            _players.Clear();
            PhaseManager.Reset();

            // 创建玩家会话
            foreach (int id in playerIds)
            {
                _players[id] = new PlayerSession(id, TimeProvider);
            }

            // 随机锁定相位
            PhaseBase lockedPhase = PhaseManager.LockRandomPhase();

            // 应用相位特殊初始化
            ApplyPhaseInitialization(lockedPhase);

            // 为每个玩家发初始手牌
            DealInitialCards();

            IsGameActive = true;
            GameEvents.RaiseGameStart();
        }

        /// <summary>
        /// 使用指定相位初始化游戏 (用于测试)
        /// </summary>
        public void InitializeGameWithPhase(IEnumerable<int> playerIds, PhaseType phaseType)
        {
            if (IsGameActive)
                throw new InvalidOperationException("Game is already active. Call EndGame() first.");

            _players.Clear();
            PhaseManager.Reset();

            foreach (int id in playerIds)
            {
                _players[id] = new PlayerSession(id, TimeProvider);
            }

            PhaseBase lockedPhase = PhaseManager.LockPhase(phaseType);
            ApplyPhaseInitialization(lockedPhase);
            DealInitialCards();

            IsGameActive = true;
            GameEvents.RaiseGameStart();
        }

        /// <summary>
        /// 应用相位特殊初始化
        /// </summary>
        private void ApplyPhaseInitialization(PhaseBase phase)
        {
            float chargeMultiplier = phase.GetChargeSpeedMultiplier();

            foreach (var session in _players.Values)
            {
                session.SkillSlots.ChargeSpeedMultiplier = chargeMultiplier;

                // 无限火力相位: 初始化幽灵保护追踪器
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
        /// 发初始手牌 — 每位玩家获得一组明牌和暗牌
        /// </summary>
        private void DealInitialCards()
        {
            foreach (var session in _players.Values)
            {
                // 每位玩家获得: 各一张石头/剪刀/布明牌 + 2张随机暗牌
                var initialCards = new List<Card.Card>
                {
                    new Card.Card(CardType.Rock, false),
                    new Card.Card(CardType.Scissors, false),
                    new Card.Card(CardType.Paper, false),
                    new Card.Card((CardType)_random.Next(3), true),
                    new Card.Card((CardType)_random.Next(3), true)
                };

                session.Cards.DealInitialHand(initialCards);
            }
        }

        /// <summary>
        /// 处理玩家漂移充能
        /// </summary>
        public bool HandleDrift(int playerId, float driftAmount)
        {
            ValidateGameActive();
            var session = GetPlayerSession(playerId);

            bool isFull = session.SkillSlots.AddCharge(driftAmount);
            GameEvents.RaiseDriftCharge(driftAmount);

            return isFull;
        }

        /// <summary>
        /// 尝试发起子空间对决
        /// </summary>
        public DuelResultData TryInitiateDuel(
            int initiatorId, int defenderId,
            CardType initiatorChoice, CardType defenderChoice)
        {
            ValidateGameActive();
            var initiator = GetPlayerSession(initiatorId);
            var defender = GetPlayerSession(defenderId);

            // 验证发起者前置条件
            var (canInitiate, reason) = DuelManager.ValidateDuelConditions(
                initiator.SkillSlots, initiator.Cards);

            if (!canInitiate)
                return null;

            // 验证防守方幽灵保护 (无限火力相位)
            if (defender.GhostTracker != null)
            {
                var (canPull, pullReason) = DuelManager.ValidateDefenderPullable(defender.GhostTracker);
                if (!canPull)
                    return null;
            }

            // 验证出牌合法性
            var (validI, _reasonI) = DuelManager.ValidateCardChoice(initiatorChoice);
            if (!validI)
                return null;

            var (validD, _reasonD) = DuelManager.ValidateCardChoice(defenderChoice);
            if (!validD)
                return null;

            // 记录防守方被拉入 (幽灵保护计数)
            if (defender.GhostTracker != null)
            {
                defender.GhostTracker.RecordPull();
            }

            // 发布对决发起事件
            GameEvents.RaiseDuelInitiated(initiatorId, defenderId);

            // 执行对决
            DuelResultData result = DuelManager.ExecuteDuel(
                initiatorId, defenderId,
                initiator.Cards, initiator.SkillSlots,
                initiatorChoice, defenderChoice);

            return result;
        }

        /// <summary>
        /// 处理对决结果 — 应用相位特殊效果到玩家
        /// </summary>
        private void HandleDuelResult(DuelResultData result)
        {
            if (!_players.ContainsKey(result.InitiatorId) || !_players.ContainsKey(result.DefenderId))
                return;

            var initiator = _players[result.InitiatorId];
            var defender = _players[result.DefenderId];

            // 无限火力相位: 发起者技能过热 (带计时)
            if (result.ActivePhase == PhaseType.InfiniteFirepower && result.OverheatDuration > 0f)
            {
                initiator.SkillSlots.StartOverheat(result.OverheatDuration);
            }
        }

        /// <summary>
        /// 获取玩家会话数据
        /// </summary>
        public PlayerSession GetPlayerSession(int playerId)
        {
            if (!_players.ContainsKey(playerId))
                throw new ArgumentException($"Player {playerId} not found in this game session.");
            return _players[playerId];
        }

        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            IsGameActive = false;
            GameEvents.RaiseGameEnd();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
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
