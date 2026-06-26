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
/// 火忍：火焰弹幕（Fire Ninjutsu: Flame Barrage）——技能牌。
/// 造成 2×3（3×3升级）点伤害，然后施加 3（4）层燃烧。
/// </summary>
public class FlameBarrage : NinjaModCard
{
    public FlameBarrage() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(2m, ValueProp.Move), new RepeatVar(3), new PowerVar<BurningPower>("Burning", 3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target,
            (int)DynamicVars["Burning"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);     // 2 -> 3
        DynamicVars["Burning"].UpgradeValueBy(1m); // 3 -> 4
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火焰弹幕", "造成 {Repeat:diff()} 次 {Damage:diff()} 点伤害，然后施加 {Burning:diff()} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Flame Barrage", "Deal {Damage:diff()} damage {Repeat:diff()} times, then apply {Burning:diff()} [gold]Burning[/gold].");
}
