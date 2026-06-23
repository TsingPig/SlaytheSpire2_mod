using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Bleed (流血): whenever the owner takes unblocked attack damage, it immediately takes
/// additional unblockable HP loss equal to the current Bleed amount.
///
/// Trigger rule: only damage flagged as a card/monster move (an attack) triggers Bleed.
/// The bonus damage is dealt as Unblockable|Unpowered (NOT a move), so it cannot trigger
/// itself (no recursion) and end-of-turn effects like Burning (also non-move) do not trigger
/// Bleed. Assassination is an attack (move) that ignores block, so it DOES trigger existing
/// Bleed, as required.
/// </summary>
public class BleedPower : NinjaModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Re-entrancy guard (belt-and-suspenders; the move-flag check already prevents recursion).
    private bool _triggering;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_triggering) return;
        if (target != Owner) return;
        if (Amount <= 0) return;
        if (!props.IsCardOrMonsterMove()) return; // only attack damage triggers Bleed
        if (result.UnblockedDamage <= 0) return;  // only damage that actually reached HP

        _triggering = true;
        try
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, Owner, Amount,
                ValueProp.Unblockable | ValueProp.Unpowered, dealer, null);
        }
        finally
        {
            _triggering = false;
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("流血",
            "每当此生物受到未被格挡的攻击伤害时，立即额外失去等同于流血层数的生命（无法被格挡）。",
            "每当此生物受到未被格挡的攻击伤害时，立即额外失去等同于流血层数的生命（无法被格挡）。")
        : new PowerLoc("Bleed",
            "Whenever this creature takes unblocked attack damage, it loses additional HP equal to its Bleed.",
            "Whenever this creature takes unblocked attack damage, it loses additional HP equal to its Bleed.");
}
