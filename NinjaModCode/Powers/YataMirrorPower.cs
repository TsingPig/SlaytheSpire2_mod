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
/// 八咫镜（Yata Mirror）Power——能力牌“八咫镜”施加的持续增益。
/// 每当本体回合开始时，获得等同于层数（Amount）的格挡。
/// </summary>
public class YataMirrorPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        Flash();
        // 能力触发的格挡：无来源卡牌，CardPlay 传 null。
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("八咫镜",
            "每回合开始时，获得等同于此层数的格挡。",
            "每回合开始时，获得等同于此层数的格挡。")
        : new PowerLoc("Yata Mirror",
            "At the start of each turn, gain Block equal to this amount.",
            "At the start of each turn, gain Block equal to this amount.");
}
