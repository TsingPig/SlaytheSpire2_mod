using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using NinjaMod.NinjaModCode.Extensions;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Kunai (飞刀) - a generated TEMPORARY attack added to hand by the Ninja passive.
/// Retain + Exhaust. Deal 5 (7) damage; if not fully blocked, apply 1 Bleed.
///
/// It inherits <see cref="CustomCardModel"/> directly (not <see cref="NinjaModCard"/>) and is
/// created with autoAdd:false so it is never added to the Ninja reward pool. It is still
/// registered in the model database (BaseLib auto-registers every model type), so it can be
/// generated at runtime. It is hidden from the card library.
/// </summary>
#pragma warning disable STS004 // Intentionally not in any pool: Kunai is a generated temporary card.
public class Kunai : CustomCardModel
{
    public Kunai() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy,
        showInCardLibrary: false, autoAdd: false)
    { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    // Card art (falls back to the placeholder card.png if kunai.png is absent).
    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

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

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("飞刀", "保留。消耗。造成 5 点伤害。如果未被完全格挡，施加 1 层流血。")
        : new CardLoc("Kunai", "Retain. Exhaust. Deal 5 damage. If unblocked, apply 1 Bleed.");
}
#pragma warning restore STS004
