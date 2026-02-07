// ============================================================
// RaceSession.cs — 赛车主循环
// 架构层级: Runtime (顶层)
// 说明: 1名玩家 + 最多5AI跑完整局。Tick驱动:
//   1) 更新车辆位置
//   2) 检测卡牌拾取
//   3) 处理漂移充能
//   4) AI决策
//   5) 处理对决
//   6) 更新UI事件队列
//   7) 更新DebugHUD
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Manager;
using RacingCardGame.Vehicle;
using RacingCardGame.Charge;
using RacingCardGame.Inventory;
using RacingCardGame.DuelFlow;
using RacingCardGame.Presentation;
using RacingCardGame.Pickup;
using RacingCardGame.AI;
using RacingCardGame.Skill;
using RacingCardGame.UI;
using RacingCardGame.Debugging;

namespace RacingCardGame.Runtime
{
    /// <summary>
    /// 单个赛车手的运行时数据
    /// </summary>
    public class RacerRuntime
    {
        public int PlayerId;
        public ICarController Car;
        public IChargeSystem Charge;
        public ICardInventorySystem Inventory;
        public AIDriver AI; // null for human player
        public bool IsHuman;
    }

    /// <summary>
    /// 赛车主循环 — 完整局管理
    /// </summary>
    public class RaceSession
    {
        public GameManager GameManager { get; private set; }
        public DuelFlowSystem DuelSystem { get; private set; }
        public SimpleDuelPresentation Presentation { get; private set; }
        public DuelUIEventQueue UIEventQueue { get; private set; }
        public CardPickupManager PickupManager { get; private set; }
        public DebugHUD DebugHUD { get; private set; }
        public ITimeProvider TimeProvider { get; private set; }

        public bool IsRaceActive { get; private set; }
        public float RaceTime { get; private set; }
        public int DuelCount { get; private set; }

        private readonly Dictionary<int, RacerRuntime> _racers = new Dictionary<int, RacerRuntime>();
        private readonly List<int> _allPlayerIds = new List<int>();
        private readonly Random _random;
        private SimpleDuelBannerPresenter _bannerPresenter;

        public IReadOnlyDictionary<int, RacerRuntime> Racers => _racers;

        public RaceSession(Random random = null, ITimeProvider timeProvider = null)
        {
            _random = random ?? new Random();
            TimeProvider = timeProvider ?? new ManualTimeProvider();
        }

        /// <summary>
        /// 初始化比赛: 创建1名玩家 + aiCount名AI
        /// </summary>
        public void Initialize(int humanPlayerId, int aiCount, PhaseType? forcePhase = null)
        {
            var cfg = BalanceConfig.Current;

            // 1. 创建 GameManager + 玩家会话
            GameManager = new GameManager(_random, TimeProvider);
            _allPlayerIds.Clear();
            _allPlayerIds.Add(humanPlayerId);
            for (int i = 1; i <= aiCount; i++)
                _allPlayerIds.Add(humanPlayerId + i);

            if (forcePhase.HasValue)
                GameManager.InitializeGameWithPhase(_allPlayerIds, forcePhase.Value);
            else
                GameManager.InitializeGame(_allPlayerIds);

            // 2. 创建 UI 组件
            _bannerPresenter = new SimpleDuelBannerPresenter();
            UIEventQueue = new DuelUIEventQueue(_bannerPresenter, TimeProvider);

            // 3. 创建 Presentation
            Presentation = new SimpleDuelPresentation(_random);

            // 4. 创建每个赛车手的运行时
            var cars = new Dictionary<int, ICarController>();
            var charges = new Dictionary<int, IChargeSystem>();
            var inventories = new Dictionary<int, ICardInventorySystem>();
            var humanIds = new HashSet<int> { humanPlayerId };

            float spacing = 10f;
            int idx = 0;
            foreach (int pid in _allPlayerIds)
            {
                var session = GameManager.GetPlayerSession(pid);
                float startPos = idx * spacing;

                var car = new CarController(pid, TimeProvider, startPos);
                var charge = new ChargeSystem(session.SkillSlots);
                var inventory = new CardInventorySystem(session.Cards, _random);

                var racer = new RacerRuntime
                {
                    PlayerId = pid,
                    Car = car,
                    Charge = charge,
                    Inventory = inventory,
                    IsHuman = (pid == humanPlayerId),
                    AI = null
                };

                _racers[pid] = racer;
                cars[pid] = car;
                charges[pid] = charge;
                inventories[pid] = inventory;
                idx++;
            }

            // 5. 创建 DuelFlowSystem
            DuelSystem = new DuelFlowSystem(
                GameManager, Presentation, UIEventQueue,
                cars, charges, inventories, humanIds);

            // 6. 创建 AI Drivers
            foreach (int pid in _allPlayerIds)
            {
                if (pid == humanPlayerId) continue;
                var racer = _racers[pid];
                racer.AI = new AIDriver(
                    pid, racer.Car, racer.Charge, DuelSystem,
                    _random, GetCarPosition);
            }

            // 7. 创建道具箱
            PickupManager = new CardPickupManager();
            PickupManager.GenerateZones(cfg.TrackLength, cfg.CardPickupCount, 5f, _random);

            // 8. 创建 DebugHUD
            DebugHUD = new DebugHUD(GameManager);
            DebugHUD.IsVisible = true;

            // Hook banner events to DebugHUD
            UIEventQueue.OnBannerShown += (bannerType) =>
            {
                if (DuelSystem.LastResultContext != null)
                    DebugHUD.SetLastDuelResult(DuelSystem.LastResultContext, bannerType);
            };

            IsRaceActive = true;
            RaceTime = 0f;
            DuelCount = 0;

            // Subscribe to count duels
            GameEvents.OnDuelResolved += OnDuelResolved;
        }

