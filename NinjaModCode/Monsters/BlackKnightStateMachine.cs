using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 构建黑骑士的意图状态机。使用明确命名的 <see cref="MoveState"/> 顺序链接
/// （<c>FollowUpStateId</c>）实现严格固定的循环，真身阶段的三次行动是独立命名状态，
/// 因此战斗存档/读取会精确恢复当前阶段与“真身剩余攻击”，而不是靠易错的整数自增。
///
/// 完整循环：
///   诅咒发放 → 斜劈 → 斜劈 → 横砍 → 竖劈+ → 暗鬼铠甲 → 强化—真身
///   → 横砍 → 横砍 → 竖劈+ → （返回）诅咒发放
/// </summary>
internal static class BlackKnightStateMachine
{
    /// <summary>
    /// 把行动包一层防御性 try/catch：任何未预期的异常都记录日志并让回合正常结束，
    /// 绝不因单个行动抛异常而卡死玩家的整场战斗/存档。
    /// </summary>
    private static Func<IReadOnlyList<Creature>, Task> Guard(
        Func<IReadOnlyList<Creature>, Task> action, string label) =>
        async targets =>
        {
            try { await action(targets); }
            catch (Exception e) { BlackKnightLog.Warn($"行动『{label}』发生异常，已跳过以避免卡死：{e}"); }
        };

    public static MonsterMoveStateMachine Build(BlackKnightActions a)
    {
        BlackKnightLoc.EnsureInjected();

        var curse = new MoveState(
            BlackKnightConfig.StateCurse, Guard(a.CurseCast, "诅咒发放"),
            new BlackKnightIntents.CurseCast())
        { FollowUpStateId = BlackKnightConfig.StateDiagonalA };

        var diagonalA = new MoveState(
            BlackKnightConfig.StateDiagonalA, Guard(a.DiagonalSlashA, "斜劈A"),
            new BlackKnightIntents.Slash(() => BlackKnightConfig.DiagonalSlashDamage, "BK_DIAGONAL"))
        { FollowUpStateId = BlackKnightConfig.StateDiagonalB };

        var diagonalB = new MoveState(
            BlackKnightConfig.StateDiagonalB, Guard(a.DiagonalSlashB, "斜劈B"),
            new BlackKnightIntents.Slash(() => BlackKnightConfig.DiagonalSlashDamage, "BK_DIAGONAL"))
        { FollowUpStateId = BlackKnightConfig.StateHorizontal };

        var horizontal = new MoveState(
            BlackKnightConfig.StateHorizontal, Guard(a.HorizontalSlash, "横砍"),
            new BlackKnightIntents.Slash(() => BlackKnightConfig.HorizontalSlashDamage, "BK_HORIZONTAL"))
        { FollowUpStateId = BlackKnightConfig.StateVertical };

        var vertical = new MoveState(
            BlackKnightConfig.StateVertical, Guard(a.VerticalSlash, "竖劈+"),
            new BlackKnightIntents.Vertical(() => BlackKnightConfig.VerticalSlashDamage))
        { FollowUpStateId = BlackKnightConfig.StateDarkArmor };

        var darkArmor = new MoveState(
            BlackKnightConfig.StateDarkArmor, Guard(a.DarkArmor, "暗鬼铠甲"),
            new BlackKnightIntents.DarkArmor(), new DebuffIntent())
        { FollowUpStateId = BlackKnightConfig.StateTrueForm };

        var trueForm = new MoveState(
            BlackKnightConfig.StateTrueForm, Guard(a.TrueForm, "强化真身"),
            new BlackKnightIntents.TrueForm())
        { FollowUpStateId = BlackKnightConfig.StateTrueFormHorizA };

        var tfHorizontalA = new MoveState(
            BlackKnightConfig.StateTrueFormHorizA, Guard(a.TrueFormHorizontalA, "真身横砍A"),
            new BlackKnightIntents.Slash(() => BlackKnightConfig.HorizontalSlashDamage, "BK_HORIZONTAL"))
        { FollowUpStateId = BlackKnightConfig.StateTrueFormHorizB };

        var tfHorizontalB = new MoveState(
            BlackKnightConfig.StateTrueFormHorizB, Guard(a.TrueFormHorizontalB, "真身横砍B"),
            new BlackKnightIntents.Slash(() => BlackKnightConfig.HorizontalSlashDamage, "BK_HORIZONTAL"))
        { FollowUpStateId = BlackKnightConfig.StateTrueFormVertical };

        var tfVertical = new MoveState(
            BlackKnightConfig.StateTrueFormVertical, Guard(a.TrueFormVertical, "真身竖劈+"),
            new BlackKnightIntents.Vertical(() => BlackKnightConfig.VerticalSlashDamage))
        { FollowUpStateId = BlackKnightConfig.StateCurse }; // 循环回到诅咒发放

        var states = new List<MonsterState>
        {
            curse, diagonalA, diagonalB, horizontal, vertical,
            darkArmor, trueForm, tfHorizontalA, tfHorizontalB, tfVertical,
        };

        return new MonsterMoveStateMachine(states, curse);
    }
}
