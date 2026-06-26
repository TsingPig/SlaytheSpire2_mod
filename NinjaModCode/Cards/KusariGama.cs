using System;
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
/// 锁镰（Kusari-Gama）——攻击牌。
/// 1 费，造成 9（升级 12）点伤害；若目标有流血，则额外造成 4（升级 6）点伤害。
/// </summary>
public class KusariGama : NinjaModCard
{
    public KusariGama() : base(BalanceCost(nameof(KusariGama), 1), BalanceType(nameof(KusariGama), CardType.Attack), BalanceRarity(nameof(KusariGama), CardRarity.Uncommon), BalanceTarget(nameof(KusariGama), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 9m), ValueProp.Move), new ExtraDamageVar(BalanceDecimal("BaseExtraDamage", 4m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hadBleed = cardPlay.Target.GetPower<BleedPower>() != null;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (hadBleed)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.ExtraDamage.BaseValue,
                ValueProp.Move, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m)); // 9 -> 12
        DynamicVars.ExtraDamage.UpgradeValueBy(BalanceDelta("BaseExtraDamage", "UpgradeExtraDamage", 2m)); // 4 -> 6
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("锁镰", "造成 {Damage:diff()} 点伤害。如果目标有[gold]流血[/gold]，额外造成 {ExtraDamage:diff()} 点伤害。")
        : new CardLoc("Kusari-Gama", "Deal {Damage:diff()} damage. If the target has [gold]Bleed[/gold], deal {ExtraDamage:diff()} extra damage.");
}
