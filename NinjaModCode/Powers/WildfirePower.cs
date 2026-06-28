using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 燎原（Wildfire）能力——“火忍：燎原”施加。
/// 你的回合开始时，给予所有敌人等同于层数（2/升级 3）的【燃烧】。
/// </summary>
public class WildfirePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        if (Amount <= 0) return;
        if (CombatManager.Instance == null || CombatManager.Instance.IsOverOrEnding) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        Flash();
        foreach (var enemy in combatState.Enemies.Where(e => e.IsAlive).ToList())
        {
            await PowerCmd.Apply<BurningPower>(choiceContext, enemy, Amount, Owner, null);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("燎原",
            "你的回合开始时，给予所有敌人等同于层数的[gold]燃烧[/gold]。",
            "你的回合开始时，给予所有敌人等同于层数的[gold]燃烧[/gold]。")
        : new PowerLoc("Wildfire",
            "At the start of your turn, apply Burning equal to its amount to ALL enemies.",
            "At the start of your turn, apply Burning equal to its amount to ALL enemies.");
}
