using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：唤石（Earth Ninjutsu: Stone Summon）——技能牌。
/// 1 费，获得（当前抵挡层数 × 4，升级为 × 5）点格挡。
/// 倍率用 <see cref="_multiplier"/> 变量表示，随升级改变。
/// </summary>
public class StoneSummon : NinjaModCard
{
    // 格挡倍率，升级后提升到 5。
    private int _multiplier = 4;

    public StoneSummon() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 读取自身抵挡（Resist）层数，乘以倍率作为格挡量。
        var resist = Owner.Creature.GetPower<ResistPower>();
        decimal stacks = resist?.Amount ?? 0m;
        decimal block = stacks * _multiplier;

        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(block, ValueProp.Move), cardPlay);
    }

    protected override void OnUpgrade() => _multiplier = 5;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：唤石", $"获得（当前抵挡层数 × {_multiplier}）点格挡。")
        : new CardLoc("Earth Ninjutsu: Stone Summon", $"Gain Block equal to {_multiplier}× your current Resist.");
}
