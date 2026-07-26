using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 黑骑士的固有规则说明词条。
///
/// 实际的逐玩家回合结束检查仍由 BlackKnightEnemy 执行；该 Power 常驻于黑骑士身上，
/// 让玩家随时可以从状态栏确认【幽冥诅咒】与【竖劈锁定】之间的关系。
/// </summary>
public sealed class NetherExecutionPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc(
            "幽冥处刑",
            "[gold]固有[/gold]。每名玩家的回合结束时，若其手牌中有[gold]幽冥诅咒[/gold]，"
            + "该玩家获得[gold]竖劈锁定[/gold]：下一次[gold]竖劈[/gold]对其造成的伤害无法被格挡。",
            "[gold]固有[/gold]。每名玩家的回合结束时，若其手牌中有[gold]幽冥诅咒[/gold]，"
            + "该玩家获得[gold]竖劈锁定[/gold]：下一次[gold]竖劈[/gold]对其造成的伤害无法被格挡。")
        : new PowerLoc(
            "Nether Execution",
            "[gold]Innate[/gold]. At the end of each player's turn, if they have a "
            + "[gold]Nether Curse[/gold] in hand, they gain [gold]Vertical Slash Lock[/gold]: "
            + "the next [gold]Vertical Slash[/gold] against them is unblockable.",
            "[gold]Innate[/gold]. At the end of each player's turn, if they have a "
            + "[gold]Nether Curse[/gold] in hand, they gain [gold]Vertical Slash Lock[/gold]: "
            + "the next [gold]Vertical Slash[/gold] against them is unblockable.");
}
