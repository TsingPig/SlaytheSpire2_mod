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
/// 起承拳（Rising Fist）——攻击牌。
/// 1 费，造成 6（升级 9）点伤害；若手牌中有飞刀（<see cref="Kunai"/>），
/// 则自动免费打出一张飞刀（结算其伤害与流血，对同一目标）。
/// </summary>
public class RisingFist : NinjaModCard
{
    // 飞刀的基础伤害（与 Kunai 一致）。
    private const int KunaiDamage = 5;

    public RisingFist() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 手牌中若有飞刀，自动免费打出一张：结算飞刀伤害+流血，并从手牌移除。
        var kunai = CardPile.GetCards(Owner, new[] { PileType.Hand })
            .OfType<Kunai>()
            .FirstOrDefault();
        if (kunai != null)
        {
            var attack = await DamageCmd.Attack(KunaiDamage)
                .FromCard(kunai)
                .Targeting(cardPlay.Target)
                .WithHitFx(NinjaConstants.SlashVfx)
                .Execute(choiceContext);

            if (attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0)
            {
                await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, kunai);
            }
            await CardPileCmd.RemoveFromCombat(kunai, true);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m); // 6 -> 9

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("起承拳", $"造成 {DynamicVars.Damage.BaseValue} 点伤害。如果手牌中有飞刀，自动免费打出一张飞刀。")
        : new CardLoc("Rising Fist", $"Deal {DynamicVars.Damage.BaseValue} damage. If you have a Kunai in hand, auto-play one for free.");
}
