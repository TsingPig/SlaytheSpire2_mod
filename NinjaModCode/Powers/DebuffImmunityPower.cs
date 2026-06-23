using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Debuff Immunity (免疫负面): prevents debuffs applied to the owner, using the same hook the
/// base game's Artifact uses. Unlike Artifact it is not consumed per debuff; instead it lasts
/// until the end of the owner's next turn. The amount is used as a turn counter and ticks down
/// at the end of each of the owner's turns.
/// </summary>
public class DebuffImmunityPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target,
        decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (target != Owner)
        {
            modifiedAmount = amount;
            return false;
        }

        if (canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
        {
            modifiedAmount = amount;
            return false;
        }

        if (!canonicalPower.IsVisible)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = 0m; // cancel the debuff
        return true;
    }

    public override Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // Tick down at the end of each of the owner's turns; removed automatically at 0.
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("免疫负面",
            "在下个回合结束前阻止施加于自身的负面效果。",
            "在下个回合结束前阻止施加于自身的负面效果。")
        : new PowerLoc("Debuff Immunity",
            "Prevents negative effects until the end of next turn.",
            "Prevents negative effects until the end of next turn.");
}
