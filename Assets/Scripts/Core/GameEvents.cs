// ============================================================
// GameEvents.cs — 事件系统
// 架构层级: Core (基础层)
// 说明: 使用C#事件实现模块间解耦通信,各Manager通过
//       订阅/发布事件进行交互,避免直接引用
// ============================================================

using System;

namespace RacingCardGame.Core
{
    /// <summary>
    /// 全局事件总线 — 各系统通过此类进行解耦通信
    /// </summary>
    public static class GameEvents
    {
        // ---- 技能格事件 ----

        /// <summary>
        /// 漂移充能事件: float = 充能量
        /// </summary>
        public static event Action<float> OnDriftCharge;

        /// <summary>
        /// 技能格变化事件: int = 当前已满技能格数
        /// </summary>
        public static event Action<int> OnSkillSlotChanged;

        /// <summary>
        /// 技能释放事件 (技能格满时触发)
        /// </summary>
        public static event Action OnSkillActivated;

        // ---- 子空间对决事件 ----

        /// <summary>
        /// 子空间对决发起事件: (发起者ID, 防守方ID)
        /// </summary>
        public static event Action<int, int> OnDuelInitiated;

        /// <summary>
        /// 子空间对决结束事件: DuelResultData = 对决结果数据
        /// </summary>
        public static event Action<DuelResultData> OnDuelResolved;

        // ---- 门票事件 ----

        /// <summary>
        /// 门票消耗事件: (玩家ID, 被消耗的卡牌)
        /// </summary>
        public static event Action<int, Card.Card> OnTicketConsumed;

        // ---- 相位事件 ----

        /// <summary>
        /// 相位锁定事件: PhaseType = 本局锁定的相位
        /// </summary>
        public static event Action<PhaseType> OnPhaseLocked;

        /// <summary>
        /// 相位规则触发事件: string = 规则描述 (用于UI展示)
        /// </summary>
        public static event Action<string> OnPhaseRuleTriggered;

        // ---- 无限火力相位事件 ----

        /// <summary>
        /// 过热开始事件: (玩家ID, 过热持续秒数)
        /// </summary>
        public static event Action<int, float> OnOverheatStarted;

        /// <summary>
        /// 过热结束事件: 玩家ID
        /// </summary>
        public static event Action<int> OnOverheatEnded;

        /// <summary>
        /// 幽灵保护激活事件: (玩家ID, 保护持续秒数)
        /// </summary>
        public static event Action<int, float> OnGhostProtectionActivated;

        /// <summary>
        /// 幽灵保护结束事件: 玩家ID
        /// </summary>
        public static event Action<int> OnGhostProtectionExpired;

        // ---- 赛车/状态效果事件 (Task 5) ----

        /// <summary>
        /// 状态效果施加事件: (playerId, StatusEffect)
        /// </summary>
        public static event Action<int, StatusEffect> OnStatusEffectApplied;

        /// <summary>
        /// 状态效果过期事件: (playerId, StatusEffectType)
        /// </summary>
        public static event Action<int, StatusEffectType> OnStatusEffectExpired;

        /// <summary>
        /// 卡牌拾取事件: (playerId, CardType)
        /// </summary>
        public static event Action<int, CardType> OnCardPickedUp;

        /// <summary>
        /// 对决流程进入子空间: (attackerId, defenderId)
        /// </summary>
        public static event Action<int, int> OnSubspaceEntered;

        /// <summary>
        /// 对决流程退出子空间: (attackerId, defenderId)
        /// </summary>
        public static event Action<int, int> OnSubspaceExited;

        /// <summary>
        /// 对决验证失败事件: (attackerId, reason)
        /// </summary>
        public static event Action<int, string> OnDuelValidationFailed;

        // ---- 游戏生命周期事件 ----

        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public static event Action OnGameStart;

        /// <summary>
        /// 游戏结束事件
        /// </summary>
        public static event Action OnGameEnd;

        // ==== 事件触发方法 ====

