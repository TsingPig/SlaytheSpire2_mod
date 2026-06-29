using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 刀意流转（Blade Flow）——技能牌（罕见）。
/// 2（升级 1）费，对目标自动免费打出手牌中的所有【飞刀】与【手里剑】。
/// </summary>
public class BladeFlow : NinjaModCard
{
    public BladeFlow() : base(BalanceCost(nameof(BladeFlow), 2), BalanceType(nameof(BladeFlow), CardType.Skill), BalanceRarity(nameof(BladeFlow), CardRarity.Uncommon), BalanceTarget(nameof(BladeFlow), TargetType.AnyEnemy)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var target = cardPlay.Target;
        var hand = CardPile.Get(PileType.Hand, Owner);
        if (hand == null) return;

        var cardsToPlay = hand.Cards
            .Where(c => c is Kunai or Shuriken)
            .ToList();

        foreach (var card in cardsToPlay)
        {
            if (target.CurrentHp <= 0) break;
            if (card.Pile != hand) continue;

            await CardCmd.AutoPlay(choiceContext, card, target);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(BladeFlow), -1)); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("刀意流转", "对目标自动免费打出手牌中的所有[gold]飞刀[/gold]与[gold]手里剑[/gold]。")
        : new CardLoc("Blade Flow", "Auto-play all [gold]Kunai[/gold] and [gold]Shuriken[/gold] from your hand at the target for free.");
}
