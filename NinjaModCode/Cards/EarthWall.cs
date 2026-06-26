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
/// Earth Ninjutsu: Earth Wall (土忍：土墙) - Skill.
/// Gain 7 (10 upgraded) Block and gain Debuff Immunity until the end of next turn.
/// </summary>
public class EarthWall : NinjaModCard
{
    public EarthWall() : base(BalanceCost(nameof(EarthWall), 1), BalanceType(nameof(EarthWall), CardType.Skill), BalanceRarity(nameof(EarthWall), CardRarity.Common), BalanceTarget(nameof(EarthWall), TargetType.Self)) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BalanceDecimal("BaseBlock", 7m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        // Amount 2 => lasts until the end of the next turn.
        await PowerCmd.Apply<DebuffImmunityPower>(choiceContext, Owner.Creature, BalanceValue("BaseDebuffImmunity", 2), Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 3m));

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：土墙", "获得 {Block:diff()} 点格挡，并获得[gold]免疫负面[/gold]效果 2 个回合。")
        : new CardLoc("Earth Ninjutsu: Earth Wall", "Gain {Block:diff()} Block and gain [gold]Debuff Immunity[/gold] until the end of next turn.");
}
