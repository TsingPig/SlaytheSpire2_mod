using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 手里剑（Shuriken）——攻击牌。
/// 造成 10（升级 13）点伤害；若未被完全格挡，则对目标施加 2（升级 3）层流血。
/// 注意：本牌为“非消耗”，打出后正常进入弃牌堆。
/// </summary>
public class Shuriken : NinjaModCard
{
    public Shuriken() : base(BalanceCost(nameof(Shuriken), 1), BalanceType(nameof(Shuriken), CardType.Attack), BalanceRarity(nameof(Shuriken), CardRarity.Basic), BalanceTarget(nameof(Shuriken), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 10m), ValueProp.Move), new PowerVar<BleedPower>("Bleed", BalanceDecimal("BaseBleed", 2m))];

    /// <summary>打出效果：造成攻击伤害，若真正打掉血量则附加流血。</summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 只有在“有伤害真正穿过格挡打到血量”时才施加流血。
        if (attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0)
        {
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target,
                (int)DynamicVars["Bleed"].BaseValue, Owner.Creature, this);
        }
    }

    /// <summary>升级：伤害 +3，流血层数提升到 3。</summary>
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m));
        DynamicVars["Bleed"].UpgradeValueBy(BalanceDelta("BaseBleed", "UpgradeBleed", 1m));
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("手里剑", "造成 {Damage:diff()} 点伤害。如果未被完全格挡，施加 {Bleed:diff()} 层[gold]流血[/gold]。")
        : new CardLoc("Shuriken", "Deal {Damage:diff()} damage. If unblocked, apply {Bleed:diff()} [gold]Bleed[/gold].");
}
