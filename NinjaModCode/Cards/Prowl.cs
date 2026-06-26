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
/// 潜行（Prowl）——技能牌。
/// 0 费，获得 4 点格挡；并施加 <see cref="ProwlPower"/>，使下个回合开始时获得一张暗杀。
/// </summary>
public class Prowl : NinjaModCard
{
    public Prowl() : base(BalanceCost(nameof(Prowl), 0), BalanceType(nameof(Prowl), CardType.Skill), BalanceRarity(nameof(Prowl), CardRarity.Common), BalanceTarget(nameof(Prowl), TargetType.Self)) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BalanceDecimal("BaseBlock", 4m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<ProwlPower>(choiceContext, Owner.Creature, BalanceValue("BaseProwl", 1), Owner.Creature, this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("潜行", "获得 {Block:diff()} 点格挡。下个回合开始时，获得一张[gold]暗杀[/gold]。")
        : new CardLoc("Prowl", "Gain {Block:diff()} Block. At the start of your next turn, add an [gold]Assassination[/gold] to your hand.");
}
