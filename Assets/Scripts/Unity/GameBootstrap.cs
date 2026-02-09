// ============================================================
// GameBootstrap.cs — 场景唯一逻辑入口 (MonoBehaviour)
// 架构层级: Unity Bridge
// 说明: 将纯C# RaceSession 桥接到 Unity 场景。
//       一个 GameObject 挂这个脚本就够了, 不需要其他 MB。
//       GameBootstrap 负责:
//         1. 应用 BalanceConfigSO
//         2. 创建 RaceSession + UnityTimeProvider
//         3. 实例化 Car/Pickup Prefab
//         4. Update 中驱动主循环 + 同步 Transform
//         5. 处理键盘输入 (Shift漂移, E对决, F1 HUD)
//         6. OnGUI 显示 DebugHUD
//
// Inspector 设置:
//   - Balance Config: 拖入 BalanceConfigSO.asset
//   - Player Car Prefab: 拖入玩家车 Prefab (Capsule 即可)
//   - Ai Car Prefab: 拖入 AI 车 Prefab (Capsule 即可)
//   - Card Pickup Prefab: 拖入道具箱 Prefab (Cube 即可)
//   - Ai Count: AI 数量 (默认 2, 最多 5)
// ============================================================

#if UNITY_5_3_OR_NEWER
using UnityEngine;
using System.Collections.Generic;
using RacingCardGame.Runtime;
using RacingCardGame.DuelFlow;

namespace RacingCardGame.Unity
{
    public class GameBootstrap : MonoBehaviour
    {
        // ---- Inspector 字段 (必须拖入) ----

        [Header("Config (必须)")]
        [Tooltip("右键 Create → RacingCardGame → Balance Config 创建")]
        [SerializeField] private BalanceConfigSO balanceConfig;

        [Header("Prefabs (必须)")]
        [Tooltip("玩家车 Prefab — Capsule + 材质即可")]
        [SerializeField] private GameObject playerCarPrefab;

        [Tooltip("AI车 Prefab — Capsule + 不同材质即可")]
        [SerializeField] private GameObject aiCarPrefab;

        [Tooltip("道具箱 Prefab — Cube + 材质即可")]
        [SerializeField] private GameObject cardPickupPrefab;

        [Header("Settings")]
        [Range(1, 5)]
        [SerializeField] private int aiCount = 2;

        [SerializeField] private int humanPlayerId = 0;

        [Header("Controls")]
        [SerializeField] private KeyCode driftKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode duelKey = KeyCode.E;
        [SerializeField] private KeyCode debugHudKey = KeyCode.F1;

        [Header("Track Visual")]
        [Tooltip("赛道方向 — 车辆沿此方向前进")]
        [SerializeField] private Vector3 trackDirection = Vector3.forward;

        [Tooltip("逻辑位置(米) → 世界坐标的缩放")]
        [SerializeField] private float positionScale = 1f;

        // ---- Runtime ----
        private RaceSession _session;
        private Dictionary<int, Transform> _carTransforms = new Dictionary<int, Transform>();
        private List<Transform> _pickupTransforms = new List<Transform>();
        private bool _showDebugHud = true;
        private string _debugText = "";
        private GUIStyle _debugStyle;

        // ---- Lifecycle ----

