using System;
using System.Collections.Generic;
using System.Linq;
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
/// Shuriken (手里剑) - attack. Deal 9 (12) damage; if not fully blocked, apply 2 (3) Bleed. Exhaust.
/// </summary>
public class Shuriken : NinjaModCard
{
    private int _bleed = 2;

    public Shuriken() : base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0)
        {
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, _bleed, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        _bleed = 3;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("手里剑", "造成 9 点伤害。如果未被完全格挡，施加 2 层流血。消耗。")
        : new CardLoc("Shuriken", "Deal 9 damage. If unblocked, apply 2 Bleed. Exhaust.");
}
