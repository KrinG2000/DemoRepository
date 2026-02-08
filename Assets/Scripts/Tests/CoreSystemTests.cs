// ============================================================
// CoreSystemTests.cs — 核心系统单元测试 (Task 4: 可验证性基建)
// 架构层级: Tests
// 说明: 覆盖B0-B7全部用例类别 (40+条):
//   B0 基础石头剪刀布 | B1 门票+溢出 | B2 P1免疫+幽灵
//   B3 小丑 | B4 止戈 | B5 赌场 | B6 横幅优先级 | B7 火力过热
//   + BalanceConfig / DuelLog / DebugHUD 基建测试
//   一键跑测试: mono test.exe → 全 PASS 即交付
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Skill;
using RacingCardGame.Phase;
using RacingCardGame.Duel;
using RacingCardGame.Manager;
using RacingCardGame.UI;
using RacingCardGame.Config;
using RacingCardGame.Debugging;

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
        public static int PassedCount => _passed;
    }

    public static class CoreSystemTests
    {
        /// <summary>
        /// 通用测试初始化 — 每个测试组前调用
        /// </summary>
        static void Setup()
        {
            GameEvents.ClearAll();
            BalanceConfig.Reset();
            DuelLog.Clear();
            DuelLog.Enabled = true;
        }

        public static void RunAll()
        {
            TestRunner.Reset();
            Card.Card.ResetIdCounter();
            Setup();

            // ========== B0: 基础石头剪刀布 ==========
            Console.WriteLine("=== B0: Basic RPS ===");
            TestB0_RPS_AllOutcomes();

            // ========== B1: 门票+溢出 ==========
            Console.WriteLine("\n=== B1: Tickets + Overflow ===");
            TestB1_TicketFIFO();
            TestB1_TicketEmpty();
            TestB1_OverflowOnThirdCard();
            TestB1_OverflowAutoExpiry();
            TestB1_OverflowReplaceSlot();
            TestB1_OverflowClearedOnSubspace();

            // ========== B2: P1免疫 + 幽灵保护 ==========
            Console.WriteLine("\n=== B2: P1 Immunity + Ghost ===");
            TestB2_P1Immunity_BlocksDuel();
            TestB2_P1Immunity_ExpiresAfterDuration();
            TestB2_P1Immunity_AppliedAfterDuel();
            TestB2_GhostProtection_ActivatesAfterThreshold();
            TestB2_GhostProtection_ExpiresAfterDuration();
            TestB2_GhostProtection_WindowExpiry();

            // ========== B3: 小丑相位 ==========
            Console.WriteLine("\n=== B3: Jester (Joker) ===");
            TestB3_JesterSwap_Triggered();
            TestB3_JesterSwap_NoSwap();
            TestB3_JesterSwap_ConfigDriven();

            // ========== B4: 止戈相位 ==========
            Console.WriteLine("\n=== B4: Peace (Ceasefire) ===");
            TestB4_ScissorsConvertedToRock();
            TestB4_RockBeatsPaper();
            TestB4_ScissorsVsScissors_Draw();
            TestB4_AllCardsPlayable();

            // ========== B5: 赌场 (天命赌场) ==========
            Console.WriteLine("\n=== B5: Casino (DestinyGambit) ===");
            TestB5_CritMultiplier_1_5x();
            TestB5_CounterMultiplier_1_5x();
            TestB5_ConquerHeaven_1_3x_Fixed();
            TestB5_BadLuck_NoBonus();
            TestB5_ShengTianBanZi_Trigger();

            // ========== B6: 横幅优先级 ==========
            Console.WriteLine("\n=== B6: Banner Priority ===");
            TestB6_P1_ConquerHeaven();
            TestB6_P2_JesterSwap();
            TestB6_P3_DestinyCrit();
            TestB6_P4_DestinyCounter();
            TestB6_P5_BadLuck();
            TestB6_P6_NormalWin();
            TestB6_P7_Draw();
            TestB6_Priority_ConquerHeavenOverDraw();
            TestB6_Priority_CritOverNormalWin();
            TestB6_Adapter_AllPhases();
            TestB6_Queue_SingleBanner();
            TestB6_Queue_DelayAndDuration();
            TestB6_Queue_SkipNonDeadlock();
            TestB6_Presenter_ShowHideSkip();

            // ========== B7: 火力过热 ==========
            Console.WriteLine("\n=== B7: Overload Overheat ===");
            TestB7_ChargeMultiplier_2_5x();
            TestB7_OverheatBlocks5s();
            TestB7_OverheatExpiresAndRecovers();
            TestB7_GhostPenaltyReduction();

            // ========== Config: BalanceConfig ==========
            Console.WriteLine("\n=== Config: BalanceConfig ===");
            TestConfig_DefaultValues();
            TestConfig_HotReload_VersionIncrement();
            TestConfig_CustomConfig_AffectsPhase();
            TestConfig_WeightedPhaseSelection();

            // ========== DuelLog ==========
            Console.WriteLine("\n=== DuelLog ===");
            TestDuelLog_6StagesPerDuel();
            TestDuelLog_Filter();
            TestDuelLog_DisableToggle();

            // ========== DebugHUD ==========
            Console.WriteLine("\n=== DebugHUD ===");
            TestDebugHUD_Toggle();
            TestDebugHUD_RenderPlayerState();
            TestDebugHUD_RenderLastDuel();

            // ========== Integration ==========
            Console.WriteLine("\n=== Integration ===");
            TestIntegration_FullDuelFlow();
            TestIntegration_InfiniteFirepowerFlow();
            TestIntegration_CeasefireFlow();

            // ========== SkillSlot fundamentals ==========
            Console.WriteLine("\n=== SkillSlot Fundamentals ===");
            TestSkillSlot_Charging();
            TestSkillSlot_FullCharge();
            TestSkillSlot_Activation();
            TestSkillSlot_ChargeSpeedMultiplier();
            TestSkillSlot_TimedOverheat();

            // ========== PhaseManager ==========
            Console.WriteLine("\n=== PhaseManager ===");
            TestPhaseManager_LockAndMutualExclusion();
            TestPhaseManager_Reset();

            TestRunner.PrintSummary();
        }

        // ============================================================
        // B0: 基础石头剪刀布 — 3种胜、3种负、3种平
        // ============================================================

        static void TestB0_RPS_AllOutcomes()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));

            // 3 wins
            TestRunner.AssertEqual(DuelOutcome.Win, phase.ResolveDuel(CardType.Rock, CardType.Scissors),
                "B0: Rock beats Scissors");
            TestRunner.AssertEqual(DuelOutcome.Win, phase.ResolveDuel(CardType.Scissors, CardType.Paper),
                "B0: Scissors beats Paper");
            TestRunner.AssertEqual(DuelOutcome.Win, phase.ResolveDuel(CardType.Paper, CardType.Rock),
                "B0: Paper beats Rock");

            // 3 losses
            TestRunner.AssertEqual(DuelOutcome.Lose, phase.ResolveDuel(CardType.Scissors, CardType.Rock),
                "B0: Scissors loses to Rock");
            TestRunner.AssertEqual(DuelOutcome.Lose, phase.ResolveDuel(CardType.Paper, CardType.Scissors),
                "B0: Paper loses to Scissors");
            TestRunner.AssertEqual(DuelOutcome.Lose, phase.ResolveDuel(CardType.Rock, CardType.Paper),
                "B0: Rock loses to Paper");

            // 3 draws
            TestRunner.AssertEqual(DuelOutcome.Draw, phase.ResolveDuel(CardType.Rock, CardType.Rock),
                "B0: Rock vs Rock = Draw");
            TestRunner.AssertEqual(DuelOutcome.Draw, phase.ResolveDuel(CardType.Scissors, CardType.Scissors),
                "B0: Scissors vs Scissors = Draw");
            TestRunner.AssertEqual(DuelOutcome.Draw, phase.ResolveDuel(CardType.Paper, CardType.Paper),
                "B0: Paper vs Paper = Draw");
        }

        // ============================================================
        // B1: 门票+溢出
        // ============================================================

        static void TestB1_TicketFIFO()
        {
            Setup();
            var cards = new CardManager(1);
            var dark1 = new Card.Card(CardType.Rock, true, 100);
            var dark2 = new Card.Card(CardType.Paper, true, 200);
            var dark3 = new Card.Card(CardType.Scissors, true, 300);

            cards.AddDarkCard(dark1);
            cards.AddDarkCard(dark2);
            cards.AddDarkCard(dark3);

            TestRunner.AssertEqual(3, cards.DarkCardCount, "B1 FIFO: 3 dark cards initially");

            var consumed = cards.ConsumeTicket();
            TestRunner.AssertEqual(dark1.Id, consumed.Id, "B1 FIFO: oldest consumed first");

            var consumed2 = cards.ConsumeTicket();
            TestRunner.AssertEqual(dark2.Id, consumed2.Id, "B1 FIFO: second oldest next");

            var consumed3 = cards.ConsumeTicket();
            TestRunner.AssertEqual(dark3.Id, consumed3.Id, "B1 FIFO: third oldest last");
            TestRunner.AssertEqual(0, cards.DarkCardCount, "B1 FIFO: 0 dark cards after all consumed");
        }

        static void TestB1_TicketEmpty()
        {
            Setup();
            var cards = new CardManager(1);
            var consumed = cards.ConsumeTicket();
            TestRunner.Assert(consumed == null, "B1 Empty: returns null when no dark cards");
        }

        static void TestB1_OverflowOnThirdCard()
        {
            Setup();
            var time = new ManualTimeProvider();
            var cards = new CardManager(1, time);

            cards.AddToHand(new Card.Card(CardType.Rock, false));
            cards.AddToHand(new Card.Card(CardType.Scissors, false));
            TestRunner.AssertEqual(2, cards.Hand.Count, "B1 Overflow: 2 cards in hand (MaxHandSlots)");
            TestRunner.Assert(!cards.HasOverflow, "B1 Overflow: no overflow yet");

            // 3rd card goes to overflow
            var thirdCard = new Card.Card(CardType.Paper, false);
            cards.AddToHand(thirdCard);
            TestRunner.AssertEqual(2, cards.Hand.Count, "B1 Overflow: still 2 in hand after 3rd");
            TestRunner.Assert(cards.HasOverflow, "B1 Overflow: overflow card exists");
            TestRunner.AssertEqual(CardType.Paper, cards.OverflowCard.Type, "B1 Overflow: overflow is Paper");
        }

        static void TestB1_OverflowAutoExpiry()
        {
            Setup();
            var time = new ManualTimeProvider();
            var cards = new CardManager(1, time);

            cards.AddToHand(new Card.Card(CardType.Rock, false));
            cards.AddToHand(new Card.Card(CardType.Scissors, false));

            time.CurrentTime = 10f;
            cards.AddToHand(new Card.Card(CardType.Paper, false));
            TestRunner.Assert(cards.HasOverflow, "B1 Expiry: overflow exists at t=10");

            // Still there at 3.9s
            time.Advance(3.9f);
            TestRunner.Assert(cards.HasOverflow, "B1 Expiry: overflow still exists at t=13.9");
            TestRunner.Assert(cards.OverflowRemainingTime > 0f, "B1 Expiry: remaining time > 0");

            // Expired at 4.1s
            time.Advance(0.2f);
            TestRunner.Assert(!cards.HasOverflow, "B1 Expiry: overflow expired at t=14.1 (4s lifetime)");
        }

        static void TestB1_OverflowReplaceSlot()
        {
            Setup();
            var time = new ManualTimeProvider();
            var cards = new CardManager(1, time);

            cards.AddToHand(new Card.Card(CardType.Rock, false));
            cards.AddToHand(new Card.Card(CardType.Scissors, false));
            cards.AddToHand(new Card.Card(CardType.Paper, false)); // -> overflow

            // Replace Slot 0 (Rock) with overflow (Paper)
            var replaced = cards.ReplaceSlotWithOverflow(0);
            TestRunner.Assert(replaced != null, "B1 Replace: replaced card not null");
            TestRunner.AssertEqual(CardType.Rock, replaced.Type, "B1 Replace: replaced card was Rock");
            TestRunner.AssertEqual(CardType.Paper, cards.Hand[0].Type, "B1 Replace: Slot 0 is now Paper");
            TestRunner.Assert(!cards.HasOverflow, "B1 Replace: overflow cleared after replace");
        }

        static void TestB1_OverflowClearedOnSubspace()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            var p1 = gm.GetPlayerSession(1);

            // Manually add overflow
            p1.Cards.AddToHand(new Card.Card(CardType.Paper, false));
            TestRunner.Assert(p1.Cards.HasOverflow, "B1 Subspace: overflow exists before duel");

            gm.HandleDrift(1, 300f);
            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            TestRunner.Assert(result != null, "B1 Subspace: duel executed");
            TestRunner.Assert(!p1.Cards.HasOverflow, "B1 Subspace: overflow cleared on subspace entry");

            gm.EndGame();
            gm.Dispose();
        }

        // ============================================================
        // B2: P1免疫 + 幽灵保护
        // ============================================================

        static void TestB2_P1Immunity_BlocksDuel()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            gm.HandleDrift(1, 300f);
            // First duel: should succeed
            var result1 = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);
            TestRunner.Assert(result1 != null, "B2 P1Imm: first duel succeeds");

            // Defender (P2) now has immunity. P1 attacks P2 again:
            gm.HandleDrift(1, 300f);
            var result2 = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);
            TestRunner.Assert(result2 == null, "B2 P1Imm: second duel blocked by P1 immunity");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestB2_P1Immunity_ExpiresAfterDuration()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            gm.HandleDrift(1, 300f);
            gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            // P2 is immune for 7s (default P1_ImmunityDuration)
            var p2 = gm.GetPlayerSession(2);
            TestRunner.Assert(p2.IsImmune, "B2 P1Imm Expiry: P2 immune after duel");

            time.Advance(6.9f);
            TestRunner.Assert(p2.IsImmune, "B2 P1Imm Expiry: P2 still immune at 6.9s");

            time.Advance(0.2f); // 7.1s total
            TestRunner.Assert(!p2.IsImmune, "B2 P1Imm Expiry: P2 immunity expired at 7.1s");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestB2_P1Immunity_AppliedAfterDuel()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            var p2 = gm.GetPlayerSession(2);
            TestRunner.Assert(!p2.IsImmune, "B2 P1Imm Apply: P2 not immune initially");
            TestRunner.AssertEqual(0f, p2.ImmunityRemainingTime, "B2 P1Imm Apply: 0s remaining initially");

            gm.HandleDrift(1, 300f);
            gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            TestRunner.Assert(p2.IsImmune, "B2 P1Imm Apply: P2 immune after being pulled");
            TestRunner.Assert(p2.ImmunityRemainingTime > 6f, "B2 P1Imm Apply: remaining > 6s");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestB2_GhostProtection_ActivatesAfterThreshold()
        {
            Setup();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();
            TestRunner.Assert(!tracker.IsProtected, "B2 Ghost: not protected after 1 pull");

            time.Advance(10f);
            tracker.RecordPull();
            TestRunner.Assert(!tracker.IsProtected, "B2 Ghost: not protected after 2 pulls");

            time.Advance(10f);
            bool triggered = tracker.RecordPull();
            TestRunner.Assert(triggered, "B2 Ghost: triggered on 3rd pull");
            TestRunner.Assert(tracker.IsProtected, "B2 Ghost: now protected");
        }

        static void TestB2_GhostProtection_ExpiresAfterDuration()
        {
            Setup();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();
            time.Advance(1f);
            tracker.RecordPull();
            time.Advance(1f);
            tracker.RecordPull();

            TestRunner.Assert(tracker.IsProtected, "B2 Ghost Expire: protected after 3 pulls");

            time.Advance(19.9f);
            TestRunner.Assert(tracker.IsProtected, "B2 Ghost Expire: still protected at 19.9s");

            time.Advance(0.2f);
            TestRunner.Assert(!tracker.IsProtected, "B2 Ghost Expire: expired at 20.1s");
        }

        static void TestB2_GhostProtection_WindowExpiry()
        {
            Setup();
            var time = new ManualTimeProvider();
            var tracker = new GhostProtectionTracker(1, time, pullThreshold: 3, timeWindow: 45f, protectionDuration: 20f);

            time.CurrentTime = 0f;
            tracker.RecordPull();

            time.CurrentTime = 44f;
            tracker.RecordPull();

            time.CurrentTime = 46f; // 1st pull is now outside 45s window
            bool triggered = tracker.RecordPull();
            TestRunner.Assert(!triggered, "B2 Ghost Window: 3rd pull outside window doesn't trigger");
            TestRunner.AssertEqual(2, tracker.RecentPullCount, "B2 Ghost Window: only 2 pulls in window");
        }

        // ============================================================
        // B3: 小丑相位
        // ============================================================

        static void TestB3_JesterSwap_Triggered()
        {
            Setup();
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
                    TestRunner.Assert(swapped, $"B3 Swap: triggered (seed={seed})");
                    TestRunner.AssertEqual(CardType.Paper, a, "B3 Swap: initiator gets defender's card");
                    TestRunner.AssertEqual(CardType.Rock, b, "B3 Swap: defender gets initiator's card");
                    swapFound = true;
                    break;
                }
            }
            TestRunner.Assert(swapFound, "B3 Swap: found a seed that triggers swap");
        }

        static void TestB3_JesterSwap_NoSwap()
        {
            Setup();
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
                    TestRunner.Assert(!swapped, $"B3 NoSwap: no swap (seed={seed})");
                    TestRunner.AssertEqual(CardType.Rock, a, "B3 NoSwap: initiator keeps card");
                    break;
                }
            }
        }

        static void TestB3_JesterSwap_ConfigDriven()
        {
            Setup();
            // Override config to 100% swap chance
            var config = new BalanceConfig();
            config.JesterTriggerChance = 1.0f;
            BalanceConfig.SetCurrent(config);

            var phase = new JokerPhase(new Random(42));
            CardType a = CardType.Rock;
            CardType b = CardType.Paper;
            phase.PreDuelModify(ref a, ref b, out bool swapped);
            TestRunner.Assert(swapped, "B3 Config: 100% chance -> always swap");

            // Override to 0% swap chance
            config.JesterTriggerChance = 0f;
            BalanceConfig.SetCurrent(config);

            var phase2 = new JokerPhase(new Random(42));
            CardType c = CardType.Rock;
            CardType d = CardType.Paper;
            phase2.PreDuelModify(ref c, ref d, out bool swapped2);
            TestRunner.Assert(!swapped2, "B3 Config: 0% chance -> never swap");

            BalanceConfig.Reset();
        }

        // ============================================================
        // B4: 止戈相位
        // ============================================================

        static void TestB4_ScissorsConvertedToRock()
        {
            Setup();
            var phase = new CeasefirePhase();

            CardType a = CardType.Scissors;
            CardType b = CardType.Paper;
            phase.PreDuelModify(ref a, ref b, out bool swapped);

            TestRunner.AssertEqual(CardType.Rock, a, "B4: Scissors -> Rock (剪视为石)");
            TestRunner.AssertEqual(CardType.Paper, b, "B4: Paper unchanged");
            TestRunner.Assert(!swapped, "B4: conversion is not a swap");
        }

        static void TestB4_RockBeatsPaper()
        {
            Setup();
            var phase = new CeasefirePhase();
            TestRunner.AssertEqual(DuelOutcome.Win, phase.ResolveDuel(CardType.Rock, CardType.Paper),
                "B4: Rock beats Paper (撞晕)");
            TestRunner.AssertEqual(DuelOutcome.Lose, phase.ResolveDuel(CardType.Paper, CardType.Rock),
                "B4: Paper loses to Rock");
        }

        static void TestB4_ScissorsVsScissors_Draw()
        {
            Setup();
            var phase = new CeasefirePhase();

            CardType a = CardType.Scissors;
            CardType b = CardType.Scissors;
            phase.PreDuelModify(ref a, ref b, out bool _);

            TestRunner.AssertEqual(CardType.Rock, a, "B4 SvS: Scissors A -> Rock");
            TestRunner.AssertEqual(CardType.Rock, b, "B4 SvS: Scissors B -> Rock");
            TestRunner.AssertEqual(DuelOutcome.Draw, phase.ResolveDuel(a, b), "B4 SvS: Rock vs Rock = Draw");
        }

        static void TestB4_AllCardsPlayable()
        {
            var phase = new CeasefirePhase();
            TestRunner.Assert(phase.IsCardPlayable(CardType.Scissors), "B4: Scissors playable (converted)");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Rock), "B4: Rock playable");
            TestRunner.Assert(phase.IsCardPlayable(CardType.Paper), "B4: Paper playable");
        }

        // ============================================================
        // B5: 赌场 (天命赌场) — Multipliers from BalanceConfig
        // ============================================================

        static void TestB5_CritMultiplier_1_5x()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.WinnerMatched);
            TestRunner.AssertEqual(1.5f, reward, "B5 Crit: reward = 1.5x (from BalanceConfig)");
            TestRunner.AssertEqual(1.0f, penalty, "B5 Crit: penalty = 1.0x");
        }

        static void TestB5_CounterMultiplier_1_5x()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.DefenderMatched);
            TestRunner.AssertEqual(1.0f, reward, "B5 Counter: reward = 1.0x");
            TestRunner.AssertEqual(1.5f, penalty, "B5 Counter: penalty = 1.5x (from BalanceConfig)");
        }

        static void TestB5_ConquerHeaven_1_3x_Fixed()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.BothMatchedDraw);
            TestRunner.AssertEqual(1.3f, reward, "B5 ConquerHeaven: reward = 1.3x fixed (no random)");
            TestRunner.AssertEqual(1.0f, penalty, "B5 ConquerHeaven: penalty = 1.0x");

            // Verify it's always the same (not random)
            var phase2 = new DestinyGambitPhase(new Random(99));
            var (reward2, penalty2) = phase2.CalculateMultipliers(DestinyMatchType.BothMatchedDraw);
            TestRunner.AssertEqual(reward, reward2, "B5 ConquerHeaven: deterministic (same with different seed)");
        }

        static void TestB5_BadLuck_NoBonus()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.LoserMatched);
            TestRunner.AssertEqual(1.0f, reward, "B5 BadLuck: reward = 1.0x (no reduction)");
            TestRunner.AssertEqual(1.0f, penalty, "B5 BadLuck: penalty = 1.0x (no help)");
        }

        static void TestB5_ShengTianBanZi_Trigger()
        {
            Setup();
            var phase = new DestinyGambitPhase(new Random(42));

            var match = phase.ResolveDestinyMatch(
                DuelOutcome.Draw, CardType.Rock, CardType.Rock, CardType.Rock);
            TestRunner.AssertEqual(DestinyMatchType.BothMatchedDraw, match,
                "B5 ShengTianBanZi: draw + both match -> BothMatchedDraw");

            var effect = phase.ResolveDestinyEffect(DestinyMatchType.BothMatchedDraw);
            TestRunner.AssertEqual(DestinyEffectType.ShengTianBanZi, effect,
                "B5 ShengTianBanZi: effect type = ShengTianBanZi");

            // Draw but no match: different destiny card
            var match2 = phase.ResolveDestinyMatch(
                DuelOutcome.Draw, CardType.Rock, CardType.Rock, CardType.Paper);
            TestRunner.AssertEqual(DestinyMatchType.None, match2,
                "B5 ShengTianBanZi: draw + no match -> None");
        }

        // ============================================================
        // B6: 横幅优先级
        // ============================================================

        static DuelResultContext MakeCtx_ConquerHeaven()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Casino,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Rock, DefenderPlayed = CardType.Rock,
                IsDraw = true, WinnerId = null, LoserId = null,
                CasinoHouseCard = CardType.Rock,
                CasinoAttackerHitHouse = true, CasinoDefenderHitHouse = true,
                CasinoLoserHitHouse = false,
                CasinoConquerHeavenTriggered = true,
                MultiplierAppliedToWinner = 1.3f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static DuelResultContext MakeCtx_JesterSwap()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Jester,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Paper, DefenderPlayed = CardType.Rock,
                IsDraw = false, WinnerId = 1, LoserId = 2,
                JesterSwapTriggered = true,
                MultiplierAppliedToWinner = 1.0f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static DuelResultContext MakeCtx_DestinyCrit()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Casino,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Rock, DefenderPlayed = CardType.Scissors,
                IsDraw = false, WinnerId = 1, LoserId = 2,
                CasinoHouseCard = CardType.Rock,
                CasinoAttackerHitHouse = true, CasinoDefenderHitHouse = false,
                CasinoLoserHitHouse = false,
                MultiplierAppliedToWinner = 1.5f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static DuelResultContext MakeCtx_DestinyCounter()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Casino,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Scissors, DefenderPlayed = CardType.Rock,
                IsDraw = false, WinnerId = 2, LoserId = 1,
                CasinoHouseCard = CardType.Rock,
                CasinoAttackerHitHouse = false, CasinoDefenderHitHouse = true,
                CasinoLoserHitHouse = false,
                MultiplierAppliedToWinner = 1.0f, MultiplierAppliedToLoser = 1.5f
            };
        }

        static DuelResultContext MakeCtx_BadLuck()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Casino,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Rock, DefenderPlayed = CardType.Scissors,
                IsDraw = false, WinnerId = 1, LoserId = 2,
                CasinoHouseCard = CardType.Scissors,
                CasinoAttackerHitHouse = false, CasinoDefenderHitHouse = true,
                CasinoLoserHitHouse = true,
                MultiplierAppliedToWinner = 1.0f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static DuelResultContext MakeCtx_NormalWin()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Standard,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Rock, DefenderPlayed = CardType.Scissors,
                IsDraw = false, WinnerId = 1, LoserId = 2,
                MultiplierAppliedToWinner = 1.0f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static DuelResultContext MakeCtx_Draw()
        {
            return new DuelResultContext
            {
                Phase = DuelPhase.Standard,
                AttackerId = 1, DefenderId = 2,
                AttackerPlayed = CardType.Rock, DefenderPlayed = CardType.Rock,
                IsDraw = true, WinnerId = null, LoserId = null,
                MultiplierAppliedToWinner = 1.0f, MultiplierAppliedToLoser = 1.0f
            };
        }

        static void TestB6_P1_ConquerHeaven()
        {
            TestRunner.AssertEqual(UIBannerType.ConquerHeaven, DuelBannerResolver.PickBanner(MakeCtx_ConquerHeaven()),
                "B6 P1: ConquerHeaven");
        }

        static void TestB6_P2_JesterSwap()
        {
            TestRunner.AssertEqual(UIBannerType.JesterSwap, DuelBannerResolver.PickBanner(MakeCtx_JesterSwap()),
                "B6 P2: JesterSwap");
        }

        static void TestB6_P3_DestinyCrit()
        {
            TestRunner.AssertEqual(UIBannerType.DestinyCrit, DuelBannerResolver.PickBanner(MakeCtx_DestinyCrit()),
                "B6 P3: DestinyCrit");
        }

        static void TestB6_P4_DestinyCounter()
        {
            TestRunner.AssertEqual(UIBannerType.DestinyCounter, DuelBannerResolver.PickBanner(MakeCtx_DestinyCounter()),
                "B6 P4: DestinyCounter");
        }

        static void TestB6_P5_BadLuck()
        {
            TestRunner.AssertEqual(UIBannerType.BadLuck, DuelBannerResolver.PickBanner(MakeCtx_BadLuck()),
                "B6 P5: BadLuck");
        }

        static void TestB6_P6_NormalWin()
        {
            TestRunner.AssertEqual(UIBannerType.NormalWin, DuelBannerResolver.PickBanner(MakeCtx_NormalWin()),
                "B6 P6: NormalWin");
        }

        static void TestB6_P7_Draw()
        {
            TestRunner.AssertEqual(UIBannerType.Draw, DuelBannerResolver.PickBanner(MakeCtx_Draw()),
                "B6 P7: Draw");
        }

        static void TestB6_Priority_ConquerHeavenOverDraw()
        {
            var ctx = MakeCtx_ConquerHeaven();
            TestRunner.Assert(ctx.IsDraw, "B6 Priority: ConquerHeaven ctx is a draw");
            TestRunner.AssertEqual(UIBannerType.ConquerHeaven, DuelBannerResolver.PickBanner(ctx),
                "B6 Priority: P1 ConquerHeaven > P7 Draw");
        }

        static void TestB6_Priority_CritOverNormalWin()
        {
            var ctx = MakeCtx_DestinyCrit();
            TestRunner.Assert(ctx.WinnerId.HasValue, "B6 Priority: DestinyCrit has a winner");
            TestRunner.AssertEqual(UIBannerType.DestinyCrit, DuelBannerResolver.PickBanner(ctx),
                "B6 Priority: P3 DestinyCrit > P6 NormalWin");
        }

        static void TestB6_Adapter_AllPhases()
        {
            Setup();
            // DestinyGambit -> Casino
            var dataCasino = new DuelResultData
            {
                InitiatorId = 1, DefenderId = 2,
                InitiatorCard = CardType.Rock, DefenderCard = CardType.Rock,
                DestinyCard = CardType.Rock,
                Outcome = DuelOutcome.Draw,
                ActivePhase = PhaseType.DestinyGambit,
                IsShengTianBanZi = true,
                RewardMultiplier = 1.3f, PenaltyMultiplier = 1.0f
            };
            var ctxCasino = DuelResultAdapter.Convert(dataCasino);
            TestRunner.AssertEqual(DuelPhase.Casino, ctxCasino.Phase, "B6 Adapter: DestinyGambit -> Casino");
            TestRunner.Assert(ctxCasino.CasinoConquerHeavenTriggered, "B6 Adapter: ShengTianBanZi -> ConquerHeaven");

            // Joker -> Jester
            var dataJester = new DuelResultData
            {
                InitiatorId = 1, DefenderId = 2,
                InitiatorCard = CardType.Paper, DefenderCard = CardType.Rock,
                Outcome = DuelOutcome.Win,
                ActivePhase = PhaseType.Joker,
                CardsSwapped = true,
                RewardMultiplier = 1.0f, PenaltyMultiplier = 1.0f
            };
            var ctxJester = DuelResultAdapter.Convert(dataJester);
            TestRunner.AssertEqual(DuelPhase.Jester, ctxJester.Phase, "B6 Adapter: Joker -> Jester");
            TestRunner.Assert(ctxJester.JesterSwapTriggered, "B6 Adapter: CardsSwapped -> JesterSwap");

            // Ceasefire -> Peace
            var dataPeace = new DuelResultData
            {
                InitiatorId = 1, DefenderId = 2,
                InitiatorCard = CardType.Rock, DefenderCard = CardType.Paper,
                OriginalInitiatorCard = CardType.Scissors,
                Outcome = DuelOutcome.Win,
                ActivePhase = PhaseType.Ceasefire,
                ScissorsConverted = true,
                RewardMultiplier = 1.0f, PenaltyMultiplier = 1.0f
            };
            var ctxPeace = DuelResultAdapter.Convert(dataPeace);
            TestRunner.AssertEqual(DuelPhase.Peace, ctxPeace.Phase, "B6 Adapter: Ceasefire -> Peace");
            TestRunner.Assert(ctxPeace.PeaceRuleApplied, "B6 Adapter: ScissorsConverted -> PeaceRule");

            // InfiniteFirepower -> Overload
            var dataOverload = new DuelResultData
            {
                InitiatorId = 1, DefenderId = 2,
                InitiatorCard = CardType.Rock, DefenderCard = CardType.Scissors,
                Outcome = DuelOutcome.Win,
                ActivePhase = PhaseType.InfiniteFirepower,
                OverheatDuration = 5.0f,
                GhostProtectionGranted = true,
                RewardMultiplier = 1.0f, PenaltyMultiplier = 0.5f
            };
            var ctxOverload = DuelResultAdapter.Convert(dataOverload);
            TestRunner.AssertEqual(DuelPhase.Overload, ctxOverload.Phase, "B6 Adapter: InfiniteFirepower -> Overload");
            TestRunner.Assert(ctxOverload.OverloadAttackerOverheatApplied, "B6 Adapter: overheat applied");

            // Winner/Loser mapping
            TestRunner.AssertEqual(1, ctxOverload.WinnerId.Value, "B6 Adapter Win: winner = initiator");
            TestRunner.AssertEqual(2, ctxOverload.LoserId.Value, "B6 Adapter Win: loser = defender");
        }

        static void TestB6_Queue_SingleBanner()
        {
            var time = new ManualTimeProvider();
            var presenter = new SimpleDuelBannerPresenter();
            var queue = new DuelUIEventQueue(presenter, time);

            int bannerCount = 0;
            queue.OnBannerShown += (type) => bannerCount++;

            queue.Enqueue(MakeCtx_ConquerHeaven());
            queue.Tick();           // Idle -> WaitingDelay
            time.Advance(0.2f);
            queue.Tick();           // WaitingDelay -> ShowingBanner

            TestRunner.AssertEqual(1, bannerCount, "B6 Queue: exactly 1 banner per context");
            TestRunner.AssertEqual(UIBannerType.ConquerHeaven, presenter.CurrentBanner.Value,
                "B6 Queue: correct banner type");
        }

        static void TestB6_Queue_DelayAndDuration()
        {
            var time = new ManualTimeProvider();
            var presenter = new SimpleDuelBannerPresenter();
            var queue = new DuelUIEventQueue(presenter, time);

            queue.Enqueue(MakeCtx_NormalWin());

            // Delay check
            queue.Tick();
            TestRunner.Assert(!presenter.IsShowing, "B6 Queue Delay: not showing during delay");
            time.Advance(0.10f);
            queue.Tick();
            TestRunner.Assert(!presenter.IsShowing, "B6 Queue Delay: still not at 0.10s");
            time.Advance(0.06f);
            queue.Tick();
            TestRunner.Assert(presenter.IsShowing, "B6 Queue Delay: showing after 0.16s");

            // Duration check
            time.Advance(1.4f);
            queue.Tick();
            TestRunner.Assert(presenter.IsShowing, "B6 Queue Duration: still showing at 1.4s");
            time.Advance(0.2f);
            queue.Tick();
            TestRunner.Assert(!presenter.IsShowing, "B6 Queue Duration: auto-hidden after 1.5s");
        }

        static void TestB6_Queue_SkipNonDeadlock()
        {
            var time = new ManualTimeProvider();
            var presenter = new SimpleDuelBannerPresenter();
            var queue = new DuelUIEventQueue(presenter, time);

            queue.Enqueue(MakeCtx_ConquerHeaven());
            queue.Enqueue(MakeCtx_NormalWin());

            // Show first
            queue.Tick();
            time.Advance(0.2f);
            queue.Tick();
            TestRunner.AssertEqual(UIBannerType.ConquerHeaven, presenter.CurrentBanner.Value,
                "B6 Skip: first = ConquerHeaven");

            // Skip first
            queue.Skip();
            TestRunner.Assert(!presenter.IsShowing, "B6 Skip: first skipped");

            // Process second
            queue.Tick();
            queue.Tick();
            time.Advance(0.2f);
            queue.Tick();
            TestRunner.Assert(presenter.IsShowing, "B6 Skip: second showing (no deadlock)");
            TestRunner.AssertEqual(UIBannerType.NormalWin, presenter.CurrentBanner.Value,
                "B6 Skip: second = NormalWin");
        }

        static void TestB6_Presenter_ShowHideSkip()
        {
            var presenter = new SimpleDuelBannerPresenter();
            TestRunner.Assert(!presenter.IsShowing, "B6 Presenter: not showing initially");

            presenter.Show(UIBannerType.NormalWin, 1.5f, true);
            TestRunner.Assert(presenter.IsShowing, "B6 Presenter: showing after Show()");
            TestRunner.AssertEqual(UIBannerType.NormalWin, presenter.CurrentBanner.Value,
                "B6 Presenter: correct type");

            presenter.Hide();
            TestRunner.Assert(!presenter.IsShowing, "B6 Presenter: hidden after Hide()");

            // Skippable
            presenter.Show(UIBannerType.Draw, 1.5f, true);
            presenter.Skip();
            TestRunner.Assert(!presenter.IsShowing, "B6 Presenter: skip works when skippable");

            // Not skippable
            presenter.Show(UIBannerType.Draw, 1.5f, false);
            presenter.Skip();
            TestRunner.Assert(presenter.IsShowing, "B6 Presenter: skip ignored when not skippable");
            presenter.Hide();

            // Banner texts
            TestRunner.AssertEqual("胜天半子！", SimpleDuelBannerPresenter.GetBannerText(UIBannerType.ConquerHeaven),
                "B6 Text: ConquerHeaven");
            TestRunner.AssertEqual("天命暴击！", SimpleDuelBannerPresenter.GetBannerText(UIBannerType.DestinyCrit),
                "B6 Text: DestinyCrit");
            TestRunner.AssertEqual("平局！", SimpleDuelBannerPresenter.GetBannerText(UIBannerType.Draw),
                "B6 Text: Draw");
        }

        // ============================================================
        // B7: 火力过热
        // ============================================================

        static void TestB7_ChargeMultiplier_2_5x()
        {
            Setup();
            var phase = new InfiniteFirepowerPhase();
            TestRunner.AssertEqual(2.5f, phase.GetChargeSpeedMultiplier(),
                "B7: 2.5x charge speed");
        }

        static void TestB7_OverheatBlocks5s()
        {
            Setup();
            var time = new ManualTimeProvider();
            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);

            TestRunner.Assert(!skill.IsOverheated, "B7 Overheat: not overheated initially");
            skill.StartOverheat(5.0f);
            TestRunner.Assert(skill.IsOverheated, "B7 Overheat: overheated after StartOverheat");

            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(!activated, "B7 Overheat: activation blocked while overheated");

            time.Advance(4.9f);
            TestRunner.Assert(skill.IsOverheated, "B7 Overheat: still overheated at 4.9s");
        }

        static void TestB7_OverheatExpiresAndRecovers()
        {
            Setup();
            var time = new ManualTimeProvider();
            time.CurrentTime = 10f;
            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);
            skill.StartOverheat(5.0f);

            time.Advance(5.1f); // t=15.1
            TestRunner.Assert(!skill.IsOverheated, "B7 Recover: overheat expired at 5.1s");

            // Can activate after overheat expires
            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(activated, "B7 Recover: activation succeeds after overheat expires");
        }

        static void TestB7_GhostPenaltyReduction()
        {
            Setup();
            var phase = new InfiniteFirepowerPhase();
            var (reward, penalty) = phase.CalculateMultipliers(DestinyMatchType.None);
            TestRunner.Assert(penalty <= 0.5f,
                $"B7 Ghost: penalty reduced to <= 0.5 (actual: {penalty})");

            var result = new DuelResultData();
            phase.PostDuelEffect(result);
            TestRunner.AssertEqual(5.0f, result.OverheatDuration,
                "B7 PostDuel: overheat duration = 5s");
        }

        // ============================================================
        // Config: BalanceConfig
        // ============================================================

        static void TestConfig_DefaultValues()
        {
            Setup();
            var config = BalanceConfig.Current;
            TestRunner.AssertEqual(3, config.MaxSkillSlots, "Config: MaxSkillSlots = 3");
            TestRunner.AssertEqual(2, config.MaxHandSlots, "Config: MaxHandSlots = 2");
            TestRunner.AssertEqual(4.0f, config.OverflowLifetime, "Config: OverflowLifetime = 4.0");
            TestRunner.AssertEqual(7.0f, config.P1_ImmunityDuration, "Config: P1_ImmunityDuration = 7.0");
            TestRunner.AssertEqual(1.5f, config.CritMultiplier, "Config: CritMultiplier = 1.5");
            TestRunner.AssertEqual(1.5f, config.CounterMultiplier, "Config: CounterMultiplier = 1.5");
            TestRunner.AssertEqual(1.3f, config.ConquerHeavenMultiplier, "Config: ConquerHeavenMultiplier = 1.3");
            TestRunner.AssertEqual(0.20f, config.JesterTriggerChance, "Config: JesterTriggerChance = 0.20");
            TestRunner.AssertEqual(2.5f, config.OverloadChargeMultiplier, "Config: OverloadChargeMultiplier = 2.5");
            TestRunner.AssertEqual(5.0f, config.AttackerOverheatTime, "Config: AttackerOverheatTime = 5.0");
            TestRunner.AssertEqual(1.5f, config.RockStunDuration, "Config: RockStunDuration = 1.5");
        }

        static void TestConfig_HotReload_VersionIncrement()
        {
            Setup();
            int v1 = BalanceConfig.Current.ConfigVersion;

            BalanceConfig.ReloadBalanceConfig();
            int v2 = BalanceConfig.Current.ConfigVersion;

            TestRunner.Assert(v2 > v1, $"Config HotReload: version incremented ({v1} -> {v2})");

            BalanceConfig.ReloadBalanceConfig();
            int v3 = BalanceConfig.Current.ConfigVersion;
            TestRunner.Assert(v3 > v2, $"Config HotReload: version incremented again ({v2} -> {v3})");

            BalanceConfig.Reset();
        }

        static void TestConfig_CustomConfig_AffectsPhase()
        {
            Setup();
            // Override CritMultiplier to 3.0
            var config = new BalanceConfig();
            config.CritMultiplier = 3.0f;
            config.ConquerHeavenMultiplier = 2.0f;
            BalanceConfig.SetCurrent(config);

            var phase = new DestinyGambitPhase(new Random(42));
            var (reward, p1) = phase.CalculateMultipliers(DestinyMatchType.WinnerMatched);
            TestRunner.AssertEqual(3.0f, reward, "Config Custom: CritMultiplier = 3.0 affects DestinyGambit");

            var (reward2, p2) = phase.CalculateMultipliers(DestinyMatchType.BothMatchedDraw);
            TestRunner.AssertEqual(2.0f, reward2, "Config Custom: ConquerHeavenMultiplier = 2.0 affects DestinyGambit");

            BalanceConfig.Reset();
        }

        static void TestConfig_WeightedPhaseSelection()
        {
            Setup();
            var config = BalanceConfig.Current;

            // Check phase weights
            TestRunner.AssertEqual(50, config.GetPhaseWeight(PhaseType.DestinyGambit),
                "Config Weights: Casino = 50");
            TestRunner.AssertEqual(20, config.GetPhaseWeight(PhaseType.Ceasefire),
                "Config Weights: Peace = 20");
            TestRunner.AssertEqual(15, config.GetPhaseWeight(PhaseType.Joker),
                "Config Weights: Jester = 15");
            TestRunner.AssertEqual(15, config.GetPhaseWeight(PhaseType.InfiniteFirepower),
                "Config Weights: Overload = 15");

            // Weighted selection: run many times, all phases should appear
            var phaseCounts = new Dictionary<PhaseType, int>();
            for (int i = 0; i < 200; i++)
            {
                var pm = new PhaseManager(new Random(i));
                pm.LockRandomPhase();
                var pt = pm.ActivePhaseType.Value;
                if (!phaseCounts.ContainsKey(pt)) phaseCounts[pt] = 0;
                phaseCounts[pt]++;
                pm.Reset();
            }

            TestRunner.Assert(phaseCounts.ContainsKey(PhaseType.DestinyGambit),
                "Config Weights: DestinyGambit appears in random selection");
            TestRunner.Assert(phaseCounts.ContainsKey(PhaseType.Ceasefire),
                "Config Weights: Ceasefire appears in random selection");
            TestRunner.Assert(phaseCounts.ContainsKey(PhaseType.Joker),
                "Config Weights: Joker appears in random selection");
            TestRunner.Assert(phaseCounts.ContainsKey(PhaseType.InfiniteFirepower),
                "Config Weights: InfiniteFirepower appears in random selection");

            // Casino (weight 50) should be most frequent
            TestRunner.Assert(phaseCounts[PhaseType.DestinyGambit] > phaseCounts[PhaseType.Joker],
                "Config Weights: Casino more frequent than Jester");
        }

        // ============================================================
        // DuelLog
        // ============================================================

        static void TestDuelLog_6StagesPerDuel()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            gm.HandleDrift(1, 300f);
            gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            // Should have at least 6 log entries (one per stage)
            TestRunner.Assert(DuelLog.LogBuffer.Count >= 6,
                $"DuelLog: >= 6 entries per duel (actual: {DuelLog.LogBuffer.Count})");

            // Check stage names present
            var stages = new[] { "Trigger", "Validate", "ConsumeTicket", "Lock", "PhaseEvent", "ResultApply" };
            foreach (var stage in stages)
            {
                var filtered = DuelLog.Filter(stage);
                TestRunner.Assert(filtered.Count >= 1,
                    $"DuelLog: Stage={stage} present in log");
            }

            gm.EndGame();
            gm.Dispose();
        }

        static void TestDuelLog_Filter()
        {
            Setup();
            DuelLog.LogTrigger(1, 2, "DestinyGambit");
            DuelLog.LogValidate(1, 2, "DestinyGambit", true, true, true, true, null);
            DuelLog.LogPhaseEvent(1, 2, "DestinyGambit", "ConquerHeaven=true");

            var all = DuelLog.LogBuffer;
            TestRunner.AssertEqual(3, all.Count, "DuelLog Filter: 3 entries total");

            var triggerOnly = DuelLog.Filter("Trigger");
            TestRunner.AssertEqual(1, triggerOnly.Count, "DuelLog Filter: 1 Trigger entry");

            var conquer = DuelLog.Filter("ConquerHeaven");
            TestRunner.AssertEqual(1, conquer.Count, "DuelLog Filter: 1 ConquerHeaven entry");

            var none = DuelLog.Filter("NonExistent");
            TestRunner.AssertEqual(0, none.Count, "DuelLog Filter: 0 for non-existent keyword");
        }

        static void TestDuelLog_DisableToggle()
        {
            Setup();
            DuelLog.Enabled = false;
            DuelLog.LogTrigger(1, 2, "Test");
            TestRunner.AssertEqual(0, DuelLog.LogBuffer.Count, "DuelLog Disable: no entries when disabled");

            DuelLog.Enabled = true;
            DuelLog.LogTrigger(1, 2, "Test");
            TestRunner.AssertEqual(1, DuelLog.LogBuffer.Count, "DuelLog Enable: entry added when enabled");
        }

        // ============================================================
        // DebugHUD
        // ============================================================

        static void TestDebugHUD_Toggle()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            var hud = new DebugHUD(gm);

            TestRunner.Assert(!hud.IsVisible, "HUD: not visible initially");
            TestRunner.AssertEqual("", hud.Render(1, 2), "HUD: empty render when not visible");

            hud.Toggle();
            TestRunner.Assert(hud.IsVisible, "HUD: visible after Toggle()");
            TestRunner.Assert(hud.Render(1, 2).Length > 0, "HUD: non-empty render when visible");

            hud.Toggle();
            TestRunner.Assert(!hud.IsVisible, "HUD: hidden after second Toggle()");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestDebugHUD_RenderPlayerState()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            var hud = new DebugHUD(gm);
            string state = hud.RenderPlayerState(1);

            TestRunner.Assert(state.Contains("Player 1"), "HUD State: contains player id");
            TestRunner.Assert(state.Contains("SkillSlots"), "HUD State: contains SkillSlots");
            TestRunner.Assert(state.Contains("HandCards"), "HUD State: contains HandCards");
            TestRunner.Assert(state.Contains("Overflow"), "HUD State: contains Overflow");
            TestRunner.Assert(state.Contains("P1Immunity"), "HUD State: contains P1Immunity");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestDebugHUD_RenderLastDuel()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            var hud = new DebugHUD(gm);

            // No duel yet
            string noDuel = hud.RenderLastDuel();
            TestRunner.Assert(noDuel.Contains("No duel"), "HUD LastDuel: shows 'No duel' initially");

            // After setting duel result
            var ctx = MakeCtx_ConquerHeaven();
            hud.SetLastDuelResult(ctx, UIBannerType.ConquerHeaven);
            string lastDuel = hud.RenderLastDuel();

            TestRunner.Assert(lastDuel.Contains("Last Duel"), "HUD LastDuel: contains 'Last Duel'");
            TestRunner.Assert(lastDuel.Contains("Casino"), "HUD LastDuel: contains phase");
            TestRunner.Assert(lastDuel.Contains("ConquerHeaven"), "HUD LastDuel: contains banner type");
            TestRunner.Assert(lastDuel.Contains("ConfigVersion"), "HUD LastDuel: contains ConfigVersion");

            gm.EndGame();
            gm.Dispose();
        }

        // ============================================================
        // Integration tests
        // ============================================================

        static void TestIntegration_FullDuelFlow()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.DestinyGambit);

            gm.HandleDrift(1, 300f);

            var p1 = gm.GetPlayerSession(1);
            TestRunner.Assert(p1.SkillSlots.IsFullyCharged, "Integration: player 1 fully charged");

            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);

            TestRunner.Assert(result != null, "Integration: duel result not null");
            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome, "Integration: Rock beats Scissors");
            TestRunner.AssertEqual(PhaseType.DestinyGambit, result.ActivePhase, "Integration: correct phase");
            TestRunner.Assert(!p1.SkillSlots.IsFullyCharged, "Integration: skill slots consumed");
            TestRunner.AssertEqual(1, p1.Cards.DarkCardCount, "Integration: one dark card consumed (2->1)");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestIntegration_InfiniteFirepowerFlow()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.InfiniteFirepower);

            var p1 = gm.GetPlayerSession(1);
            var p2 = gm.GetPlayerSession(2);

            // 2.5x charge speed
            TestRunner.AssertEqual(2.5f, p1.SkillSlots.ChargeSpeedMultiplier,
                "Integration InfFire: 2.5x charge applied");

            // Ghost tracker initialized
            TestRunner.Assert(p2.GhostTracker != null, "Integration InfFire: ghost tracker exists");

            // 120 * 2.5 = 300 -> full
            gm.HandleDrift(1, 120f);
            TestRunner.Assert(p1.SkillSlots.IsFullyCharged, "Integration InfFire: 120 drift = full");

            var result = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Scissors);
            TestRunner.Assert(result != null, "Integration InfFire: duel executed");

            // Overheat applied
            TestRunner.Assert(p1.SkillSlots.IsOverheated, "Integration InfFire: overheated");
            TestRunner.AssertEqual(5.0f, result.OverheatDuration, "Integration InfFire: 5s overheat");

            // Can't duel while overheated
            gm.HandleDrift(1, 120f);
            var result2 = gm.TryInitiateDuel(1, 2, CardType.Rock, CardType.Paper);
            TestRunner.Assert(result2 == null, "Integration InfFire: blocked while overheated");

            // Overheat expires
            time.Advance(5.1f);
            TestRunner.Assert(!p1.SkillSlots.IsOverheated, "Integration InfFire: overheat expired");

            gm.EndGame();
            gm.Dispose();
        }

        static void TestIntegration_CeasefireFlow()
        {
            Setup();
            var time = new ManualTimeProvider();
            var gm = new GameManager(new Random(42), time);
            gm.InitializeGameWithPhase(new[] { 1, 2 }, PhaseType.Ceasefire);

            gm.HandleDrift(1, 300f);

            var result = gm.TryInitiateDuel(1, 2, CardType.Scissors, CardType.Paper);
            TestRunner.Assert(result != null, "Integration Ceasefire: duel executed");
            TestRunner.AssertEqual(CardType.Rock, result.InitiatorCard, "Integration Ceasefire: Scissors -> Rock");
            TestRunner.AssertEqual(CardType.Scissors, result.OriginalInitiatorCard,
                "Integration Ceasefire: original preserved");
            TestRunner.Assert(result.ScissorsConverted, "Integration Ceasefire: ScissorsConverted flag");
            TestRunner.AssertEqual(DuelOutcome.Win, result.Outcome, "Integration Ceasefire: Rock beats Paper");

            gm.EndGame();
            gm.Dispose();
        }

        // ============================================================
        // SkillSlot fundamentals
        // ============================================================

        static void TestSkillSlot_Charging()
        {
            Setup();
            var skill = new SkillSlotManager(1);
            skill.AddCharge(50f);
            TestRunner.AssertEqual(0, skill.FilledSlots, "SkillSlot: 50 charge = 0 slots");
            TestRunner.Assert(skill.CurrentCharge == 50f, "SkillSlot: charge = 50");
        }

        static void TestSkillSlot_FullCharge()
        {
            Setup();
            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            TestRunner.AssertEqual(3, skill.FilledSlots, "SkillSlot: 300 charge = 3 slots");
            TestRunner.Assert(skill.IsFullyCharged, "SkillSlot: IsFullyCharged = true");
        }

        static void TestSkillSlot_Activation()
        {
            Setup();
            var skill = new SkillSlotManager(1);
            skill.AddCharge(300f);
            bool activated = skill.TryActivateSkill();
            TestRunner.Assert(activated, "SkillSlot: activation succeeds when full");
            TestRunner.AssertEqual(0, skill.FilledSlots, "SkillSlot: slots reset after activation");

            bool again = skill.TryActivateSkill();
            TestRunner.Assert(!again, "SkillSlot: activation fails when not full");
        }

        static void TestSkillSlot_ChargeSpeedMultiplier()
        {
            Setup();
            var skill = new SkillSlotManager(1);
            skill.ChargeSpeedMultiplier = 2.0f;
            skill.AddCharge(50f); // effective = 100
            TestRunner.AssertEqual(1, skill.FilledSlots, "SkillSlot: 2x multiplier doubles charge");
        }

        static void TestSkillSlot_TimedOverheat()
        {
            Setup();
            var time = new ManualTimeProvider();
            var skill = new SkillSlotManager(1, time);
            skill.AddCharge(300f);

            skill.StartOverheat(5.0f);
            TestRunner.Assert(skill.IsOverheated, "SkillSlot Overheat: overheated");
            TestRunner.Assert(!skill.TryActivateSkill(), "SkillSlot Overheat: activation blocked");

            time.Advance(5.1f);
            TestRunner.Assert(!skill.IsOverheated, "SkillSlot Overheat: expired after 5.1s");
            TestRunner.Assert(skill.TryActivateSkill(), "SkillSlot Overheat: activation works after expiry");
        }

        // ============================================================
        // PhaseManager
        // ============================================================

        static void TestPhaseManager_LockAndMutualExclusion()
        {
            Setup();
            var pm = new PhaseManager(new Random(42));
            var phase = pm.LockRandomPhase();
            TestRunner.Assert(phase != null, "PhaseManager: lock returns phase");
            TestRunner.Assert(pm.IsLocked, "PhaseManager: IsLocked = true");

            bool threw = false;
            try { pm.LockRandomPhase(); }
            catch (InvalidOperationException) { threw = true; }
            TestRunner.Assert(threw, "PhaseManager: double lock throws (mutual exclusion)");
        }

        static void TestPhaseManager_Reset()
        {
            Setup();
            var pm = new PhaseManager(new Random(42));
            pm.LockRandomPhase();
            pm.Reset();
            TestRunner.Assert(!pm.IsLocked, "PhaseManager: IsLocked = false after reset");
            var phase = pm.LockRandomPhase();
            TestRunner.Assert(phase != null, "PhaseManager: can lock again after reset");
        }

        /// <summary>
        /// 程序入口
        /// </summary>
        public static void Main(string[] args)
        {
            RunAll();
            Task5IntegrationTests.RunAll();
            TestRunner.PrintSummary();
            Environment.Exit(TestRunner.FailedCount > 0 ? 1 : 0);
        }
    }
}
