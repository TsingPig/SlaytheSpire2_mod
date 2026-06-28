using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：火势蔓延（Fire Ninjutsu: Fire Spread）——技能牌（普通）。
/// 1（升级 0）费，将目标身上的【燃烧】层数扩散给所有其他敌人。
/// </summary>
public class FireSpread : NinjaModCard
{
    public FireSpread() : base(BalanceCost(nameof(FireSpread), 1), BalanceType(nameof(FireSpread), CardType.Skill), BalanceRarity(nameof(FireSpread), CardRarity.Common), BalanceTarget(nameof(FireSpread), TargetType.AnyEnemy)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int burning = cardPlay.Target.GetPower<BurningPower>()?.Amount ?? 0;
        if (burning <= 0) return;

        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            if (enemy == cardPlay.Target) continue; // 目标已拥有燃烧，扩散给其余敌人
            await PowerCmd.Apply<BurningPower>(choiceContext, enemy, burning, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(FireSpread), -1)); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火势蔓延", "将目标身上的[gold]燃烧[/gold]扩散给所有其他敌人。")
        : new CardLoc("Fire Ninjutsu: Fire Spread", "Spread the target's [gold]Burning[/gold] to all other enemies.");
}
