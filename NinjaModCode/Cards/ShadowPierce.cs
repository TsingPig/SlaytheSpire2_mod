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
/// 影心刺（Shadow Pierce）——攻击牌（稀有）。
/// 0 费，造成 9（升级 14）点伤害，附加 5（升级 6）层流血。【静默】：打出不破除隐身。
/// </summary>
public class ShadowPierce : NinjaModCard
{
    public ShadowPierce() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool HasSilence => true;

    // 伤害 9（升级 14）；流血 5（升级 6），用 PowerVar 显示并自动关联流血提示。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move), new PowerVar<BleedPower>("Bleed", 5m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target,
            (int)DynamicVars["Bleed"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);   // 9 -> 14
        DynamicVars["Bleed"].UpgradeValueBy(1m); // 5 -> 6
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("影心刺", "造成 {Damage:diff()} 点伤害，附加 {Bleed:diff()} 层[gold]流血[/gold]。[gold]静默[/gold]。")
        : new CardLoc("Shadow Pierce", "Deal {Damage:diff()} damage and apply {Bleed:diff()} [gold]Bleed[/gold]. [gold]Silence[/gold].");
}
