using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 回天：绝对防御（Reversal: Absolute Defense）——技能牌（稀有）。
/// 2 费，获得 20（升级 30）点格挡，并回复 4（升级 6）点生命。
/// </summary>
public class AbsoluteDefense : NinjaModCard
{
    public AbsoluteDefense() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(20m, ValueProp.Move), new HealVar(4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(10m); // 20 -> 30
        DynamicVars.Heal.UpgradeValueBy(2m);   // 4 -> 6
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("回天：绝对防御", "获得 {Block:diff()} 点格挡，回复 {Heal:diff()} 点生命。")
        : new CardLoc("Reversal: Absolute Defense", "Gain {Block:diff()} Block and heal {Heal:diff()} HP.");
}
