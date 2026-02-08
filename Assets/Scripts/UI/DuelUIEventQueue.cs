// ============================================================
// DuelUIEventQueue.cs — 演出事件队列/调度器
// 架构层级: UI
// 说明: 管理对决结算后的UI Banner展示队列。
//       强约束: 同一次ctx只展示1个主Banner (互斥)。
//       时序: Enqueue在[Apply]后[Exit]前; 展示在[Exit]后延迟0.15s。
//       每帧调用Tick()驱动状态机。
// ============================================================

using System;
using System.Collections.Generic;
using RacingCardGame.Skill;

namespace RacingCardGame.UI
{
    public class DuelUIEventQueue
    {
        private readonly Queue<DuelResultContext> _queue;
        private readonly IDuelBannerPresenter _presenter;
        private readonly ITimeProvider _timeProvider;

        private readonly float _delayBeforeShow;
        private readonly float _bannerDuration;

        private enum QueueState { Idle, WaitingDelay, ShowingBanner }
        private QueueState _state = QueueState.Idle;
        private float _stateEnteredTime;
        private UIBannerType? _currentBannerType;

        /// <summary>
        /// VFX/SFX Hook: Banner展示时回调
        /// </summary>
        public event Action<UIBannerType> OnBannerShown;

        /// <summary>
        /// Banner隐藏时回调
        /// </summary>
        public event Action OnBannerDismissed;

        public DuelUIEventQueue(
            IDuelBannerPresenter presenter,
            ITimeProvider timeProvider,
            float delayBeforeShow = 0.15f,
            float bannerDuration = 1.5f)
        {
            _queue = new Queue<DuelResultContext>();
            _presenter = presenter;
            _timeProvider = timeProvider;
            _delayBeforeShow = delayBeforeShow;
            _bannerDuration = bannerDuration;
        }

        /// <summary>
        /// 入队一个对决结算上下文 (在[Apply]之后、[Exit]之前调用)
        /// </summary>
        public void Enqueue(DuelResultContext ctx)
        {
            _queue.Enqueue(ctx);
        }

        /// <summary>
        /// 每帧调用,驱动状态机
        /// </summary>
        public void Tick()
        {
            float now = _timeProvider.CurrentTime;

            switch (_state)
            {
                case QueueState.Idle:
                    if (_queue.Count > 0)
                    {
                        _state = QueueState.WaitingDelay;
                        _stateEnteredTime = now;
                    }
                    break;

                case QueueState.WaitingDelay:
                    if (now - _stateEnteredTime >= _delayBeforeShow)
                    {
                        var ctx = _queue.Dequeue();
                        var bannerType = DuelBannerResolver.PickBanner(ctx);
                        _currentBannerType = bannerType;
                        _presenter.Show(bannerType, _bannerDuration, true);
                        OnBannerShown?.Invoke(bannerType);
                        _state = QueueState.ShowingBanner;
                        _stateEnteredTime = now;
                    }
                    break;

                case QueueState.ShowingBanner:
                    if (!_presenter.IsShowing)
                    {
                        // Banner已被跳过
                        _currentBannerType = null;
                        _state = QueueState.Idle;
                        OnBannerDismissed?.Invoke();
                    }
                    else if (now - _stateEnteredTime >= _bannerDuration)
                    {
                        // Banner持续时间到期
                        _presenter.Hide();
                        _currentBannerType = null;
                        _state = QueueState.Idle;
                        OnBannerDismissed?.Invoke();
                    }
                    break;
            }
        }

        /// <summary>
        /// 用户点击跳过当前Banner
        /// </summary>
        public void Skip()
        {
            if (_state == QueueState.ShowingBanner)
            {
                _presenter.Skip();
            }
        }

        public int QueueCount => _queue.Count;
        public bool IsProcessing => _state != QueueState.Idle;
        public UIBannerType? CurrentBanner => _currentBannerType;
    }
}
