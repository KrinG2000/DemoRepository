// ============================================================
// CardInventorySystem.cs — 卡牌库存系统实现
// 架构层级: Inventory
// 说明: 包装 CardManager, 提供面向赛车主循环的接口。
//       门票消耗 = dequeue最旧暗牌 (FIFO), 不参与结算。
//       对决出牌 = 玩家从 slot1/slot2 选择。
//       进入子空间时 overflow 强制清空。
// ============================================================

using System;
using RacingCardGame.Core;
using RacingCardGame.Card;
using RacingCardGame.Skill;

namespace RacingCardGame.Inventory
{
    public class CardInventorySystem : ICardInventorySystem
    {
        public int PlayerId => _cards.PlayerId;

        public CardType? Slot1Card
        {
            get { return _cards.Hand.Count > 0 ? (CardType?)_cards.Hand[0].Type : null; }
        }

        public CardType? Slot2Card
        {
            get { return _cards.Hand.Count > 1 ? (CardType?)_cards.Hand[1].Type : null; }
        }

        public CardType? OverflowCard
        {
            get
            {
                var oc = _cards.OverflowCard;
                return oc != null ? (CardType?)oc.Type : null;
            }
        }

        public float OverflowRemainingTime => _cards.OverflowRemainingTime;
        public int TicketCount => _cards.DarkCardCount;
        public bool HasOverflow => _cards.HasOverflow;

        private readonly CardManager _cards;
        private readonly Random _random;

        /// <summary>
        /// 直接访问底层 CardManager (供 DuelSystem 使用)
        /// </summary>
        public CardManager InternalCardManager => _cards;

        public CardInventorySystem(CardManager cards, Random random = null)
        {
            _cards = cards;
            _random = random ?? new Random();
        }

        public bool TryAddCard(CardType newCard)
        {
            // 创建暗牌 (同时作为门票来源) 和明牌 (手牌)
            // 规则: 每次拾取产生1张明牌(进手牌/overflow) + 1张暗牌(进门票队列)
            var handCard = new Card.Card(newCard, false);
            var darkCard = new Card.Card(newCard, true);

            // 暗牌始终进入门票队列
            _cards.AddDarkCard(darkCard);

            // 明牌进手牌或overflow
            _cards.AddToHand(handCard);

            GameEvents.RaiseCardPickedUp(PlayerId, newCard);
            return _cards.HasOverflow;
        }

        public void ReplaceSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _cards.Hand.Count)
                _cards.ReplaceSlotWithOverflow(slotIndex);
        }

        public bool TryConsumeTicket(out CardType consumedTicketCard)
        {
            consumedTicketCard = CardType.Rock;
            if (_cards.DarkCardCount == 0)
                return false;

            var ticket = _cards.PeekOldestDarkCard();
            consumedTicketCard = ticket.Type;
            return true;
        }

        public void ClearOverflowOnEnterDuel()
        {
            _cards.ClearOverflow();
        }

        public CardType[] GetAvailableSlotCards()
        {
            var hand = _cards.Hand;
            var result = new CardType[hand.Count];
            for (int i = 0; i < hand.Count; i++)
                result[i] = hand[i].Type;
            return result;
        }

        public void Tick(float deltaTime)
        {
            // Overflow auto-expires via CardManager's time-based check
            // (triggered by property access — no explicit tick needed)
        }
    }
}
