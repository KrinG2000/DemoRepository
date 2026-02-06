// ============================================================
// GameEnums.cs — 核心枚举定义
// 架构层级: Core (基础层, 无依赖)
// 说明: 定义游戏中所有枚举类型,被其他所有模块引用
// ============================================================

namespace RacingCardGame.Core
{
    /// <summary>
    /// 卡牌类型 — 子空间对决中使用的三种基本牌型
    /// </summary>
    public enum CardType
    {
        Rock,       // 石头
        Scissors,   // 剪刀
        Paper       // 布
    }

    /// <summary>
    /// 相位类型 — 每局游戏随机锁定一个,互斥生效
    /// </summary>
    public enum PhaseType
    {
        DestinyGambit,      // 天命赌场: 赢家压中天命暴击,防守方压中反杀
        Joker,              // 小丑相位: 20%概率互换双方出牌
        Ceasefire,          // 止戈相位: 禁用剪刀,石头克布
        InfiniteFirepower   // 无限火力: 充能加速,发起者过热,受害者幽灵保护
    }

    /// <summary>
    /// 对决基础结果 (不含相位修正)
    /// </summary>
    public enum DuelOutcome
    {
        Win,    // 发起者胜
        Lose,   // 发起者负
        Draw    // 平局
    }

    /// <summary>
    /// 天命牌匹配状态
    /// </summary>
    public enum DestinyMatchType
    {
        None,               // 双方均未压中天命牌
        WinnerMatched,      // 赢家压中天命牌 -> 奖励加成
        DefenderMatched,    // 防守方压中天命牌 -> 惩罚加成
        LoserMatched        // 输家压中天命牌 -> 无效运气,无减免
    }

    /// <summary>
    /// 技能格状态
    /// </summary>
    public enum SkillSlotState
    {
        Empty,      // 空
        Charging,   // 充能中
        Full        // 已满,可释放
    }

    /// <summary>
    /// 玩家在对决中的角色
    /// </summary>
    public enum DuelRole
    {
        Initiator,  // 发起者 (主动进入子空间)
        Defender    // 防守方 (被拉入子空间)
    }
}
