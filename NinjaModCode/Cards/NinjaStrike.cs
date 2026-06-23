using System;
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

/// <summary>Ninja Strike (忍者打击) - basic attack. Deal 6 (9 upgraded) damage.</summary>
public class NinjaStrike : NinjaModCard
{
    public NinjaStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("忍者打击", "造成 6 点伤害。")
        : new CardLoc("Ninja Strike", "Deal 6 damage.");
}
