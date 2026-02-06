// ============================================================
// CoreSystemTests.cs — 核心系统单元测试
// 架构层级: Tests
// 说明: 覆盖技能格、门票、子空间对决、相位系统的核心逻辑。
//       使用纯C#测试 (不依赖Unity Test Runner),
//       可在任何C#环境中运行验证。
//       迁移到Unity后可适配NUnit/Unity Test Framework。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Skill;
using RacingCardGame.Phase;
using RacingCardGame.Duel;
using RacingCardGame.Manager;

namespace RacingCardGame.Tests
{
    /// <summary>
    /// 简易测试框架 — 用于在无Unity环境下验证逻辑
    /// </summary>
    public static class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;
        private static readonly List<string> _failures = new List<string>();

        public static void Assert(bool condition, string testName)
        {
            if (condition)
            {
                _passed++;
                Console.WriteLine($"  [PASS] {testName}");
            }
            else
            {
                _failed++;
                _failures.Add(testName);
                Console.WriteLine($"  [FAIL] {testName}");
            }
        }

        public static void AssertEqual<T>(T expected, T actual, string testName)
        {
            Assert(EqualityComparer<T>.Default.Equals(expected, actual),
                $"{testName} (expected: {expected}, actual: {actual})");
        }

        public static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine($"========================================");
            Console.WriteLine($"  Test Results: {_passed} passed, {_failed} failed");
            Console.WriteLine($"========================================");
            if (_failures.Count > 0)
            {
                Console.WriteLine("  Failed tests:");
                foreach (var f in _failures)
                    Console.WriteLine($"    - {f}");
            }
        }

        public static void Reset()
        {
            _passed = 0;
            _failed = 0;
            _failures.Clear();
        }

        public static int FailedCount => _failed;
    }

    public static class CoreSystemTests
    {
        public static void RunAll()
        {
            TestRunner.Reset();
            Card.Card.ResetIdCounter();
            GameEvents.ClearAll();

            Console.WriteLine("=== SkillSlotManager Tests ===");
            TestSkillSlotCharging();
            TestSkillSlotFullCharge();
            TestSkillSlotActivation();
            TestSkillSlotOverheat();
            TestSkillSlotChargeSpeedMultiplier();

            Console.WriteLine("\n=== CardManager Tests ===");
            TestCardManagerBasicOperations();
            TestTicketConsumption_FIFO();
            TestTicketConsumption_Empty();

            Console.WriteLine("\n=== PhaseManager Tests ===");
            TestPhaseManagerLock();
            TestPhaseManagerMutualExclusion();
            TestPhaseManagerReset();

            Console.WriteLine("\n=== CeasefirePhase Tests ===");
            TestCeasefirePhase_ScissorsDisabled();
            TestCeasefirePhase_RockBeatsPaper();

            Console.WriteLine("\n=== JokerPhase Tests ===");
            TestJokerPhase_SwapTriggered();
            TestJokerPhase_NoSwap();

            Console.WriteLine("\n=== DestinyGambitPhase Tests ===");
            TestDestinyGambit_CriticalReward();
            TestDestinyGambit_CounterPenalty();

            Console.WriteLine("\n=== InfiniteFirepowerPhase Tests ===");
            TestInfiniteFirepower_ChargeSpeed();
            TestInfiniteFirepower_GhostProtection();

            Console.WriteLine("\n=== SubspaceDuelManager Tests ===");
            TestDuelExecution_BasicWin();
            TestDuelExecution_Draw();
            TestDuelValidation_NoTicket();
            TestDuelValidation_NotFullyCharged();

            Console.WriteLine("\n=== GameManager Integration Tests ===");
            TestGameManagerLifecycle();
            TestFullDuelFlow();

            TestRunner.PrintSummary();
        }

        // ---- SkillSlotManager Tests ----

        static void TestSkillSlotCharging()
        {
            var skill = new SkillSlotManager(1);
            skill.AddCharge(50f);
            TestRunner.AssertEqual(0, skill.FilledSlots, "SkillSlot: 50 charge = 0 filled slots");
            TestRunner.Assert(skill.CurrentCharge == 50f, "SkillSlot: current charge = 50");
        }

        static void TestSkillSlotFullCharge()
        {
            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f); // 3 * 100 = 300, should fill all 3 slots
            TestRunner.AssertEqual(3, skill.FilledSlots, "SkillSlot: 300 charge fills all 3 slots");
            TestRunner.Assert(skill.IsFullyCharged, "SkillSlot: IsFullyCharged = true");
        }

        static void TestSkillSlotActivation()
        {
            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(activated, "SkillSlot: activation succeeds when full");
            TestRunner.AssertEqual(0, skill.FilledSlots, "SkillSlot: slots reset to 0 after activation");

            bool activatedAgain = skill.TryActivateSkill();
            TestRunner.Assert(!activatedAgain, "SkillSlot: activation fails when not full");
        }

        static void TestSkillSlotOverheat()
        {
            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            skill.IsOverheated = true;
            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(!activated, "SkillSlot: activation fails when overheated");
        }

        static void TestSkillSlotChargeSpeedMultiplier()
        {
            var skill = new SkillSlotManager(1);
            skill.ChargeSpeedMultiplier = 2.0f;
            skill.AddCharge(50f); // effective = 100f -> fills 1 slot
            TestRunner.AssertEqual(1, skill.FilledSlots, "SkillSlot: 2x multiplier doubles charge");
        }

        // ---- CardManager Tests ----

        static void TestCardManagerBasicOperations()
        {
            GameEvents.ClearAll();
            var cards = new CardManager(1);
            cards.AddToHand(new Card.Card(CardType.Rock, false));
            cards.AddToHand(new Card.Card(CardType.Scissors, false));

            TestRunner.AssertEqual(2, cards.Hand.Count, "CardManager: hand has 2 cards");
            TestRunner.Assert(cards.HasCardType(CardType.Rock), "CardManager: has Rock");

            var played = cards.PlayCard(CardType.Rock);
            TestRunner.Assert(played != null, "CardManager: PlayCard returns card");
            TestRunner.AssertEqual(1, cards.Hand.Count, "CardManager: hand has 1 card after play");
        }

        static void TestTicketConsumption_FIFO()
        {
            GameEvents.ClearAll();
            var cards = new CardManager(1);
            var dark1 = new Card.Card(CardType.Rock, true, 100);
            var dark2 = new Card.Card(CardType.Paper, true, 200);
            var dark3 = new Card.Card(CardType.Scissors, true, 300);

            cards.AddDarkCard(dark1);
            cards.AddDarkCard(dark2);
            cards.AddDarkCard(dark3);

            TestRunner.AssertEqual(3, cards.DarkCardCount, "Ticket: 3 dark cards initially");

            var consumed = cards.ConsumeTicket();
            TestRunner.Assert(consumed != null, "Ticket: consumed card is not null");
            TestRunner.AssertEqual(dark1.Id, consumed.Id, "Ticket: oldest (FIFO) card consumed first");
            TestRunner.AssertEqual(2, cards.DarkCardCount, "Ticket: 2 dark cards remaining");

            var consumed2 = cards.ConsumeTicket();
            TestRunner.AssertEqual(dark2.Id, consumed2.Id, "Ticket: second oldest consumed next");
        }

        static void TestTicketConsumption_Empty()
        {
            GameEvents.ClearAll();
            var cards = new CardManager(1);
            var consumed = cards.ConsumeTicket();
            TestRunner.Assert(consumed == null, "Ticket: returns null when no dark cards");
        }

        // ---- PhaseManager Tests ----

        static void TestPhaseManagerLock()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            var phase = pm.LockRandomPhase();
            TestRunner.Assert(phase != null, "PhaseManager: random lock returns phase");
            TestRunner.Assert(pm.IsLocked, "PhaseManager: IsLocked = true after lock");
            TestRunner.Assert(pm.ActivePhaseType.HasValue, "PhaseManager: ActivePhaseType has value");
        }

        static void TestPhaseManagerMutualExclusion()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockRandomPhase();

            bool threw = false;
            try
            {
                pm.LockRandomPhase(); // should throw
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            TestRunner.Assert(threw, "PhaseManager: double lock throws exception (mutual exclusion)");
        }

        static void TestPhaseManagerReset()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockRandomPhase();
            pm.Reset();
            TestRunner.Assert(!pm.IsLocked, "PhaseManager: IsLocked = false after reset");
            TestRunner.Assert(pm.ActivePhase == null, "PhaseManager: ActivePhase = null after reset");

            // Should be able to lock again after reset
            var phase = pm.LockRandomPhase();
            TestRunner.Assert(phase != null, "PhaseManager: can lock again after reset");
        }

        // ---- CeasefirePhase Tests ----

        static void TestCeasefirePhase_ScissorsDisabled()
        {
            var phase = new CeasefirePhase();
            TestRunner.Assert(!phase.IsCardPlayable(CardType.Scissors), "Ceasefire: Scissors not playable");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Rock), "Ceasefire: Rock is playable");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Paper), "Ceasefire: Paper is playable");
        }

        static void TestCeasefirePhase_RockBeatsPaper()
        {
            GameEvents.ClearAll();
            var phase = new CeasefirePhase();
            var result = phase.ResolveDuel(CardType.Rock, CardType.Paper);
            TestRunner.AssertEqual(DuelOutcome.Win, result, "Ceasefire: Rock beats Paper");

            var result2 = phase.ResolveDuel(CardType.Paper, CardType.Rock);
            TestRunner.AssertEqual(DuelOutcome.Lose, result2, "Ceasefire: Paper loses to Rock");
        }

        // ---- JokerPhase Tests ----

        static void TestJokerPhase_SwapTriggered()
        {
            GameEvents.ClearAll();
            // Use a seeded random that will produce a low value (< 0.2)
            // Random(0) produces NextDouble() ≈ 0.727... so we need to find a seed that gives < 0.2
            // Let's test with a mock approach: create many seeds until we find one that triggers swap
            bool swapFound = false;
            for (int seed = 0; seed < 1000; seed++)
            {
                var testRandom = new Random(seed);
                double val = testRandom.NextDouble();
                if (val < 0.2)
                {
                    var phase = new JokerPhase(new Random(seed));
                    CardType a = CardType.Rock;
                    CardType b = CardType.Paper;
                    phase.PreDuelModify(ref a, ref b, out bool swapped);
                    TestRunner.Assert(swapped, $"Joker: swap triggered (seed={seed})");
                    TestRunner.AssertEqual(CardType.Paper, a, "Joker: initiator gets defender's card");
                    TestRunner.AssertEqual(CardType.Rock, b, "Joker: defender gets initiator's card");
                    swapFound = true;
                    break;
                }
            }
            TestRunner.Assert(swapFound, "Joker: found a seed that triggers swap");
        }

        static void TestJokerPhase_NoSwap()
        {
            GameEvents.ClearAll();
            // Find a seed that doesn't trigger swap (>= 0.2)
            for (int seed = 0; seed < 1000; seed++)
            {
                var testRandom = new Random(seed);
                double val = testRandom.NextDouble();
                if (val >= 0.2)
                {
                    var phase = new JokerPhase(new Random(seed));
                    CardType a = CardType.Rock;
                    CardType b = CardType.Paper;
                    phase.PreDuelModify(ref a, ref b, out bool swapped);
                    TestRunner.Assert(!swapped, $"Joker: no swap (seed={seed})");
                    TestRunner.AssertEqual(CardType.Rock, a, "Joker: initiator keeps their card");
                    break;
                }
            }
        }

        // ---- DestinyGambitPhase Tests ----

        static void TestDestinyGambit_CriticalReward()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase();
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.WinnerMatched);
            TestRunner.AssertEqual(2.0f, reward, "DestinyGambit: winner matched -> 2x reward");
            TestRunner.AssertEqual(1.0f, penalty, "DestinyGambit: winner matched -> 1x penalty");
        }

        static void TestDestinyGambit_CounterPenalty()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase();
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.DefenderMatched);
            TestRunner.AssertEqual(1.0f, reward, "DestinyGambit: defender matched -> 1x reward");
            TestRunner.AssertEqual(2.0f, penalty, "DestinyGambit: defender matched -> 2x penalty");
        }

        // ---- InfiniteFirepowerPhase Tests ----

        static void TestInfiniteFirepower_ChargeSpeed()
        {
            var phase = new InfiniteFirepowerPhase();
            TestRunner.AssertEqual(2.0f, phase.GetChargeSpeedMultiplier(), "InfiniteFirepower: 2x charge speed");
        }

        static void TestInfiniteFirepower_GhostProtection()
        {
            GameEvents.ClearAll();
            var phase = new InfiniteFirepowerPhase();
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.None);
            TestRunner.Assert(penalty <= 0.5f, "InfiniteFirepower: ghost protection halves penalty");
        }

        // ---- SubspaceDuelManager Tests ----

        static void TestDuelExecution_BasicWin()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm, new Random(42));

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);

            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            // Rock vs Scissors -> initiator wins
            var result = duelMgr.ExecuteDuel(1, 2, cards, skill,
                CardType.Rock, CardType.Scissors);

            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome, "Duel: Rock beats Scissors");
            TestRunner.AssertEqual(1, result.InitiatorId, "Duel: correct initiator ID");
            TestRunner.AssertEqual(2, result.DefenderId, "Duel: correct defender ID");
        }

        static void TestDuelExecution_Draw()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm, new Random(42));

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);

            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var result = duelMgr.ExecuteDuel(1, 2, cards, skill,
                CardType.Rock, CardType.Rock);

            TestRunner.AssertEqual(DuelOutcome.Draw, result.Outcome, "Duel: Rock vs Rock is Draw");
        }

        static void TestDuelValidation_NoTicket()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm);

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);

            var cards = new CardManager(1); // no dark cards

            var (canInitiate, reason) = duelMgr.ValidateDuelConditions(skill, cards);
            TestRunner.Assert(!canInitiate, "Duel validation: fails with no ticket");
            TestRunner.Assert(reason.Contains("暗牌"), "Duel validation: reason mentions dark card");
        }

        static void TestDuelValidation_NotFullyCharged()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm);

            var skill = new SkillSlotManager(1);
            skill.AddCharge(100f); // only 1 slot

            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var (canInitiate, reason) = duelMgr.ValidateDuelConditions(skill, cards);
            TestRunner.Assert(!canInitiate, "Duel validation: fails when not fully charged");
        }

        // ---- GameManager Integration Tests ----

        static void TestGameManagerLifecycle()
        {
            GameEvents.ClearAll();
            var gm = new GameManager(new Random(42));
            gm.InitializeGame(new[] { 1, 2 });

            TestRunner.Assert(gm.IsGameActive, "GameManager: game is active after init");
            TestRunner.Assert(gm.PhaseManager.IsLocked, "GameManager: phase is locked");

            var p1 = gm.GetPlayerSession(1);
            TestRunner.Assert(p1 != null, "GameManager: player 1 session exists");
            TestRunner.Assert(p1.Cards.Hand.Count > 0, "GameManager: player 1 has cards");
            TestRunner.Assert(p1.Cards.DarkCardCount > 0, "GameManager: player 1 has dark cards");

            gm.EndGame();
            TestRunner.Assert(!gm.IsGameActive, "GameManager: game ended");
            gm.Dispose();
        }

        static void TestFullDuelFlow()
        {
            GameEvents.ClearAll();
            var gm = new GameManager(new Random(42));
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            // Charge player 1's skill slots to full
            gm.HandleDrift(1, 300f);

            var p1 = gm.GetPlayerSession(1);
            TestRunner.Assert(p1.SkillSlots.IsFullyCharged, "FullFlow: player 1 fully charged");

            // Execute duel
            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            TestRunner.Assert(result != null, "FullFlow: duel result not null");
            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome, "FullFlow: Rock beats Scissors");
            TestRunner.AssertEqual(PhaseType.DestinyGambit, result.ActivePhase, "FullFlow: correct phase");

            // After duel, skill slots should be consumed
            TestRunner.Assert(!p1.SkillSlots.IsFullyCharged, "FullFlow: skill slots consumed");
            // Dark card count should decrease
            TestRunner.AssertEqual(1, p1.Cards.DarkCardCount, "FullFlow: one dark card consumed as ticket");

            gm.EndGame();
            gm.Dispose();
        }

        /// <summary>
        /// 程序入口 — 独立运行测试
        /// </summary>
        public static void Main(string[] args)
        {
            RunAll();
            Environment.Exit(TestRunner.FailedCount > 0 ? 1 : 0);
        }
    }
}
