using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 刀意流转（Blade Flow）——技能牌（罕见）。
/// 2（升级 1）费，对目标打出手牌中的所有【飞刀】与【手里剑】（含残影复制牌），每张造成其伤害并附加流血。
/// </summary>
public class BladeFlow : NinjaModCard
{
    public BladeFlow() : base(BalanceCost(nameof(BladeFlow), 2), BalanceType(nameof(BladeFlow), CardType.Skill), BalanceRarity(nameof(BladeFlow), CardRarity.Uncommon), BalanceTarget(nameof(BladeFlow), TargetType.AnyEnemy)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 手牌中所有飞刀与手里剑（含【残影】生成的复制牌，它们也是 Kunai/Shuriken 实例）。
        var thrown = CardPile.GetCards(Owner, new[] { PileType.Hand })
            .Where(c => c is Kunai or Shuriken)
            .ToList();

        foreach (var card in thrown)
        {
            if (cardPlay.Target.CurrentHp <= 0) break;
            int dmg = card.DynamicVars.Damage?.IntValue ?? 0;
            var attack = await DamageCmd.Attack(dmg)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx(NinjaConstants.SlashVfx)
                .Execute(choiceContext);

            bool unblocked = attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0;
            if (unblocked && cardPlay.Target.CurrentHp > 0)
            {
                int bleed;
                if (card is Shuriken && card.DynamicVars.TryGetValue("Bleed", out var bleedVar))
                {
                    bleed = (int)bleedVar.BaseValue;
                }
                else
                {
                    bleed = BalanceValue("BaseKunaiBleed", 1);
                }
                await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, bleed, Owner.Creature, this);
            }

            // 这张飞刀/手里剑已被“打出”：消耗带消耗的（飞刀/残影），其余进入弃牌堆。
            if (card.CanonicalKeywords.Contains(CardKeyword.Exhaust))
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
            else
            {
                await CardCmd.Discard(choiceContext, card);
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(BladeFlow), -1)); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("刀意流转", "对目标打出手牌中的所有[gold]飞刀[/gold]与[gold]手里剑[/gold]。")
        : new CardLoc("Blade Flow", "Play all [gold]Kunai[/gold] and [gold]Shuriken[/gold] from your hand at the target.");
}
