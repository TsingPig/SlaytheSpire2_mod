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
/// 土忍：聚石刺（Earth Ninjutsu: Stone Gather Thrust）——攻击牌。
/// 1 费，造成 6（升级 9）点伤害，获得 3（升级 4）层抵挡。
/// </summary>
public class StoneGatherThrust : NinjaModCard
{
    public StoneGatherThrust() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move), new PowerVar<ResistPower>("Resist", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature,
            (int)DynamicVars["Resist"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);    // 6 -> 9
        DynamicVars["Resist"].UpgradeValueBy(1m); // 3 -> 4
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：聚石刺", "造成 {Damage:diff()} 点伤害，获得 {Resist:diff()} 层[gold]抵挡[/gold]。")
        : new CardLoc("Earth Ninjutsu: Stone Gather Thrust", "Deal {Damage:diff()} damage and gain {Resist:diff()} [gold]Resist[/gold].");
}
