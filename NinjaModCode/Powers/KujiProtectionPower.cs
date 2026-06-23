using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 九字护身（Kuji Protection）Power——能力牌“九字护身法”施加。
/// 每当本体回合开始时，获得等同于（当前抵挡层数 × 2）的格挡。
/// </summary>
public class KujiProtectionPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        var resist = Owner.GetPower<ResistPower>();
        int block = (resist?.Amount ?? 0) * 2;
        if (block <= 0) return;

        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Move, null);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("九字护身",
            "每回合开始时，获得当前抵挡层数 2 倍的格挡。",
            "每回合开始时，获得当前抵挡层数 2 倍的格挡。")
        : new PowerLoc("Kuji Protection",
            "At the start of each turn, gain Block equal to twice your current Resist.",
            "At the start of each turn, gain Block equal to twice your current Resist.");
}
