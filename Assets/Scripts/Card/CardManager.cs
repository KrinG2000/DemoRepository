// ============================================================
// CardManager.cs — 手牌管理器
// 架构层级: Card (功能模块层)
// 依赖: Core/GameEnums, Core/GameEvents, Card/Card
// 说明: 管理玩家的手牌、暗牌队列、门票消耗逻辑。
//       暗牌使用Queue(FIFO)确保消耗最旧的暗牌。
//       每个玩家实例拥有独立的CardManager。
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RacingCardGame.Core;

namespace RacingCardGame.Card
{
    public class CardManager
    {
        /// <summary>
        /// 所属玩家ID
        /// </summary>
        public int PlayerId { get; private set; }

        /// <summary>
        /// 玩家手牌列表 (明牌, 可用于出牌对决)
        /// </summary>
        private readonly List<Card> _hand = new List<Card>();

        /// <summary>
        /// 暗牌队列 (FIFO, 最旧的在队首, 用于门票消耗)
        /// </summary>
        private readonly Queue<Card> _darkCards = new Queue<Card>();

        /// <summary>
        /// 手牌只读访问
        /// </summary>
        public IReadOnlyList<Card> Hand => _hand.AsReadOnly();

        /// <summary>
        /// 暗牌数量
        /// </summary>
        public int DarkCardCount => _darkCards.Count;

        /// <summary>
        /// 是否有暗牌可消耗为门票
        /// </summary>
        public bool HasTicket => _darkCards.Count > 0;

        public CardManager(int playerId)
        {
            PlayerId = playerId;
        }

        /// <summary>
        /// 添加一张明牌到手牌
        /// </summary>
        public void AddToHand(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            _hand.Add(card);
        }

        /// <summary>
        /// 添加一张暗牌到暗牌队列 (队尾)
        /// </summary>
        public void AddDarkCard(Card card)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (!card.IsDarkCard)
                throw new ArgumentException("Only dark cards can be added to the dark card queue.");
            _darkCards.Enqueue(card);
        }

        /// <summary>
        /// 消耗最旧的暗牌作为门票 (FIFO)
        /// 门票消耗后不参与对决判定,只作为进入子空间的条件
        /// </summary>
        /// <returns>被消耗的暗牌; 如果没有暗牌则返回null</returns>
        public Card ConsumeTicket()
        {
            if (_darkCards.Count == 0)
                return null;

            Card consumed = _darkCards.Dequeue();
            GameEvents.RaiseTicketConsumed(PlayerId, consumed);
            return consumed;
        }

        /// <summary>
        /// 从手牌中选择一张牌出牌 (用于子空间对决)
        /// </summary>
        /// <param name="cardType">要出的牌类型</param>
        /// <returns>选出的牌; 如果手牌中没有该类型则返回null</returns>
        public Card PlayCard(CardType cardType)
        {
            Card card = _hand.FirstOrDefault(c => c.Type == cardType);
            if (card != null)
            {
                _hand.Remove(card);
            }
            return card;
        }

        /// <summary>
        /// 从手牌中通过ID选择一张牌出牌
        /// </summary>
        public Card PlayCardById(int cardId)
        {
            Card card = _hand.FirstOrDefault(c => c.Id == cardId);
            if (card != null)
            {
                _hand.Remove(card);
            }
            return card;
        }

        /// <summary>
        /// 检查手牌中是否有指定类型的牌
        /// </summary>
        public bool HasCardType(CardType type)
        {
            return _hand.Any(c => c.Type == type);
        }

        /// <summary>
        /// 获取手牌中各类型牌的数量
        /// </summary>
        public Dictionary<CardType, int> GetHandSummary()
        {
            var summary = new Dictionary<CardType, int>();
            foreach (CardType type in Enum.GetValues(typeof(CardType)))
            {
                summary[type] = _hand.Count(c => c.Type == type);
            }
            return summary;
        }

        /// <summary>
        /// 查看暗牌队列中最旧的暗牌 (不消耗)
        /// </summary>
        public Card PeekOldestDarkCard()
        {
            return _darkCards.Count > 0 ? _darkCards.Peek() : null;
        }

        /// <summary>
        /// 清空所有卡牌 (游戏重置时调用)
        /// </summary>
        public void Reset()
        {
            _hand.Clear();
            _darkCards.Clear();
        }

        /// <summary>
        /// 发一组初始手牌 (游戏开始时调用)
        /// </summary>
        /// <param name="cards">初始卡牌列表</param>
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
