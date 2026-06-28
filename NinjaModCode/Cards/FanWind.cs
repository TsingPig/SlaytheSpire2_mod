using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：扇风（Fire Ninjutsu: Fan Wind）——技能牌（罕见）。
/// 2（升级 1）费，将目标的【燃烧】层数翻倍。
/// </summary>
public class FanWind : NinjaModCard
{
    public FanWind() : base(BalanceCost(nameof(FanWind), 2), BalanceType(nameof(FanWind), CardType.Skill), BalanceRarity(nameof(FanWind), CardRarity.Uncommon), BalanceTarget(nameof(FanWind), TargetType.AnyEnemy)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int burning = cardPlay.Target.GetPower<BurningPower>()?.Amount ?? 0;
        if (burning <= 0) return;

        // 翻倍：再追加等量的燃烧。
        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target, burning, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(FanWind), -1)); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：扇风", "将目标的[gold]燃烧[/gold]层数翻倍。")
        : new CardLoc("Fire Ninjutsu: Fan Wind", "Double the target's [gold]Burning[/gold].");
}
