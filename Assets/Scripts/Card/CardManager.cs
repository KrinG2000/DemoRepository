// ============================================================
// CardManager.cs — 手牌管理器
// 架构层级: Card (功能模块层)
// 依赖: Core/GameEnums, Core/GameEvents, Config/BalanceConfig, Skill/ITimeProvider
// 说明: 管理玩家的手牌(Slot1/Slot2)、暗牌队列(门票)、Overflow。
//       暗牌使用Queue(FIFO)确保消耗最旧的暗牌。
//       手牌限制MaxHandSlots(默认2),超出进入Overflow(4s超时)。
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RacingCardGame.Core;
using RacingCardGame.Config;
using RacingCardGame.Skill;

namespace RacingCardGame.Card
{
    public class CardManager
    {
        public int PlayerId { get; private set; }

        private readonly List<Card> _hand = new List<Card>();
        private readonly Queue<Card> _darkCards = new Queue<Card>();

        private Card _overflowCard;
        private float _overflowStartTime = -1f;
        private ITimeProvider _timeProvider;

        public IReadOnlyList<Card> Hand => _hand.AsReadOnly();
        public int DarkCardCount => _darkCards.Count;
        public bool HasTicket => _darkCards.Count > 0;

        public Card OverflowCard
        {
            get { TickOverflow(); return _overflowCard; }
        }

        public bool HasOverflow
        {
            get { TickOverflow(); return _overflowCard != null; }
        }

        public float OverflowRemainingTime
        {
            get
            {
                if (_overflowCard == null || _timeProvider == null || _overflowStartTime < 0f)
                    return 0f;
                float elapsed = _timeProvider.CurrentTime - _overflowStartTime;
                float remaining = BalanceConfig.Current.OverflowLifetime - elapsed;
                return remaining > 0f ? remaining : 0f;
            }
        }

        public CardManager(int playerId) : this(playerId, null) { }

        public CardManager(int playerId, ITimeProvider timeProvider)
        {
            PlayerId = playerId;
            _timeProvider = timeProvider;
        }

        public void SetTimeProvider(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public void AddToHand(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            if (_hand.Count < BalanceConfig.Current.MaxHandSlots)
            {
                _hand.Add(card);
            }
            else
            {
                _overflowCard = card;
                _overflowStartTime = _timeProvider != null ? _timeProvider.CurrentTime : -1f;
            }
        }

        public void AddDarkCard(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (!card.IsDarkCard)
                throw new ArgumentException("Only dark cards can be added to the dark card queue.");
            _darkCards.Enqueue(card);
        }

        public Card ConsumeTicket()
        {
            if (_darkCards.Count == 0)
                return null;
            Card consumed = _darkCards.Dequeue();
            GameEvents.RaiseTicketConsumed(PlayerId, consumed);
            return consumed;
        }

        public Card PlayCard(CardType cardType)
        {
            Card card = _hand.FirstOrDefault(c => c.Type == cardType);
            if (card != null)
                _hand.Remove(card);
            return card;
        }

        public Card PlayCardById(int cardId)
        {
            Card card = _hand.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
                _hand.Remove(card);
            return card;
        }

        /// <summary>
        /// 用Overflow卡替换指定Slot的手牌
        /// </summary>
        public Card ReplaceSlotWithOverflow(int slotIndex)
        {
            TickOverflow();
            if (_overflowCard == null || slotIndex < 0 || slotIndex >= _hand.Count)
                return null;
            Card replaced = _hand[slotIndex];
            _hand[slotIndex] = _overflowCard;
            _overflowCard = null;
            _overflowStartTime = -1f;
            return replaced;
        }

        /// <summary>
        /// 清空Overflow (进入子空间时调用)
        /// </summary>
        public void ClearOverflow()
        {
            _overflowCard = null;
            _overflowStartTime = -1f;
        }

        private void TickOverflow()
        {
            if (_overflowCard == null || _timeProvider == null || _overflowStartTime < 0f)
                return;
            float elapsed = _timeProvider.CurrentTime - _overflowStartTime;
            if (elapsed >= BalanceConfig.Current.OverflowLifetime)
            {
                _overflowCard = null;
                _overflowStartTime = -1f;
            }
        }

        public bool HasCardType(CardType type)
        {
            return _hand.Any(c => c.Type == type);
        }

        public Dictionary<CardType, int> GetHandSummary()
        {
            var summary = new Dictionary<CardType, int>();
            foreach (CardType type in Enum.GetValues(typeof(CardType)))
                summary[type] = _hand.Count(c => c.Type == type);
            return summary;
        }

        public Card PeekOldestDarkCard()
        {
            return _darkCards.Count > 0 ? _darkCards.Peek() : null;
        }

        public void Reset()
        {
            _hand.Clear();
            _darkCards.Clear();
            ClearOverflow();
        }

        public void DealInitialHand(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                if (card.IsDarkCard)
                    AddDarkCard(card);
                else
                    AddToHand(card);
            }
        }
    }
}
