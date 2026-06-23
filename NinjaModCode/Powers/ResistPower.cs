using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Resist (抵挡): each incoming enemy attack hit is reduced by the Resist amount before Block
/// is applied. Implemented via <see cref="ModifyDamageAdditive"/>, the same pre-block hook the
/// base game uses for Strength. Only "move" damage (attacks) is reduced; unblockable HP loss
/// such as Bleed/Burning is not affected.
/// </summary>
public class ResistPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return 0m;        // only reduce damage aimed at the owner
        if (dealer == Owner) return 0m;         // don't reduce the owner's own damage
        if (!props.IsCardOrMonsterMove()) return 0m; // only attack hits, not unblockable HP loss
        return -Amount;                          // flat per-hit reduction before block
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("抵挡",
            "每一次受到的攻击伤害在进入格挡计算之前减少等同于抵挡层数的数值。",
            "每一次受到的攻击伤害在进入格挡计算之前减少等同于抵挡层数的数值。")
        : new PowerLoc("Resist",
            "Each incoming attack hit is reduced by this amount before Block.",
            "Each incoming attack hit is reduced by this amount before Block.");
}
