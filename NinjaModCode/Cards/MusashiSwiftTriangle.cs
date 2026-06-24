using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：迅光三角剑（Musashi: Swift Triangle）——技能牌。
/// 1 费，造成 11（升级 15）点伤害，并短暂获得 3（升级 4）点敏捷（本回合内）。
/// </summary>
public class MusashiSwiftTriangle : NinjaModCard
{
    public MusashiSwiftTriangle() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override bool IsMusashi => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(11m, ValueProp.Move), new IntVar("Dex", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature,
            DynamicVars["Dex"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);   // 11 -> 15
        DynamicVars["Dex"].UpgradeValueBy(1m);   // 3 -> 4
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：迅光三角剑", "造成 {Damage:diff()} 点伤害，获得 {Dex:diff()} 点[gold]敏捷[/gold]。")
        : new CardLoc("Musashi: Swift Triangle", "Deal {Damage:diff()} damage and gain {Dex:diff()} [gold]Dexterity[/gold].");
}