        private void OnDuelResolved(DuelResultData data)
        {
            DuelCount++;
        }

        /// <summary>
        /// 主循环Tick — 每帧调用
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsRaceActive) return;

            RaceTime += deltaTime;
            if (TimeProvider is ManualTimeProvider mtp)
                mtp.Advance(deltaTime);

            // 1. 更新车辆
            foreach (var racer in _racers.Values)
                racer.Car.Tick(deltaTime);

            // 2. 检测卡牌拾取
            foreach (var racer in _racers.Values)
            {
                float pos = racer.Car.GetPosition();
                PickupManager.CheckPickups(racer.PlayerId, pos, racer.Inventory);
            }

            // 3. 漂移充能 (人类玩家)
            foreach (var racer in _racers.Values)
            {
                if (racer.IsHuman)
                    racer.Charge.Tick(deltaTime, racer.Car.IsDrifting);
            }

            // 4. AI决策
            foreach (var racer in _racers.Values)
            {
                if (racer.AI != null)
                    racer.AI.Tick(deltaTime, _allPlayerIds);
            }

            // 5. UI事件队列
            UIEventQueue.Tick();

            // 6. DuelSystem Tick
            DuelSystem.Tick(deltaTime);
        }

        /// <summary>
        /// 人类玩家按E发起对决 (射线检测最近前方目标)
        /// </summary>
        public DuelFailReason PlayerTryDuel(int humanPlayerId)
        {
            if (!_racers.ContainsKey(humanPlayerId)) return DuelFailReason.InvalidTarget;

            var humanCar = _racers[humanPlayerId].Car;
            float myPos = humanCar.GetPosition();
            float range = BalanceConfig.Current.DuelRaycastRange;

            // 简化射线: 找前方最近的车
            int bestTarget = -1;
            float bestDist = float.MaxValue;

            foreach (var racer in _racers.Values)
            {
                if (racer.PlayerId == humanPlayerId) continue;
                float otherPos = racer.Car.GetPosition();
                float dist = otherPos - myPos; // 正方向=前方
                if (dist > 0 && dist <= range && dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = racer.PlayerId;
                }
            }

            if (bestTarget < 0)
                return DuelFailReason.InvalidTarget;

            return DuelSystem.TryStartDuel(humanPlayerId, bestTarget);
        }

        /// <summary>
        /// 人类玩家设置漂移状态
        /// </summary>
        public void PlayerSetDrift(int humanPlayerId, bool drifting)
        {
            if (_racers.ContainsKey(humanPlayerId) && _racers[humanPlayerId].Car is CarController cc)
                cc.SetDrifting(drifting);
        }

        /// <summary>
        /// 人类玩家在overflow时替换slot
        /// </summary>
        public void PlayerReplaceSlot(int humanPlayerId, int slotIndex)
        {
            if (_racers.ContainsKey(humanPlayerId))
                _racers[humanPlayerId].Inventory.ReplaceSlot(slotIndex);
        }

        /// <summary>
        /// 获取车辆位置 (供AI使用)
        /// </summary>
        public float GetCarPosition(int playerId)
        {
            if (_racers.ContainsKey(playerId))
                return _racers[playerId].Car.GetPosition();
            return 0f;
        }

        /// <summary>
        /// 检查比赛是否结束 (有车到达终点)
        /// </summary>
        public int? CheckFinish()
        {
            float trackLen = BalanceConfig.Current.TrackLength;
            foreach (var racer in _racers.Values)
            {
                if (racer.Car.GetPosition() >= trackLen)
                    return racer.PlayerId;
            }
            return null;
        }

        /// <summary>
        /// 获取排名 (按位置降序)
        /// </summary>
        public List<int> GetRankings()
        {
            var ranked = _racers.Values.OrderByDescending(r => r.Car.GetPosition()).ToList();
            return ranked.Select(r => r.PlayerId).ToList();
        }

        /// <summary>
        /// 渲染Debug信息
        /// </summary>
        public string RenderDebugState()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"===== Race t={RaceTime:F1}s Duels={DuelCount} =====");

            foreach (var racer in _racers.Values)
            {
                string type = racer.IsHuman ? "HUMAN" : "AI";
                var car = racer.Car;
                sb.AppendLine($"  P{racer.PlayerId}[{type}] pos={car.GetPosition():F0}m " +
                    $"spd={car.GetSpeed():F1} drift={car.IsDrifting} " +
                    $"slots={racer.Charge.SkillSlots}/{racer.Charge.MaxSkillSlots} " +
                    $"s1={racer.Inventory.Slot1Card?.ToString() ?? "-"} " +
                    $"s2={racer.Inventory.Slot2Card?.ToString() ?? "-"} " +
                    $"ticket={racer.Inventory.TicketCount} " +
                    $"overheat={racer.Charge.IsOverheated} " +
                    $"effects={car.ActiveEffects.Count}");
            }

            if (DuelSystem.LastResultContext != null)
            {
                var ctx = DuelSystem.LastResultContext;
                sb.AppendLine($"  LastDuel: A={ctx.AttackerId} D={ctx.DefenderId} " +
                    $"W={ctx.WinnerId?.ToString() ?? "Draw"} phase={ctx.Phase}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 结束比赛
        /// </summary>
        public void EndRace()
        {
            IsRaceActive = false;
            GameEvents.OnDuelResolved -= OnDuelResolved;
            GameManager.EndGame();
        }
    }
}
