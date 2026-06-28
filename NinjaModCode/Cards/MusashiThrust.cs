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
/// 武藏：刺（Musashi: Thrust）——攻击牌，消耗。
/// 0 费，造成 9 点伤害，附加 2 层流血。
/// </summary>
public class MusashiThrust : NinjaModCard
{
    public MusashiThrust() : base(BalanceCost(nameof(MusashiThrust), 0), BalanceType(nameof(MusashiThrust), CardType.Attack), BalanceRarity(nameof(MusashiThrust), CardRarity.Common), BalanceTarget(nameof(MusashiThrust), TargetType.AnyEnemy)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiThrust), true);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 伤害 9->12、流血 2->3 均为 DynamicVar，{:diff()} 可显示升级预览。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 9m), ValueProp.Move), new PowerVar<BleedPower>("Bleed", BalanceDecimal("BaseBleed", 2m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, DynamicVars["Bleed"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m)); // 9 -> 12
        DynamicVars["Bleed"].UpgradeValueBy(BalanceDelta("BaseBleed", "UpgradeBleed", 1m)); // 2 -> 3
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：刺", "造成 {Damage:diff()} 点伤害，附加 {Bleed:diff()} 层[gold]流血[/gold]。")
        : new CardLoc("Musashi: Thrust", "Deal {Damage:diff()} damage. Apply {Bleed:diff()} [gold]Bleed[/gold].");
}
