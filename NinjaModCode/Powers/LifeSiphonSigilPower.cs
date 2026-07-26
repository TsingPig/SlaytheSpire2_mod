using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 黑暗骑士将成功发放的诅咒转化为攻击吸血次数。
/// 实际治疗和逐次消耗在攻击动作完成后统一结算。
/// </summary>
public class LifeSiphonSigilPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc(
            "噬命诅印",
            "每次攻击后，失去 1 层，并回复该次攻击造成的生命伤害。",
            "每次攻击后，失去 1 层，并回复该次攻击造成的生命伤害。")
        : new PowerLoc(
            "Life-Siphon Sigil",
            "After each attack, lose 1 stack and heal HP equal to the HP damage dealt.",
            "After each attack, lose 1 stack and heal HP equal to the HP damage dealt.");
}
