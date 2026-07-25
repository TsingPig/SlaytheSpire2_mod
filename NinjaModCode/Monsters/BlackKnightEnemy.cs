using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using NinjaMod.NinjaModCode.Extensions;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士（Black Knight）——Boss 级敌人。测试阶段允许出现在第一幕第一场普通战斗
/// （见 <see cref="BlackKnightDebugEncounterOverride"/>）。
///
/// 500 点生命的浮空敌人：无腿、下半身为披风与幽魂尾迹，因此视觉场景使用分层 Sprite2D，
/// 不使用落地/行走/脚步动画。战斗行为由 <see cref="BlackKnightStateMachine"/> 描述的固定意图
/// 循环驱动，具体行动实现在 <see cref="BlackKnightActions"/>。
///
/// 所有伤害走游戏正式伤害管线并受力量等通用效果影响；关键数值集中在 <see cref="BlackKnightConfig"/>。
/// </summary>
public sealed class BlackKnightEnemy : CustomMonsterModel, ILocalizationProvider
{
    // 500 点最大生命 = 初始生命。
    public override int MinInitialHp => BlackKnightConfig.MaxHp;
    public override int MaxInitialHp => BlackKnightConfig.MaxHp;

    // 战斗视觉：分层 Sprite2D + AnimationPlayer 场景（非 Spine）。
    public override string? CustomVisualPath => BlackKnightConfig.VisualSceneRelPath.ScenePath();

    /// <summary>真身强化阶段的运行时标记（力量本身由 StrengthPower 持久化，随存档保存）。</summary>
    public bool InTrueForm { get; set; }

    private BlackKnightActions? _actions;
    private BlackKnightActions Actions => _actions ??= new BlackKnightActions(this);

    private BlackKnightAnimationController? _anim;
    internal BlackKnightAnimationController Anim => _anim ??= new BlackKnightAnimationController(Creature);

    private BlackKnightVfxController? _vfx;
    internal BlackKnightVfxController Vfx => _vfx ??= new BlackKnightVfxController(Creature);

    private BlackKnightSfxController? _sfx;
    internal BlackKnightSfxController Sfx => _sfx ??= new BlackKnightSfxController();

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 兜底注入怪物名字/招式标题（防止名字回退显示为 mod 名）。
        try { BlackKnightLoc.EnsureMonsterLoc(Id.Entry); } catch { /* Id 尚未就绪时忽略，AfterAddedToRoom 会再注入 */ }
        return BlackKnightStateMachine.Build(Actions);
    }

    public override Task AfterAddedToRoom()
    {
        BlackKnightLoc.EnsureInjected();
        BlackKnightLoc.EnsureMonsterLoc(Id.Entry);

        // 存档/读取后若处于真身阶段，重新显现幽影并切到真身待机。
        if (BlackKnightConfig.IsTrueFormState(NextMove?.StateId))
        {
            InTrueForm = true;
            Anim.ShowPhantom(true);
            Anim.PlayIdle(trueForm: true);
        }
        else
        {
            Anim.ShowPhantom(false);
            Anim.PlayIdle(trueForm: false);
        }
        BlackKnightLog.Info($"黑骑士进入战斗：HP {MinInitialHp}，起始意图 {NextMove?.StateId ?? BlackKnightConfig.StateCurse}。");
        return Task.CompletedTask;
    }

    public List<(string, string)>? Localization => Lang.Zh
        ? new MonsterLoc("黑骑士", ZhMoveTitles())
        : new MonsterLoc("Black Knight", EnMoveTitles());

    private static IEnumerable<(string, string)> ZhMoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "诅咒发放"),
        (BlackKnightConfig.StateDiagonalA, "斜劈"),
        (BlackKnightConfig.StateDiagonalB, "斜劈"),
        (BlackKnightConfig.StateHorizontal, "横砍"),
        (BlackKnightConfig.StateVertical, "竖劈+"),
        (BlackKnightConfig.StateDarkArmor, "暗鬼铠甲"),
        (BlackKnightConfig.StateTrueForm, "强化——真身"),
        (BlackKnightConfig.StateTrueFormHorizA, "横砍"),
        (BlackKnightConfig.StateTrueFormHorizB, "横砍"),
        (BlackKnightConfig.StateTrueFormVertical, "竖劈+"),
    ];

    private static IEnumerable<(string, string)> EnMoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "Curse Cast"),
        (BlackKnightConfig.StateDiagonalA, "Diagonal Slash"),
        (BlackKnightConfig.StateDiagonalB, "Diagonal Slash"),
        (BlackKnightConfig.StateHorizontal, "Horizontal Slash"),
        (BlackKnightConfig.StateVertical, "Vertical Slash+"),
        (BlackKnightConfig.StateDarkArmor, "Dark Armor"),
        (BlackKnightConfig.StateTrueForm, "Empower — True Form"),
        (BlackKnightConfig.StateTrueFormHorizA, "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormHorizB, "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormVertical, "Vertical Slash+"),
    ];
}
