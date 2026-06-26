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
    // 燃烧层数，升级后为 4。
    private int _burning = BalanceValue(nameof(FlameBarrage), "BaseBurning", 3);

    public FlameBarrage() : base(BalanceCost(nameof(FlameBarrage), 1), BalanceType(nameof(FlameBarrage), CardType.Skill), BalanceRarity(nameof(FlameBarrage), CardRarity.Uncommon), BalanceTarget(nameof(FlameBarrage), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 2m), ValueProp.Move), new RepeatVar(BalanceValue("BaseRepeat", 3))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target, _burning, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 1m)); // 2 -> 3
        _burning = BalanceValue("UpgradeBurning", 4);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火焰弹幕", $"造成 {{Repeat:diff()}} 次 {{Damage:diff()}} 点伤害，然后施加 {_burning} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Flame Barrage", $"Deal {{Damage:diff()}} damage {{Repeat:diff()}} times, then apply {_burning} [gold]Burning[/gold].");
}
