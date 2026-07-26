using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 玩家专属的竖劈判定标记。每次玩家回合结束时重新计算，
/// 并在黑暗骑士下一次对该玩家结算竖劈后移除。
/// </summary>
public class VerticalSlashExposurePower : NinjaModPower
{
    // 这是竖劈规则的可见标记，不应被“免疫负面”等效果拦截。
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc(
            "竖劈锁定",
            "下一次[gold]竖劈[/gold]对你造成的伤害无法被格挡。",
            "下一次[gold]竖劈[/gold]对你造成的伤害无法被格挡。")
        : new PowerLoc(
            "Vertical Slash Lock",
            "The next [gold]Vertical Slash[/gold] against you is unblockable.",
            "The next [gold]Vertical Slash[/gold] against you is unblockable.");
}
