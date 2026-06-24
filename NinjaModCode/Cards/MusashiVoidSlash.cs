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
/// 武藏：空明斩（Musashi: Void Slash）——攻击牌，消耗。
/// 0 费，造成 15（升级 21）点伤害，获得 1（升级 2）点抵挡。
/// </summary>
public class MusashiVoidSlash : NinjaModCard
{
    public MusashiVoidSlash() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    public override bool IsMusashi => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(15m, ValueProp.Move), new IntVar("Resist", 1m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature,
            DynamicVars["Resist"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);   // 15 -> 21
        DynamicVars["Resist"].UpgradeValueBy(1m); // 1 -> 2
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：空明斩", "造成 {Damage:diff()} 点伤害，获得 {Resist:diff()} 点[gold]抵挡[/gold]。")
        : new CardLoc("Musashi: Void Slash", "Deal {Damage:diff()} damage and gain {Resist:diff()} [gold]Resist[/gold].");
}
