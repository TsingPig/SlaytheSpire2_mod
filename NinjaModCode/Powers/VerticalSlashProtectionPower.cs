using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 竖劈保护（Vertical Slash Protection）——由玩家打出【幽冥诅咒】<see cref="NetherCurse"/>
/// 在黑骑士下一意图为竖劈+ 时，施加到该黑骑士身上的一次性标记 Buff。
///
/// 效果绑定与消耗逻辑由黑骑士的竖劈行动读取本 Power 实现：
///  • 存在本 Power 时，本次竖劈从“不可格挡”转为“普通可格挡”，且黑骑士恢复等同于本次实际生命伤害的生命。
///  • 竖劈结算后立即移除本 Power（消耗），因此只作用于这一次竖劈。
///
/// 采用 <see cref="PowerStackType.Single"/>：连续打出多张幽冥诅咒不会叠加保护
/// （“保护标记不能叠加”）。作为附着在具体黑骑士 Creature 上的 Power，
/// 天然满足“绑定到具有竖劈意图的明确目标、不依赖全局单例、随存档保存/恢复”。
/// </summary>
public class VerticalSlashProtectionPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("竖劈保护",
            "下一次竖劈+ 变为可以被格挡；黑骑士将恢复等同于该次实际造成生命伤害的生命。结算后消失。",
            "下一次竖劈+ 变为可以被格挡；黑骑士将恢复等同于该次实际造成生命伤害的生命。结算后消失。")
        : new PowerLoc("Warded Cleave",
            "The next Vertical Slash+ becomes blockable; the Black Knight heals equal to the HP damage it deals. Consumed after it resolves.",
            "The next Vertical Slash+ becomes blockable; the Black Knight heals equal to the HP damage it deals. Consumed after it resolves.");
}
