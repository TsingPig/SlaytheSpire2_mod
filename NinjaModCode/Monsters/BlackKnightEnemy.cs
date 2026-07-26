using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using NinjaMod.NinjaModCode.Extensions;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>黑暗骑士 Boss。</summary>
public sealed class BlackKnightEnemy : CustomMonsterModel, ILocalizationProvider
{
    public override int MinInitialHp => BlackKnightConfig.MaxHp;
    public override int MaxInitialHp => BlackKnightConfig.MaxHp;
    public override string? CustomVisualPath => BlackKnightConfig.VisualSceneRelPath.ScenePath();

    public bool InTrueForm { get; set; }

    private BlackKnightActions? _actions;
    private BlackKnightActions Actions => _actions ??= new BlackKnightActions(this);

    private BlackKnightAnimationController? _anim;
    internal BlackKnightAnimationController Anim =>
        _anim ??= new BlackKnightAnimationController(Creature);

    private BlackKnightVfxController? _vfx;
    internal BlackKnightVfxController Vfx =>
        _vfx ??= new BlackKnightVfxController(Creature);

    private BlackKnightSfxController? _sfx;
    internal BlackKnightSfxController Sfx => _sfx ??= new BlackKnightSfxController();

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        try { BlackKnightLoc.EnsureMonsterLoc(Id.Entry); }
        catch { /* AfterAddedToRoom 会再次注入。 */ }
        return BlackKnightStateMachine.Build(Actions);
    }

    public override async Task AfterAddedToRoom()
    {
        BlackKnightLoc.EnsureInjected();
        BlackKnightLoc.EnsureMonsterLoc(Id.Entry);
        BlackKnightMusicController.Start(Creature);

        // 常驻规则词条：逻辑仍由 BeforeSideTurnEnd 统一处理，这里只把规则明确展示给玩家。
        if (Creature.GetPower<NetherExecutionPower>() == null)
        {
            await PowerCmd.Apply<NetherExecutionPower>(
                new ThrowingPlayerChoiceContext(),
                Creature,
                1,
                Creature,
                null);
        }

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

        BlackKnightLog.Info(
            $"黑暗骑士进入战斗：HP {MinInitialHp}，起始意图 {NextMove?.StateId ?? BlackKnightConfig.StateCurse}。");
    }

    /// <summary>
    /// 玩家回合结束前只检查该回合参与者的手牌。
    /// 标记会在下一次该玩家回合结束时被重算，或在竖劈命中该玩家后被消耗。
    /// </summary>
    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        foreach (Creature participant in participants)
        {
            Player? player = participant.Player;
            if (player == null)
                continue;

            bool hasNetherCurse = PileType.Hand
                .GetPile(player)
                .Cards
                .Any(card => card is NetherCurse);
            var exposure = participant.GetPower<VerticalSlashExposurePower>();

            if (hasNetherCurse && exposure == null)
            {
                await PowerCmd.Apply<VerticalSlashExposurePower>(
                    choiceContext,
                    participant,
                    1,
                    Creature,
                    null);
            }
            else if (!hasNetherCurse && exposure != null)
            {
                await PowerCmd.Remove(exposure);
            }
        }
    }

    public List<(string, string)>? Localization => Lang.Zh
        ? new MonsterLoc("黑暗骑士", ZhMoveTitles())
        : new MonsterLoc("Black Knight", EnMoveTitles());

    private static IEnumerable<(string, string)> ZhMoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "诅咒发放"),
        (BlackKnightConfig.StateDiagonalA, "斜劈"),
        (BlackKnightConfig.StateDiagonalB, "斜劈"),
        (BlackKnightConfig.StateHorizontal, "横劈"),
        (BlackKnightConfig.StateVertical, "竖劈"),
        (BlackKnightConfig.StateDarkArmor, "暗魂铠甲"),
        (BlackKnightConfig.StateTrueForm, "强化——真身"),
        (BlackKnightConfig.StateTrueFormHorizA, "横劈"),
        (BlackKnightConfig.StateTrueFormHorizB, "横劈"),
        (BlackKnightConfig.StateTrueFormVertical, "竖劈"),
    ];

    private static IEnumerable<(string, string)> EnMoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "Curse Cast"),
        (BlackKnightConfig.StateDiagonalA, "Diagonal Slash"),
        (BlackKnightConfig.StateDiagonalB, "Diagonal Slash"),
        (BlackKnightConfig.StateHorizontal, "Horizontal Slash"),
        (BlackKnightConfig.StateVertical, "Vertical Slash"),
        (BlackKnightConfig.StateDarkArmor, "Dark Armor"),
        (BlackKnightConfig.StateTrueForm, "Empower — True Form"),
        (BlackKnightConfig.StateTrueFormHorizA, "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormHorizB, "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormVertical, "Vertical Slash"),
    ];
}
