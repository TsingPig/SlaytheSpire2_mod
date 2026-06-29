using System;
using System.Collections.Generic;
using System.Linq;
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
/// 燕返（Swallow Return）——攻击牌（普通）。
/// 1 费，造成 4（升级 7）点伤害；若伤害被完全格挡，获得 1 点能量。
/// </summary>
public class SwallowReturn : NinjaModCard
{
    public SwallowReturn() : base(BalanceCost(nameof(SwallowReturn), 1), BalanceType(nameof(SwallowReturn), CardType.Attack), BalanceRarity(nameof(SwallowReturn), CardRarity.Common), BalanceTarget(nameof(SwallowReturn), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 4m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) <= 0)
        {
            await PlayerCmd.GainEnergy(BalanceValue("BaseSwallowReturnEnergy", 1), Owner);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m)); // 4 -> 7

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("燕返", "造成 {Damage:diff()} 点伤害。如果伤害被完全格挡，获得 1 点能量。")
        : new CardLoc("Swallow Return", "Deal {Damage:diff()} damage. If fully blocked, gain 1 Energy.");
}
