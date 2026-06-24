using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Kunai (飞刀) - a generated TEMPORARY attack added to hand by the Ninja passive (Hidden Blade).
/// Retain + Exhaust. Deal 5 (7) damage; if not fully blocked, apply 1 Bleed.
///
/// It is a normal registered <see cref="NinjaModCard"/> with <see cref="CardRarity.Token"/> rarity.
/// Token cards are never offered in card rewards (rewards only roll Common/Uncommon/Rare), so it
/// does not pollute the reward pool, yet it IS registered in the model database so it can be
/// generated at runtime via <see cref="CreateInHand"/> (the same pattern the base game uses for Shiv).
/// </summary>
public class Kunai : NinjaModCard
{
    public Kunai() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

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
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    /// <summary>
    /// Create a fresh temporary Kunai directly into the owner's hand.
    /// Mirrors the base game's <c>Shiv.CreateInHand</c> (CreateCard + AddGeneratedCardToCombat).
    /// </summary>
    public static async Task CreateInHand(Player owner, ICombatState combatState)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var kunai = combatState.CreateCard<Kunai>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(kunai, PileType.Hand, owner);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("飞刀", "[gold]保留[/gold]。[gold]消耗[/gold]。造成 {Damage:diff()} 点伤害。如果未被完全格挡，施加 1 层[gold]流血[/gold]。")
        : new CardLoc("Kunai", "[gold]Retain[/gold]. [gold]Exhaust[/gold]. Deal {Damage:diff()} damage. If unblocked, apply 1 [gold]Bleed[/gold].");
}
