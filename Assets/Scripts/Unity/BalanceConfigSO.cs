// ============================================================
// BalanceConfigSO.cs — ScriptableObject 版平衡配置
// 架构层级: Unity Bridge
// 说明: 纯C# BalanceConfig 的 Unity ScriptableObject 包装。
//       在Project面板右键 Create → RacingCardGame → Balance Config
//       创建 .asset 文件后拖入 GameBootstrap Inspector 即可。
//       OnEnable 时自动将值推送到 BalanceConfig.Current 单例。
//
// 用法:
//   1. Project 面板右键 → Create → RacingCardGame → Balance Config
//   2. 在 Inspector 中调整参数 (默认值已填好)
//   3. 拖入 GameBootstrap 的 Balance Config 字段
//   4. 运行时自动生效; 也可在 Play Mode 中修改并调用 Apply()
// ============================================================

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using RacingCardGame.Config;

namespace RacingCardGame.Unity
{
    [CreateAssetMenu(fileName = "BalanceConfigSO", menuName = "RacingCardGame/Balance Config")]
    public class BalanceConfigSO : ScriptableObject
    {
        // ==== Core ====
        [Header("Core")]
        public int MaxSkillSlots = 3;
        public int MaxHandSlots = 2;
        public float OverflowLifetime = 4.0f;
        public float P1_ImmunityDuration = 7.0f;
        public float BaseDriftChargeSpeed = 1.0f;

        // ==== Phase Weights ====
        [Header("Phase Weights (sum need not be 100)")]
        public int CasinoWeight = 50;
        public int PeaceWeight = 20;
        public int JesterWeight = 15;
        public int OverloadWeight = 15;

        // ==== Casino (DestinyGambit) ====
        [Header("Casino / DestinyGambit")]
        public float CritMultiplier = 1.5f;
        public float CounterMultiplier = 1.5f;
        public float ConquerHeavenMultiplier = 1.3f;

        // ==== Jester ====
        [Header("Jester")]
        [Range(0f, 1f)]
        public float JesterTriggerChance = 0.20f;

        // ==== Overload (InfiniteFirepower) ====
        [Header("Overload / InfiniteFirepower")]
        public float OverloadChargeMultiplier = 2.5f;
        public float AttackerOverheatTime = 5.0f;
        public int GhostTriggerCount = 3;
        public float GhostCheckWindow = 45.0f;
        public float GhostDuration = 20.0f;
        [Range(0f, 1f)]
        public float GhostPenaltyReduction = 0.5f;

        // ==== Peace (Ceasefire) ====
        [Header("Peace / Ceasefire")]
        public float RockStunDuration = 1.5f;

        // ==== Track & Simulation ====
        [Header("Track & Simulation")]
        public float TrackLength = 1000f;
        public float BaseCarSpeed = 50f;
        [Tooltip("Charge per second while drifting (~3s to fill 1 slot at 33.3)")]
        public float DriftChargeBaseRate = 33.3f;
        public float CardPickupInterval = 100f;
        public int CardPickupCount = 8;

        // ==== Duel Flow ====
        [Header("Duel Flow")]
        public float DuelPickTimeLimit = 2.5f;
        public float DuelRaycastRange = 30f;

        // ==== Winner Rewards ====
        [Header("Winner Rewards (by card type)")]
        public float ScissorsWinSpeedMultiplier = 1.35f;
        public float ScissorsWinDuration = 3.0f;
        public float PaperWinSpeedMultiplier = 1.40f;
        public float PaperWinDuration = 2.5f;
        public float RockWinSuperArmorDuration = 4.0f;

        // ==== Loser Penalties ====
        [Header("Loser Penalties (by winner card type)")]
        public float ScissorsLoseFrictionMultiplier = 0.5f;
        public float ScissorsLoseFrictionDuration = 2.0f;
        public float ScissorsLoseSpeedCap = 0.7f;
        public float ScissorsLoseCapDuration = 2.0f;
        public float PaperLoseSlowMultiplier = 0.9f;
        public float PaperLoseSlowDuration = 1.5f;
        // Rock lose: no penalty

