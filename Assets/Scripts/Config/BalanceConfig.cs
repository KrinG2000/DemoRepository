// ============================================================
// BalanceConfig.cs — 平衡配置 (ScriptableObject 等价物)
// 架构层级: Config
// 说明: 集中管理所有可调参数。支持运行时热更(ReloadBalanceConfig)。
//       在Unity中可替换为ScriptableObject资产。
// ============================================================

namespace RacingCardGame.Config
{
    public class BalanceConfig
    {
        private static BalanceConfig _current;
        public static BalanceConfig Current
        {
            get { return _current ?? (_current = new BalanceConfig()); }
        }

        /// <summary>
        /// 配置版本号 — 每次Reload递增,Debug HUD可显示确认是否生效
        /// </summary>
        public int ConfigVersion = 1;

        // ==== Core ====
        public int MaxSkillSlots = 3;
        public int MaxHandSlots = 2;
        public float OverflowLifetime = 4.0f;
        public float P1_ImmunityDuration = 7.0f;
        public float BaseDriftChargeSpeed = 1.0f;

        // ==== Phase Weights (权重,总和不必为100) ====
        public int CasinoWeight = 50;
        public int PeaceWeight = 20;
        public int JesterWeight = 15;
        public int OverloadWeight = 15;

        // ==== Casino (天命赌场 / DestinyGambit) ====
        public float CritMultiplier = 1.5f;
        public float CounterMultiplier = 1.5f;
        public float ConquerHeavenMultiplier = 1.3f;

        // ==== Jester (小丑相位 / Joker) ====
        public float JesterTriggerChance = 0.20f;

        // ==== Overload (无限火力 / InfiniteFirepower) ====
        public float OverloadChargeMultiplier = 2.5f;
        public float AttackerOverheatTime = 5.0f;
        public int GhostTriggerCount = 3;
        public float GhostCheckWindow = 45.0f;
        public float GhostDuration = 20.0f;
        public float GhostPenaltyReduction = 0.5f;

        // ==== Peace (止戈相位 / Ceasefire) ====
        public float RockStunDuration = 1.5f;

        /// <summary>
        /// 替换当前配置实例 (用于测试或运行时修改)
        /// </summary>
        public static void SetCurrent(BalanceConfig config)
        {
            _current = config;
        }

        /// <summary>
        /// 热更: 重新加载默认配置并递增版本号
        /// 在Unity中,此方法会从ScriptableObject资产重新读取
        /// </summary>
        public static void ReloadBalanceConfig()
        {
            int prevVersion = (_current != null) ? _current.ConfigVersion : 0;
            _current = new BalanceConfig();
            _current.ConfigVersion = prevVersion + 1;
        }

        /// <summary>
        /// 重置为null (测试清理用)
        /// </summary>
        public static void Reset()
        {
            _current = null;
        }

        /// <summary>
        /// 获取指定相位的权重
        /// </summary>
        public int GetPhaseWeight(Core.PhaseType phase)
        {
            switch (phase)
            {
                case Core.PhaseType.DestinyGambit: return CasinoWeight;
                case Core.PhaseType.Ceasefire: return PeaceWeight;
                case Core.PhaseType.Joker: return JesterWeight;
                case Core.PhaseType.InfiniteFirepower: return OverloadWeight;
                default: return 0;
            }
        }
    }
}
