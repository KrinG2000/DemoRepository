// ============================================================
// ICardInventorySystem.cs — 卡牌库存系统接口
// 架构层级: Inventory (积木式接口)
// 说明: 管理 slot1/slot2/overflow/门票(暗牌M1)。
//       门票消耗最旧暗牌 (FIFO), 不参与结算。
//       对决出牌从 slot1/slot2 选择。
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Inventory
{
    public interface ICardInventorySystem
    {
        int PlayerId { get; }
        CardType? Slot1Card { get; }
        CardType? Slot2Card { get; }
        CardType? OverflowCard { get; }
        float OverflowRemainingTime { get; }
        int TicketCount { get; }
        bool HasOverflow { get; }

        /// <summary>
        /// 拾取卡牌: slot未满填slot, 满则进overflow(4s倒计时)
        /// </summary>
        bool TryAddCard(CardType newCard);

        /// <summary>
        /// 替换slot (overflow存在时): 0=slot1, 1=slot2
        /// </summary>
        void ReplaceSlot(int slotIndex);

        /// <summary>
        /// 消耗最旧暗牌作为门票 (M1规则)
        /// </summary>
        bool TryConsumeTicket(out CardType consumedTicketCard);

        /// <summary>
        /// 进入子空间时清空overflow
        /// </summary>
        void ClearOverflowOnEnterDuel();

        /// <summary>
        /// 获取slot卡牌用于对决选择
        /// </summary>
        CardType[] GetAvailableSlotCards();

        /// <summary>
        /// 每帧更新 (overflow倒计时)
        /// </summary>
        void Tick(float deltaTime);
    }
}
