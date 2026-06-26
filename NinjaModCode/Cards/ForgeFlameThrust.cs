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
/// 火忍：锻火刺（Fire Ninjutsu: Forge Flame Thrust）——攻击牌。
/// 1 费，造成 6（升级 9）点伤害，附加 4（升级 5）层燃烧。
/// </summary>
public class ForgeFlameThrust : NinjaModCard
{
    public ForgeFlameThrust() : base(BalanceCost(nameof(ForgeFlameThrust), 1), BalanceType(nameof(ForgeFlameThrust), CardType.Attack), BalanceRarity(nameof(ForgeFlameThrust), CardRarity.Common), BalanceTarget(nameof(ForgeFlameThrust), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 6m), ValueProp.Move), new PowerVar<BurningPower>("Burning", BalanceDecimal("BaseBurning", 4m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target,
            (int)DynamicVars["Burning"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m));     // 6 -> 9
        DynamicVars["Burning"].UpgradeValueBy(BalanceDelta("BaseBurning", "UpgradeBurning", 1m)); // 4 -> 5
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：锻火刺", "造成 {Damage:diff()} 点伤害，附加 {Burning:diff()} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Forge Flame Thrust", "Deal {Damage:diff()} damage and apply {Burning:diff()} [gold]Burning[/gold].");
}
