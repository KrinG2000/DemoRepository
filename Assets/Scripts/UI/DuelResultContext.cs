// ============================================================
// DuelResultContext.cs — 对决结算上下文
// 架构层级: UI
// 说明: 封装一次对决的结算上下文数据,包含判定结果和相位事件,
//       供UI Banner系统进行优先级选择和展示
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.UI
{
    /// <summary>
    /// 对决相位类型 (UI层命名)
    /// </summary>
    public enum DuelPhase
    {
        Standard,   // 无特殊相位
        Casino,     // 天命赌场 (DestinyGambit)
        Jester,     // 小丑相位 (Joker)
        Overload,   // 无限火力 (InfiniteFirepower)
        Peace       // 止戈相位 (Ceasefire)
    }

    /// <summary>
    /// 一次对决的结算上下文 — 包含判定结果 + 相位事件
    /// </summary>
    public class DuelResultContext
    {
        // ---- 基础信息 ----
        public DuelPhase Phase;
        public int AttackerId;
        public int DefenderId;
        public CardType AttackerPlayed;      // 最终用于结算的牌 (相位修改后)
        public CardType DefenderPlayed;      // 最终用于结算的牌 (相位修改后)
        public bool IsDraw;
        public int? WinnerId;                // 平局则null
        public int? LoserId;                 // 平局则null

        // ---- Casino (天命赌场) 专属 ----
        public CardType? CasinoHouseCard;               // 天命牌 (仅Casino)
        public bool CasinoAttackerHitHouse;              // 攻击者压中天命牌
        public bool CasinoDefenderHitHouse;              // 防守方压中天命牌
        public bool CasinoLoserHitHouse;                 // 败者压中天命牌 (平局时false)
        public bool CasinoConquerHeavenTriggered;        // 胜天半子触发

        // ---- Jester (小丑相位) 专属 ----
        public bool JesterSwapTriggered;                 // 小丑互换触发

        // ---- Peace (止戈相位) 专属 ----
        public bool PeaceRuleApplied;                    // 剪→石规则应用

        // ---- Overload (无限火力) 专属 ----
        public bool OverloadAttackerOverheatApplied;     // 攻击者过热应用

        // ---- 全局 ----
        public bool VictimImmunityApplied;               // 受害者免疫应用
        public bool VictimGhostTriggered;                // 幽灵保护触发 (仅Overload)

        // ---- 倍率 ----
        public float MultiplierAppliedToWinner;          // 赢家奖励系数 (平局时用于双方)
        public float MultiplierAppliedToLoser;           // 败者惩罚系数
    }
}
