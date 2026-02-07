// ============================================================
// IDuelPresentation.cs — 对决演出系统接口
// 架构层级: Presentation (积木式接口)
// 说明: 镜头/子空间UI/选牌输入。MVP允许非常简陋。
//       Enter后冻结控制, Pick/Lock有倒计时, Exit后Banner弹出。
// ============================================================

using RacingCardGame.Core;

namespace RacingCardGame.Presentation
{
    /// <summary>
    /// 对决演出系统接口
    /// </summary>
    public interface IDuelPresentation
    {
        void EnterSubspace(int attackerId, int defenderId);
        void ExitSubspace();
        bool IsInSubspace { get; }

        /// <summary>
        /// 请求选牌 — 返回双方选择的 CardType
        /// attackerSlots/defenderSlots: 可选牌列表
        /// </summary>
        CardPickResult RequestCardPick(
            CardType[] attackerSlots, CardType[] defenderSlots,
            float timeLimit, bool attackerIsHuman, bool defenderIsHuman);
    }

    /// <summary>
    /// 选牌结果
    /// </summary>
    public class CardPickResult
    {
        public CardType AttackerChoice;
        public CardType DefenderChoice;
    }
}