        // ==== AI ====
        [Header("AI")]
        public float AIDuelCheckInterval = 5.0f;
        [Range(0f, 1f)]
        public float AIDuelChance = 0.3f;
        public float AIMinDuelDistance = 10f;
        public float AIMaxDuelDistance = 30f;

        /// <summary>
        /// 将所有字段推送到 BalanceConfig.Current 单例。
        /// 在 OnEnable 时自动调用; 也可手动调用以热更。
        /// </summary>
        public void Apply()
        {
            var c = new BalanceConfig();

            // Core
            c.MaxSkillSlots = MaxSkillSlots;
            c.MaxHandSlots = MaxHandSlots;
            c.OverflowLifetime = OverflowLifetime;
            c.P1_ImmunityDuration = P1_ImmunityDuration;
            c.BaseDriftChargeSpeed = BaseDriftChargeSpeed;

            // Phase Weights
            c.CasinoWeight = CasinoWeight;
            c.PeaceWeight = PeaceWeight;
            c.JesterWeight = JesterWeight;
            c.OverloadWeight = OverloadWeight;

            // Casino
            c.CritMultiplier = CritMultiplier;
            c.CounterMultiplier = CounterMultiplier;
            c.ConquerHeavenMultiplier = ConquerHeavenMultiplier;

            // Jester
            c.JesterTriggerChance = JesterTriggerChance;

            // Overload
            c.OverloadChargeMultiplier = OverloadChargeMultiplier;
            c.AttackerOverheatTime = AttackerOverheatTime;
            c.GhostTriggerCount = GhostTriggerCount;
            c.GhostCheckWindow = GhostCheckWindow;
            c.GhostDuration = GhostDuration;
            c.GhostPenaltyReduction = GhostPenaltyReduction;

            // Peace
            c.RockStunDuration = RockStunDuration;

            // Track
            c.TrackLength = TrackLength;
            c.BaseCarSpeed = BaseCarSpeed;
            c.DriftChargeBaseRate = DriftChargeBaseRate;
            c.CardPickupInterval = CardPickupInterval;
            c.CardPickupCount = CardPickupCount;

            // Duel Flow
            c.DuelPickTimeLimit = DuelPickTimeLimit;
            c.DuelRaycastRange = DuelRaycastRange;

            // Winner Rewards
            c.ScissorsWinSpeedMultiplier = ScissorsWinSpeedMultiplier;
            c.ScissorsWinDuration = ScissorsWinDuration;
            c.PaperWinSpeedMultiplier = PaperWinSpeedMultiplier;
            c.PaperWinDuration = PaperWinDuration;
            c.RockWinSuperArmorDuration = RockWinSuperArmorDuration;

            // Loser Penalties
            c.ScissorsLoseFrictionMultiplier = ScissorsLoseFrictionMultiplier;
            c.ScissorsLoseFrictionDuration = ScissorsLoseFrictionDuration;
            c.ScissorsLoseSpeedCap = ScissorsLoseSpeedCap;
            c.ScissorsLoseCapDuration = ScissorsLoseCapDuration;
            c.PaperLoseSlowMultiplier = PaperLoseSlowMultiplier;
            c.PaperLoseSlowDuration = PaperLoseSlowDuration;

            // AI
            c.AIDuelCheckInterval = AIDuelCheckInterval;
            c.AIDuelChance = AIDuelChance;
            c.AIMinDuelDistance = AIMinDuelDistance;
            c.AIMaxDuelDistance = AIMaxDuelDistance;

            BalanceConfig.SetCurrent(c);
            Debug.Log("[BalanceConfigSO] Config applied to BalanceConfig.Current");
        }

        /// <summary>
        /// ScriptableObject 加载时自动推送值
        /// </summary>
        private void OnEnable()
        {
            Apply();
        }

        /// <summary>
        /// Inspector 中修改值后立即生效 (Editor only)
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
                Apply();
        }
    }
}
#endif
