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

/// <summary>Ninja Defend (忍者防御) - basic skill. Gain 5 (8 upgraded) Block.</summary>
public class NinjaDefend : NinjaModCard
{
    public NinjaDefend() : base(BalanceCost(nameof(NinjaDefend), 1), BalanceType(nameof(NinjaDefend), CardType.Skill), BalanceRarity(nameof(NinjaDefend), CardRarity.Basic), BalanceTarget(nameof(NinjaDefend), TargetType.Self)) { }

    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BalanceDecimal("BaseBlock", 5m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 3m));

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("忍者防御", "获得 {Block:diff()} 点格挡。")
        : new CardLoc("Ninja Defend", "Gain {Block:diff()} Block.");
}
