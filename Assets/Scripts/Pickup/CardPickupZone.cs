// ============================================================
// CardPickupZone.cs — 卡牌拾取区域
// 架构层级: Pickup
// 说明: 赛道道具箱。车辆经过时触发 TryAddCard(RandomCardType)。
//       在Unity中替换为Collider + OnTriggerEnter。
//       纯C#中使用位置检测。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Core;
using RacingCardGame.Inventory;

namespace RacingCardGame.Pickup
{
    public class CardPickupZone
    {
        public float Position { get; private set; }
        public float TriggerRadius { get; private set; }

        private readonly Random _random;
        // 冷却: 同一玩家短时间内不重复触发
        private readonly HashSet<int> _recentPickups = new HashSet<int>();

        public CardPickupZone(float position, float triggerRadius = 5f, Random random = null)
        {
            Position = position;
            TriggerRadius = triggerRadius;
            _random = random ?? new Random();
        }

        /// <summary>
        /// 检查车辆是否在触发区内, 若是则触发拾取
        /// </summary>
        public bool TryPickup(int playerId, float carPosition, ICardInventorySystem inventory)
        {
            if (Math.Abs(carPosition - Position) > TriggerRadius)
            {
                // 离开区域后清除冷却
                _recentPickups.Remove(playerId);
                return false;
            }

            if (_recentPickups.Contains(playerId))
                return false; // 已经拾取过, 等离开后再允许

            // 触发拾取
            CardType randomCard = (CardType)_random.Next(3);
            inventory.TryAddCard(randomCard);
            _recentPickups.Add(playerId);
            return true;
        }

        /// <summary>
        /// 重置拾取冷却
        /// </summary>
        public void ResetCooldowns()
        {
            _recentPickups.Clear();
        }
    }

    /// <summary>
    /// 道具箱管理器 — 管理赛道上所有拾取区域
    /// </summary>
    public class CardPickupManager
    {
        private readonly List<CardPickupZone> _zones = new List<CardPickupZone>();

        public IReadOnlyList<CardPickupZone> Zones => _zones.AsReadOnly();

        /// <summary>
        /// 沿赛道均匀分布道具箱
        /// </summary>
        public void GenerateZones(float trackLength, int count, float triggerRadius = 5f, Random random = null)
        {
            _zones.Clear();
            if (count <= 0) return;

            float spacing = trackLength / (count + 1);
            for (int i = 1; i <= count; i++)
            {
                _zones.Add(new CardPickupZone(spacing * i, triggerRadius, random));
            }
        }

        /// <summary>
        /// 对单个玩家检测所有道具箱
        /// </summary>
        public int CheckPickups(int playerId, float carPosition, ICardInventorySystem inventory)
        {
            int pickupCount = 0;
            foreach (var zone in _zones)
            {
                if (zone.TryPickup(playerId, carPosition, inventory))
                    pickupCount++;
            }
            return pickupCount;
        }
    }
}
