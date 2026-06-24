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

/// <summary>
/// Assassination (暗杀) - attack. Deal 7 (10) damage ignoring Block.
/// Dealt as a move (attack) that is also Unblockable, so it counts as unblocked HP damage and
/// triggers existing Bleed, but it does not apply new Bleed itself.
/// </summary>
public class Assassination : NinjaModCard
{
    public Assassination() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    public override bool PreservesStealth => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // Move => counts as an attack (triggers Bleed); Unblockable => ignores the target's Block.
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage.BaseValue,
            ValueProp.Move | ValueProp.Unblockable, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("暗杀", "无视格挡，造成 {Damage:diff()} 点伤害。打出这张牌不会失去[gold]隐身[/gold]。")
        : new CardLoc("Assassination", "Deal {Damage:diff()} damage ignoring Block. Playing this card does not break [gold]Stealth[/gold].");
}