        private void Start()
        {
            // Validate
            if (balanceConfig == null)
            {
                Debug.LogError("[GameBootstrap] BalanceConfigSO 未拖入! " +
                    "请在 Project 面板右键 Create → RacingCardGame → Balance Config 创建, " +
                    "然后拖入 GameBootstrap Inspector 的 Balance Config 字段.");
                enabled = false;
                return;
            }

            // 1. Apply config → BalanceConfig.Current
            balanceConfig.Apply();

            // 2. Create session with Unity time
            var timeProvider = new UnityTimeProvider();
            _session = new RaceSession(timeProvider: timeProvider);
            _session.Initialize(humanPlayerId, aiCount);

            // 3. Instantiate car prefabs
            Vector3 dir = trackDirection.normalized;
            foreach (var kvp in _session.Racers)
            {
                var racer = kvp.Value;
                GameObject prefab = racer.IsHuman ? playerCarPrefab : aiCarPrefab;

                if (prefab == null)
                {
                    Debug.LogWarning($"[GameBootstrap] " +
                        $"{(racer.IsHuman ? "PlayerCar" : "AICar")} Prefab 未设置, " +
                        $"跳过 P{racer.PlayerId} 的视觉实例化");
                    continue;
                }

                float logicalPos = racer.Car.GetPosition();
                Vector3 worldPos = dir * logicalPos * positionScale;
                GameObject go = Instantiate(prefab, worldPos, Quaternion.LookRotation(dir));
                go.name = racer.IsHuman ? "PlayerCar" : $"AICar_{racer.PlayerId}";
                _carTransforms[racer.PlayerId] = go.transform;
            }

            // 4. Instantiate pickup prefabs
            if (cardPickupPrefab != null && _session.PickupManager != null)
            {
                foreach (var zone in _session.PickupManager.Zones)
                {
                    Vector3 worldPos = dir * zone.Position * positionScale;
                    GameObject go = Instantiate(cardPickupPrefab, worldPos, Quaternion.identity);
                    go.name = $"CardPickup_{zone.Position:F0}m";
                    _pickupTransforms.Add(go.transform);
                }
            }

            Debug.Log($"[GameBootstrap] Race started: 1 human (P{humanPlayerId}) + {aiCount} AI, " +
                $"{_session.PickupManager?.Zones.Count ?? 0} pickups on track");
        }

        private void Update()
        {
            if (_session == null || !_session.IsRaceActive) return;

            // Input
            HandleInput();

            // Main loop tick
            _session.Tick(Time.deltaTime);

            // Sync visual positions
            SyncCarPositions();

            // Race finish check
            int? winner = _session.CheckFinish();
            if (winner.HasValue)
            {
                var rankings = _session.GetRankings();
                string rankStr = string.Join(" > ", rankings.ConvertAll(id => $"P{id}"));
                Debug.Log($"[GameBootstrap] Race finished! Winner: P{winner.Value}  Rankings: {rankStr}");
                _session.EndRace();
            }

            // Refresh debug text
            if (_showDebugHud)
                _debugText = _session.RenderDebugState();
        }

        // ---- Input ----

        private void HandleInput()
        {
            // Drift: hold Shift
            _session.PlayerSetDrift(humanPlayerId, Input.GetKey(driftKey));

            // Duel: press E
            if (Input.GetKeyDown(duelKey))
            {
                DuelFailReason reason = _session.PlayerTryDuel(humanPlayerId);
                if (reason == DuelFailReason.None)
                    Debug.Log("[Duel] Subspace entered — duel started!");
                else
                    Debug.LogWarning($"[Duel] Cannot start duel: {reason}");
            }

            // DebugHUD: toggle F1
            if (Input.GetKeyDown(debugHudKey))
                _showDebugHud = !_showDebugHud;
        }

        // ---- Visual Sync ----

        private void SyncCarPositions()
        {
            Vector3 dir = trackDirection.normalized;
            foreach (var kvp in _session.Racers)
            {
                int pid = kvp.Key;
                float logicalPos = kvp.Value.Car.GetPosition();
                if (_carTransforms.ContainsKey(pid))
                    _carTransforms[pid].position = dir * logicalPos * positionScale;
            }
        }

        // ---- Debug HUD (OnGUI) ----

        private void OnGUI()
        {
            if (!_showDebugHud || string.IsNullOrEmpty(_debugText)) return;

            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    richText = false,
                    wordWrap = false
                };
                _debugStyle.normal.textColor = Color.white;

                // Drop shadow for readability
                var bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
                bgTex.Apply();
                _debugStyle.normal.background = bgTex;
                _debugStyle.padding = new RectOffset(8, 8, 4, 4);
            }

            GUI.Label(new Rect(10, 10, 700, 500), _debugText, _debugStyle);
        }

        // ---- Public API (for future UI buttons) ----

        /// <summary>
        /// 替换 overflow 卡到手牌槽 (给 UI 按钮绑定)
        /// </summary>
        public void ReplaceSlot(int slotIndex)
        {
            if (_session != null)
                _session.PlayerReplaceSlot(humanPlayerId, slotIndex);
        }
    }
}
#endif
