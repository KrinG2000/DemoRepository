// ============================================================
// Card.cs — 卡牌数据模型
// 架构层级: Card (功能模块层)
// 依赖: Core/GameEnums
// 说明: 定义单张卡牌的数据结构。卡牌分为明牌和暗牌,
//       暗牌用于门票消耗,明牌用于对决出牌。
// ============================================================

using System;
using RacingCardGame.Core;

namespace RacingCardGame.Card
{
    public class Card
    {
        private static int _nextId = 0;

        /// <summary>
        /// 卡牌唯一ID (用于追踪和排序)
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 卡牌类型: 石头/剪刀/布
        /// </summary>
        public CardType Type { get; private set; }

        /// <summary>
        /// 是否为暗牌 (暗牌可作为门票消耗)
        /// </summary>
        public bool IsDarkCard { get; private set; }

        /// <summary>
        /// 卡牌创建时间戳 (用于确定"最旧"的暗牌)
        /// </summary>
        public long CreatedTimestamp { get; private set; }

        public Card(CardType type, bool isDarkCard)
        {
            Id = _nextId++;
            Type = type;
            IsDarkCard = isDarkCard;
            CreatedTimestamp = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// 用于测试: 支持指定时间戳的构造函数
        /// </summary>
        public Card(CardType type, bool isDarkCard, long timestamp)
        {
            Id = _nextId++;
            Type = type;
            IsDarkCard = isDarkCard;
            CreatedTimestamp = timestamp;
        }

        /// <summary>
        /// 重置ID计数器 (测试用)
        /// </summary>
        public static void ResetIdCounter()
        {
            _nextId = 0;
        }

        public override string ToString()
        {
            string darkLabel = IsDarkCard ? "[暗]" : "[明]";
            return $"{darkLabel} {Type} (ID:{Id})";
        }
    }
}
