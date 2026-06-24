using System;
using System.Collections.Generic;
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
/// 细雪（Light Snow）——攻击牌。
/// 0 费，造成 6 点伤害，回复 2（升级 3）点生命。
/// </summary>
public class LightSnow : NinjaModCard
{
    public LightSnow() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(1m, ValueProp.Move), new RepeatVar(6), new HealVar(2m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue, true);
    }

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(1m); // 2 -> 3

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("细雪", "造成 {Damage:diff()} 点伤害，共 {Repeat} 段，回复 {Heal:diff()} 点生命。")
        : new CardLoc("Light Snow", "Deal {Damage:diff()} damage {Repeat} times. Heal {Heal:diff()} HP.");
}
