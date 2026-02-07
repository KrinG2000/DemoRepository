// ============================================================
// DuelBannerPresenter.cs — Banner展示器
// 架构层级: UI
// 说明: 负责将Banner显示到屏幕。支持Show/Skip/Hide。
//       当前为纯C#占位实现,后续可替换为Unity UI实现。
// ============================================================

using System;
using System.Collections.Generic;

namespace RacingCardGame.UI
{
    /// <summary>
    /// Banner展示器接口
    /// </summary>
    public interface IDuelBannerPresenter
    {
        void Show(UIBannerType type, float durationSeconds, bool skippable);
        void Skip();
        void Hide();
        bool IsShowing { get; }
        UIBannerType? CurrentBanner { get; }
    }

    /// <summary>
    /// 简单Banner展示器实现 — 纯C#占位,记录状态和事件回调
    /// </summary>
    public class SimpleDuelBannerPresenter : IDuelBannerPresenter
    {
        private UIBannerType? _currentBanner;
        private bool _skippable;
        private bool _isShowing;

        public event Action<UIBannerType> OnBannerDisplayed;
        public event Action OnBannerHidden;

        private static readonly Dictionary<UIBannerType, string> BannerTexts =
            new Dictionary<UIBannerType, string>
        {
            { UIBannerType.ConquerHeaven, "胜天半子！" },
            { UIBannerType.JesterSwap, "小丑惊魂！" },
            { UIBannerType.DestinyCrit, "天命暴击！" },
            { UIBannerType.DestinyCounter, "天命反杀！" },
            { UIBannerType.BadLuck, "无效运气！" },
            { UIBannerType.NormalWin, "胜利！" },
            { UIBannerType.Draw, "平局！" },
        };

        public void Show(UIBannerType type, float durationSeconds, bool skippable)
        {
            _currentBanner = type;
            _skippable = skippable;
            _isShowing = true;
            OnBannerDisplayed?.Invoke(type);
        }

        public void Skip()
        {
            if (_isShowing && _skippable)
            {
                Hide();
            }
        }

        public void Hide()
        {
            var wasBanner = _currentBanner;
            _currentBanner = null;
            _isShowing = false;
            if (wasBanner.HasValue)
                OnBannerHidden?.Invoke();
        }

        public bool IsShowing => _isShowing;
        public UIBannerType? CurrentBanner => _currentBanner;

        /// <summary>
        /// 获取Banner占位文本
        /// </summary>
        public static string GetBannerText(UIBannerType type)
        {
            return BannerTexts.TryGetValue(type, out var text) ? text : type.ToString();
        }
    }
}