        public static void RaiseDriftCharge(float amount) => OnDriftCharge?.Invoke(amount);
        public static void RaiseSkillSlotChanged(int filledSlots) => OnSkillSlotChanged?.Invoke(filledSlots);
        public static void RaiseSkillActivated() => OnSkillActivated?.Invoke();
        public static void RaiseDuelInitiated(int initiatorId, int defenderId) => OnDuelInitiated?.Invoke(initiatorId, defenderId);
        public static void RaiseDuelResolved(DuelResultData result) => OnDuelResolved?.Invoke(result);
        public static void RaiseTicketConsumed(int playerId, Card.Card card) => OnTicketConsumed?.Invoke(playerId, card);
        public static void RaisePhaseLocked(PhaseType phase) => OnPhaseLocked?.Invoke(phase);
        public static void RaisePhaseRuleTriggered(string description) => OnPhaseRuleTriggered?.Invoke(description);
        public static void RaiseOverheatStarted(int playerId, float duration) => OnOverheatStarted?.Invoke(playerId, duration);
        public static void RaiseOverheatEnded(int playerId) => OnOverheatEnded?.Invoke(playerId);
        public static void RaiseGhostProtectionActivated(int playerId, float duration) => OnGhostProtectionActivated?.Invoke(playerId, duration);
        public static void RaiseGhostProtectionExpired(int playerId) => OnGhostProtectionExpired?.Invoke(playerId);
        public static void RaiseStatusEffectApplied(int playerId, StatusEffect effect) => OnStatusEffectApplied?.Invoke(playerId, effect);
        public static void RaiseStatusEffectExpired(int playerId, StatusEffectType type) => OnStatusEffectExpired?.Invoke(playerId, type);
        public static void RaiseCardPickedUp(int playerId, CardType cardType) => OnCardPickedUp?.Invoke(playerId, cardType);
        public static void RaiseSubspaceEntered(int attackerId, int defenderId) => OnSubspaceEntered?.Invoke(attackerId, defenderId);
        public static void RaiseSubspaceExited(int attackerId, int defenderId) => OnSubspaceExited?.Invoke(attackerId, defenderId);
        public static void RaiseDuelValidationFailed(int attackerId, string reason) => OnDuelValidationFailed?.Invoke(attackerId, reason);
        public static void RaiseGameStart() => OnGameStart?.Invoke();
        public static void RaiseGameEnd() => OnGameEnd?.Invoke();

        /// <summary>
        /// 清除所有事件订阅 (场景切换/游戏重置时调用)
        /// </summary>
        public static void ClearAll()
        {
            OnDriftCharge = null;
            OnSkillSlotChanged = null;
            OnSkillActivated = null;
            OnDuelInitiated = null;
            OnDuelResolved = null;
            OnTicketConsumed = null;
            OnPhaseLocked = null;
            OnPhaseRuleTriggered = null;
            OnOverheatStarted = null;
            OnOverheatEnded = null;
            OnGhostProtectionActivated = null;
            OnGhostProtectionExpired = null;
            OnStatusEffectApplied = null;
            OnStatusEffectExpired = null;
            OnCardPickedUp = null;
            OnSubspaceEntered = null;
            OnSubspaceExited = null;
            OnDuelValidationFailed = null;
            OnGameStart = null;
            OnGameEnd = null;
        }
    }

    /// <summary>
    /// 对决结果数据 — 封装一次子空间对决的完整结果
    /// </summary>
    public class DuelResultData
    {
        public int InitiatorId;
        public int DefenderId;
        public CardType InitiatorCard;
        public CardType DefenderCard;
        public CardType DestinyCard;
        public DuelOutcome Outcome;
        public DestinyMatchType DestinyMatch;
        public PhaseType ActivePhase;

        // 相位修正后的最终奖惩倍率
        public float RewardMultiplier;
        public float PenaltyMultiplier;

        // 天命效果类型 (用于UI展示和效果分发)
        public DestinyEffectType DestinyEffect;

        // 相位特殊效果描述
        public string PhaseEffectDescription;

        // 是否发生了小丑相位的牌面互换
        public bool CardsSwapped;

        // 是否触发了胜天半子 (仅天命赌场相位)
        public bool IsShengTianBanZi;

        // 无限火力: 过热持续时间 (秒)
        public float OverheatDuration;

        // 无限火力: 是否触发了幽灵保护
        public bool GhostProtectionGranted;

        // 止戈相位: 是否发生了剪刀→石头的强制转换
        public bool ScissorsConverted;

        // 原始出牌 (相位修改前)
        public CardType OriginalInitiatorCard;
        public CardType OriginalDefenderCard;
    }
}
