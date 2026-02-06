// ============================================================
// CoreSystemTests.cs — 核心系统单元测试
// 架构层级: Tests
// 说明: 覆盖技能格、门票、子空间对决、相位系统的核心逻辑。
//       包含Task 2新增的: 胜天半子、剪视为石、过热计时、
//       幽灵保护追踪器等新机制测试。
//       使用纯C#测试 (不依赖Unity Test Runner)。
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
    /// 简易测试框架
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
            TestSkillSlotChargeSpeedMultiplier();

            Console.WriteLine("\n=== SkillSlot Timed Overheat Tests ===");
            TestTimedOverheat_Basic();
            TestTimedOverheat_ExpiresAfterDuration();
            TestTimedOverheat_BlocksActivation();

            Console.WriteLine("\n=== CardManager Tests ===");
            TestCardManagerBasicOperations();
            TestTicketConsumption_FIFO();
            TestTicketConsumption_Empty();

            Console.WriteLine("\n=== PhaseManager Tests ===");
            TestPhaseManagerLock();
            TestPhaseManagerMutualExclusion();
            TestPhaseManagerReset();

            Console.WriteLine("\n=== DestinyGambitPhase Tests ===");
            TestDestinyGambit_CriticalReward();
            TestDestinyGambit_CounterPenalty();
            TestDestinyGambit_BadLuck();
            TestDestinyGambit_ShengTianBanZi();
            TestDestinyGambit_DrawNoMatch();

            Console.WriteLine("\n=== JokerPhase Tests ===");
            TestJokerPhase_SwapTriggered();
            TestJokerPhase_NoSwap();

            Console.WriteLine("\n=== CeasefirePhase Tests (Updated: 剪视为石) ===");
            TestCeasefirePhase_ScissorsConvertedToRock();
            TestCeasefirePhase_RockBeatsPaper();
            TestCeasefirePhase_ScissorsVsScissors_BecomesDraw();
            TestCeasefirePhase_AllCardsPlayable();

            Console.WriteLine("\n=== InfiniteFirepowerPhase Tests (Updated: 2.5x, 5s overheat) ===");
            TestInfiniteFirepower_ChargeSpeed();
            TestInfiniteFirepower_GhostProtectionMultiplier();
            TestInfiniteFirepower_OverheatDuration();

            Console.WriteLine("\n=== GhostProtectionTracker Tests ===");
            TestGhostTracker_NoProtectionInitially();
            TestGhostTracker_ActivatesAfterThreshold();
            TestGhostTracker_ExpiresAfterDuration();
            TestGhostTracker_WindowExpiry();

            Console.WriteLine("\n=== SubspaceDuelManager Tests ===");
            TestDuelExecution_BasicWin();
            TestDuelExecution_Draw();
            TestDuelValidation_NoTicket();
            TestDuelValidation_NotFullyCharged();
            TestDuelValidation_Overheated();
            TestDuelValidation_GhostProtection();

            Console.WriteLine("\n=== Duel Result Data Fields Tests ===");
            TestDuelResult_OriginalCards();
            TestDuelResult_ScissorsConverted();
            TestDuelResult_DestinyEffectType();
            TestDuelResult_ShengTianBanZi();

            Console.WriteLine("\n=== GameManager Integration Tests ===");
            TestGameManagerLifecycle();
            TestFullDuelFlow();
            TestInfiniteFirepower_FullFlow();
            TestCeasefire_FullFlow();

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
            skill.AddCharge(300f);
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

        static void TestSkillSlotChargeSpeedMultiplier()
        {
            var skill = new SkillSlotManager(1);
            skill.ChargeSpeedMultiplier = 2.0f;
            skill.AddCharge(50f); // effective = 100f -> fills 1 slot
            TestRunner.AssertEqual(1, skill.FilledSlots, "SkillSlot: 2x multiplier doubles charge");
        }

        // ---- Timed Overheat Tests ----

        static void TestTimedOverheat_Basic()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);

            TestRunner.Assert(!skill.IsOverheated, "TimedOverheat: not overheated initially");
            skill.StartOverheat(5.0f);
            TestRunner.Assert(skill.IsOverheated, "TimedOverheat: overheated after StartOverheat");
        }

        static void TestTimedOverheat_ExpiresAfterDuration()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var skill = new SkillSlotManager(1, time);

            time.CurrentTime = 10f;
            skill.StartOverheat(5.0f);
            TestRunner.Assert(skill.IsOverheated, "TimedOverheat: overheated at t=10");

            time.Advance(4.9f); // t=14.9
            TestRunner.Assert(skill.IsOverheated, "TimedOverheat: still overheated at t=14.9");

            time.Advance(0.2f); // t=15.1
            TestRunner.Assert(!skill.IsOverheated, "TimedOverheat: expired at t=15.1 (5s passed)");
        }

        static void TestTimedOverheat_BlocksActivation()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);
            skill.StartOverheat(5.0f);

            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(!activated, "TimedOverheat: activation blocked while overheated");

            time.Advance(5.1f);
            // Recharge (overheat blocked previous activation so slots are still full)
            activated = skill.TryActivateSkill();
            TestRunner.Assert(activated, "TimedOverheat: activation works after overheat expires");
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
            try { pm.LockRandomPhase(); }
            catch (InvalidOperationException) { threw = true; }
            TestRunner.Assert(threw, "PhaseManager: double lock throws (mutual exclusion)");
        }

        static void TestPhaseManagerReset()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockRandomPhase();
            pm.Reset();
            TestRunner.Assert(!pm.IsLocked, "PhaseManager: IsLocked = false after reset");
            var phase = pm.LockRandomPhase();
            TestRunner.Assert(phase != null, "PhaseManager: can lock again after reset");
        }

        // ---- DestinyGambitPhase Tests ----

        static void TestDestinyGambit_CriticalReward()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.WinnerMatched);
            TestRunner.AssertEqual(2.0f, reward, "DestinyGambit: winner matched -> 2x reward");
            TestRunner.AssertEqual(1.0f, penalty, "DestinyGambit: winner matched -> 1x penalty");
        }

        static void TestDestinyGambit_CounterPenalty()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.DefenderMatched);
            TestRunner.AssertEqual(1.0f, reward, "DestinyGambit: defender matched -> 1x reward");
            TestRunner.AssertEqual(2.0f, penalty, "DestinyGambit: defender matched -> 2x penalty");
        }

        static void TestDestinyGambit_BadLuck()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.LoserMatched);
            TestRunner.AssertEqual(1.0f, reward, "DestinyGambit: loser matched -> 1x reward (no reduction)");
            TestRunner.AssertEqual(1.0f, penalty, "DestinyGambit: loser matched -> 1x penalty (bad luck)");
        }

        static void TestDestinyGambit_ShengTianBanZi()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));

            // 胜天半子条件: 平局 + 双方都压中天命牌
            // Rock vs Rock (draw), destiny = Rock -> both match
            var match = phase.ResolveDestinyMatch(
                DuelOutcome.Draw, CardType.Rock, CardType.Rock, CardType.Rock);
            TestRunner.AssertEqual(DestinyMatchType.BothMatchedDraw, match,
                "ShengTianBanZi: draw + both match -> BothMatchedDraw");

            // 验证奖励倍率在1.25~1.30范围内
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.BothMatchedDraw);
            TestRunner.Assert(reward >= 1.25f && reward <= 1.30f,
                $"ShengTianBanZi: reward in [1.25, 1.30] (actual: {reward:F4})");
            TestRunner.AssertEqual(1.0f, penalty, "ShengTianBanZi: penalty = 1.0 (no penalty)");

            // 验证天命效果类型
            var effect = phase.ResolveDestinyEffect(DestinyMatchType.BothMatchedDraw);
            TestRunner.AssertEqual(DestinyEffectType.ShengTianBanZi, effect,
                "ShengTianBanZi: effect type is ShengTianBanZi");
        }

        static void TestDestinyGambit_DrawNoMatch()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));

            // 平局但不压中天命: Rock vs Rock, destiny = Paper
            var match = phase.ResolveDestinyMatch(
                DuelOutcome.Draw, CardType.Rock, CardType.Rock, CardType.Paper);
            TestRunner.AssertEqual(DestinyMatchType.None, match,
                "DestinyGambit: draw + no match -> None");
        }

        // ---- JokerPhase Tests ----

        static void TestJokerPhase_SwapTriggered()
        {
            GameEvents.ClearAll();
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

        // ---- CeasefirePhase Tests (Updated) ----

        static void TestCeasefirePhase_ScissorsConvertedToRock()
        {
            GameEvents.ClearAll();
            var phase = new CeasefirePhase();

            CardType a = CardType.Scissors;
            CardType b = CardType.Paper;
            phase.PreDuelModify(ref a, ref b, out bool swapped);

            TestRunner.AssertEqual(CardType.Rock, a,
                "Ceasefire: initiator Scissors -> Rock (剪视为石)");
            TestRunner.AssertEqual(CardType.Paper, b,
                "Ceasefire: defender Paper unchanged");
            TestRunner.Assert(!swapped, "Ceasefire: conversion is not a swap");
        }

        static void TestCeasefirePhase_RockBeatsPaper()
        {
            GameEvents.ClearAll();
            var phase = new CeasefirePhase();
            var result = phase.ResolveDuel(CardType.Rock, CardType.Paper);
            TestRunner.AssertEqual(DuelOutcome.Win, result, "Ceasefire: Rock beats Paper (撞晕)");

            var result2 = phase.ResolveDuel(CardType.Paper, CardType.Rock);
            TestRunner.AssertEqual(DuelOutcome.Lose, result2, "Ceasefire: Paper loses to Rock");
        }

        static void TestCeasefirePhase_ScissorsVsScissors_BecomesDraw()
        {
            GameEvents.ClearAll();
            var phase = new CeasefirePhase();

            CardType a = CardType.Scissors;
            CardType b = CardType.Scissors;
            phase.PreDuelModify(ref a, ref b, out bool swapped);

            TestRunner.AssertEqual(CardType.Rock, a, "Ceasefire: Scissors A -> Rock");
            TestRunner.AssertEqual(CardType.Rock, b, "Ceasefire: Scissors B -> Rock");

            var result = phase.ResolveDuel(a, b);
            TestRunner.AssertEqual(DuelOutcome.Draw, result, "Ceasefire: Rock vs Rock = Draw");
        }

        static void TestCeasefirePhase_AllCardsPlayable()
        {
            var phase = new CeasefirePhase();
            TestRunner.Assert(phase.IsCardPlayable(CardType.Scissors),
                "Ceasefire: Scissors IS playable (converted, not blocked)");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Rock), "Ceasefire: Rock is playable");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Paper), "Ceasefire: Paper is playable");
        }

        // ---- InfiniteFirepowerPhase Tests (Updated) ----

        static void TestInfiniteFirepower_ChargeSpeed()
        {
            var phase = new InfiniteFirepowerPhase();
            TestRunner.AssertEqual(2.5f, phase.GetChargeSpeedMultiplier(),
                "InfiniteFirepower: 2.5x charge speed");
        }

        static void TestInfiniteFirepower_GhostProtectionMultiplier()
        {
            GameEvents.ClearAll();
            var phase = new InfiniteFirepowerPhase();
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.None);
            TestRunner.Assert(penalty <= 0.5f,
                "InfiniteFirepower: ghost protection halves penalty");
        }

        static void TestInfiniteFirepower_OverheatDuration()
        {
            GameEvents.ClearAll();
            var phase = new InfiniteFirepowerPhase();
            var result = new DuelResultData();
            phase.PostDuelEffect(result);
            TestRunner.AssertEqual(5.0f, result.OverheatDuration,
                "InfiniteFirepower: overheat duration = 5s");
        }

        // ---- GhostProtectionTracker Tests ----

        static void TestGhostTracker_NoProtectionInitially()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time);
            TestRunner.Assert(!tracker.IsProtected, "GhostTracker: not protected initially");
            TestRunner.AssertEqual(0, tracker.RecentPullCount, "GhostTracker: 0 pulls initially");
        }

        static void TestGhostTracker_ActivatesAfterThreshold()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();
            TestRunner.Assert(!tracker.IsProtected, "GhostTracker: not protected after 1 pull");

            time.Advance(10f);
            tracker.RecordPull();
            TestRunner.Assert(!tracker.IsProtected, "GhostTracker: not protected after 2 pulls");

            time.Advance(10f);
            bool triggered = tracker.RecordPull();
            TestRunner.Assert(triggered, "GhostTracker: protection triggered on 3rd pull");
            TestRunner.Assert(tracker.IsProtected, "GhostTracker: now protected");
        }

        static void TestGhostTracker_ExpiresAfterDuration()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();
            time.Advance(1f);
            tracker.RecordPull();
            time.Advance(1f);
            tracker.RecordPull();

            TestRunner.Assert(tracker.IsProtected, "GhostTracker: protected after 3 pulls");

            time.Advance(19.9f);
            TestRunner.Assert(tracker.IsProtected, "GhostTracker: still protected at 19.9s");

            time.Advance(0.2f);
            TestRunner.Assert(!tracker.IsProtected, "GhostTracker: protection expired at 20.1s");
        }

        static void TestGhostTracker_WindowExpiry()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();

            time.CurrentTime = 44f;
            tracker.RecordPull();

            time.CurrentTime = 46f;
            bool triggered = tracker.RecordPull();
            TestRunner.Assert(!triggered, "GhostTracker: 3rd pull outside window doesn't trigger");
            TestRunner.AssertEqual(2, tracker.RecentPullCount,
                "GhostTracker: only 2 pulls in window (oldest expired)");
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

            var result = duelMgr.ExecuteDuel(1, 2, cards, skill, CardType.Rock, CardType.Scissors);

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

            var result = duelMgr.ExecuteDuel(1, 2, cards, skill, CardType.Rock, CardType.Rock);
            TestRunner.AssertEqual(DuelOutcome.Draw, result.Outcome, "Duel: Rock vs Rock = Draw");
        }

        static void TestDuelValidation_NoTicket()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm);

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);

            var cards = new CardManager(1);

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
            skill.AddCharge(100f);

            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var (canInitiate, _) = duelMgr.ValidateDuelConditions(skill, cards);
            TestRunner.Assert(!canInitiate, "Duel validation: fails when not fully charged");
        }

        static void TestDuelValidation_Overheated()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.InfiniteFirepower);
            var duelMgr = new SubspaceDuelManager(pm);

            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);
            skill.StartOverheat(5f);

            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var (canInitiate, reason) = duelMgr.ValidateDuelConditions(skill, cards);
            TestRunner.Assert(!canInitiate, "Duel validation: fails when overheated");
            TestRunner.Assert(reason.Contains("过热"), "Duel validation: reason mentions overheat");
        }

        static void TestDuelValidation_GhostProtection()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.InfiniteFirepower);
            var duelMgr = new SubspaceDuelManager(pm);

            var tracker = new GhostProtectionTracker(2, time, pullThreshold: 1, timeWindow: 45f, protectionDuration: 20f);
            tracker.RecordPull();

            var (canPull, reason) = duelMgr.ValidateDefenderPullable(tracker);
            TestRunner.Assert(!canPull, "Duel validation: fails when defender has ghost protection");
            TestRunner.Assert(reason.Contains("幽灵保护"), "Duel validation: reason mentions ghost protection");
        }

        // ---- Duel Result Data Fields Tests ----

        static void TestDuelResult_OriginalCards()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.Ceasefire);
            var duelMgr = new SubspaceDuelManager(pm, new Random(42));

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var result = duelMgr.ExecuteDuel(1, 2, cards, skill, CardType.Scissors, CardType.Paper);

            TestRunner.AssertEqual(CardType.Scissors, result.OriginalInitiatorCard,
                "DuelResult: original initiator card preserved");
            TestRunner.AssertEqual(CardType.Rock, result.InitiatorCard,
                "DuelResult: final card is Rock (converted)");
        }

        static void TestDuelResult_ScissorsConverted()
        {
            GameEvents.ClearAll();
            var pm = new PhaseManager(new Random(42));
            pm.LockPhase(PhaseType.Ceasefire);
            var duelMgr = new SubspaceDuelManager(pm, new Random(42));

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            var result = duelMgr.ExecuteDuel(1, 2, cards, skill, CardType.Scissors, CardType.Paper);
            TestRunner.Assert(result.ScissorsConverted, "DuelResult: ScissorsConverted flag set");
        }

        static void TestDuelResult_DestinyEffectType()
        {
            GameEvents.ClearAll();
            var phase = new DestinyGambitPhase(new Random(42));

            TestRunner.AssertEqual(DestinyEffectType.Crit,
                phase.ResolveDestinyEffect(DestinyMatchType.WinnerMatched),
                "DestinyEffect: WinnerMatched -> Crit");
            TestRunner.AssertEqual(DestinyEffectType.Counter,
                phase.ResolveDestinyEffect(DestinyMatchType.DefenderMatched),
                "DestinyEffect: DefenderMatched -> Counter");
            TestRunner.AssertEqual(DestinyEffectType.BadLuck,
                phase.ResolveDestinyEffect(DestinyMatchType.LoserMatched),
                "DestinyEffect: LoserMatched -> BadLuck");
            TestRunner.AssertEqual(DestinyEffectType.ShengTianBanZi,
                phase.ResolveDestinyEffect(DestinyMatchType.BothMatchedDraw),
                "DestinyEffect: BothMatchedDraw -> ShengTianBanZi");
        }

        static void TestDuelResult_ShengTianBanZi()
        {
            GameEvents.ClearAll();
            // Find a seed where destiny card = Rock (0)
            int targetSeed = -1;
            for (int seed = 0; seed < 1000; seed++)
            {
                var r = new Random(seed);
                if ((CardType)r.Next(3) == CardType.Rock)
                {
                    targetSeed = seed;
                    break;
                }
            }

            var pm = new PhaseManager(new Random(targetSeed));
            pm.LockPhase(PhaseType.DestinyGambit);
            var duelMgr = new SubspaceDuelManager(pm, new Random(targetSeed));

            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            var cards = new CardManager(1);
            cards.AddDarkCard(new Card.Card(CardType.Rock, true));

            // Both play Rock, destiny = Rock -> ShengTianBanZi
            var result = duelMgr.ExecuteDuel(1, 2, cards, skill, CardType.Rock, CardType.Rock);
            TestRunner.Assert(result.IsShengTianBanZi,
                "DuelResult: ShengTianBanZi triggered in actual duel");
            TestRunner.AssertEqual(DuelOutcome.Draw, result.Outcome,
                "DuelResult: ShengTianBanZi is a draw");
        }

        // ---- GameManager Integration Tests ----

        static void TestGameManagerLifecycle()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
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
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            gm.HandleDrift(1, 300f);

            var p1 = gm.GetPlayerSession(1);
            TestRunner.Assert(p1.SkillSlots.IsFullyCharged, "FullFlow: player 1 fully charged");

            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            TestRunner.Assert(result != null, "FullFlow: duel result not null");
            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome, "FullFlow: Rock beats Scissors");
            TestRunner.AssertEqual(PhaseType.DestinyGambit, result.ActivePhase, "FullFlow: correct phase");
            TestRunner.Assert(!p1.SkillSlots.IsFullyCharged, "FullFlow: skill slots consumed");
            TestRunner.AssertEqual(1, p1.Cards.DarkCardCount, "FullFlow: one dark card consumed");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestInfiniteFirepower_FullFlow()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.InfiniteFirepower);

            var p1 = gm.GetPlayerSession(1);
            var p2 = gm.GetPlayerSession(2);

            // Verify 2.5x charge speed
            TestRunner.AssertEqual(2.5f, p1.SkillSlots.ChargeSpeedMultiplier,
                "InfFirepower Flow: 2.5x charge multiplier applied");

            // Verify ghost tracker initialized
            TestRunner.Assert(p2.GhostTracker != null,
                "InfFirepower Flow: ghost tracker initialized");

            // Charge and duel: 120 * 2.5 = 300 -> full
            gm.HandleDrift(1, 120f);
            TestRunner.Assert(p1.SkillSlots.IsFullyCharged,
                "InfFirepower Flow: 120 drift with 2.5x = full charge");

            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);
            TestRunner.Assert(result != null, "InfFirepower Flow: duel executed");

            // Verify overheat applied
            TestRunner.Assert(p1.SkillSlots.IsOverheated,
                "InfFirepower Flow: initiator overheated after duel");
            TestRunner.AssertEqual(5.0f, result.OverheatDuration,
                "InfFirepower Flow: overheat duration = 5s");

            // Can't duel while overheated
            gm.HandleDrift(1, 120f);
            var result2 = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Paper);
            TestRunner.Assert(result2 == null,
                "InfFirepower Flow: can't duel while overheated");

            // Wait for overheat to expire
            time.Advance(5.1f);
            TestRunner.Assert(!p1.SkillSlots.IsOverheated,
                "InfFirepower Flow: overheat expired after 5.1s");

            // Penalty multiplier halved by ghost protection
            TestRunner.Assert(result.PenaltyMultiplier <= 0.75f,
                $"InfFirepower Flow: penalty halved (actual: {result.PenaltyMultiplier})");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestCeasefire_FullFlow()
        {
            GameEvents.ClearAll();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.Ceasefire);

            gm.HandleDrift(1, 300f);

            // Scissors allowed but converted
            var result = gm.TryInitiateDuel(1, 2, CardType.Scissors, CardType.Paper);
            TestRunner.Assert(result != null, "Ceasefire Flow: duel executed with Scissors");
            TestRunner.AssertEqual(CardType.Rock, result.InitiatorCard,
                "Ceasefire Flow: Scissors converted to Rock");
            TestRunner.AssertEqual(CardType.Scissors, result.OriginalInitiatorCard,
                "Ceasefire Flow: original card preserved as Scissors");
            TestRunner.Assert(result.ScissorsConverted,
                "Ceasefire Flow: ScissorsConverted flag set");

            // Rock beats Paper in Ceasefire
            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome,
                "Ceasefire Flow: Rock(converted) beats Paper");

            gm.EndGame();
            gm.Dispose();
        }

        /// <summary>
        /// 程序入口
        /// </summary>
        public static void Main(string[] args)
        {
            RunAll();
            Environment.Exit(TestRunner.FailedCount > 0 ? 1 : 0);
        }
    }
}
