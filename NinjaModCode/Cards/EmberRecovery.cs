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
/// 火忍：余烬回收（Fire Ninjutsu: Ember Recovery）——技能牌（罕见）。
/// 1（升级 0）费，点燃目标身上的【燃烧】；若成功点燃，获得 1 点能量并抽 1 张牌。
/// </summary>
public class EmberRecovery : NinjaModCard
{
    public EmberRecovery() : base(BalanceCost(nameof(EmberRecovery), 1), BalanceType(nameof(EmberRecovery), CardType.Skill), BalanceRarity(nameof(EmberRecovery), CardRarity.Uncommon), BalanceTarget(nameof(EmberRecovery), TargetType.AnyEnemy)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var burning = cardPlay.Target.GetPower<BurningPower>();
        if (burning == null || burning.Amount <= 0) return;

        await burning.IgniteAsync(choiceContext, Owner.Creature);
        // 成功点燃：获得 1 点能量并抽 1 张牌。
        await PlayerCmd.GainEnergy(BalanceValue("BaseEnergy", 1), Owner);
        await CardPileCmd.Draw(choiceContext, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(EmberRecovery), -1)); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：余烬回收", "点燃目标身上的[gold]燃烧[/gold]（造成燃烧 2 倍的无法格挡伤害并移除）。如果成功点燃，获得 1 点能量并抽 1 张牌。")
        : new CardLoc("Fire Ninjutsu: Ember Recovery", "Ignite the target's [gold]Burning[/gold] (deal twice Burning as unblockable damage, then remove). If it ignited, gain 1 Energy and draw 1 card.");
}
