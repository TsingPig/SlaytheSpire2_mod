using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Monsters;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 怯懦（Cowardice）——黑骑士【暗鬼铠甲】对玩家施加的可叠层 Debuff。
///
/// 规则（严格按需求实现）：
///  • 玩家拥有 N 层怯懦时，每当一张【幽冥诅咒】<see cref="NetherCurse"/> 进入玩家手牌：
///      - 仅让这一个卡牌实例获得【消耗】（写入实例 <c>LocalKeywords</c>，不修改卡牌原型 CanonicalKeywords）。
///      - 怯懦层数减少 1。
///  • 每张进入手牌的幽冥诅咒最多消耗 1 层。
///  • 普通卡牌进入手牌不消耗层数。
///  • 因手牌已满而进入弃牌堆的幽冥诅咒不会触发（我们检查卡牌当前所在牌堆是否为手牌）。
///  • 层数降到 0 时移除。
///
/// 使用 <see cref="AbstractModel.AfterCardChangedPiles"/> 覆盖所有“进入手牌”的正式事件
/// （正常抽牌、从抽牌堆直接取出、搜索后加入手牌等）。
/// </summary>
public class CowardicePower : NinjaModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardChangedPiles(CardModel card, PileType pile, AbstractModel source)
    {
        if (Amount <= 0) return;
        if (card is not NetherCurse) return;

        // 只处理属于本 Debuff 所有者（玩家）的卡。
        if (card.Owner == null || Owner.Player == null || card.Owner != Owner.Player) return;

        // 关键：以卡牌“当前所在牌堆”为准判断是否真的进入了手牌，
        // 避免因手牌已满而落入弃牌堆时误消耗层数。
        if (card.Pile == null || card.Pile.Type != PileType.Hand) return;

        // 幂等：若该实例已获得消耗（例如同一事件重复触发），不再重复处理。
        if (card.Keywords.Contains(CardKeyword.Exhaust)) return;

        Flash();
        // 仅让这一个实例获得消耗。AddKeyword 写入实例级 LocalKeywords，触发 KeywordsChanged 刷新 UI。
        card.AddKeyword(CardKeyword.Exhaust);
        // 每张幽冥诅咒最多消耗 1 层怯懦。
        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("怯懦",
            "接下来抽到的 {Amount} 张【幽冥诅咒】进入手牌时会获得【消耗】（每张消耗 1 层）。",
            "接下来抽到的 {Amount} 张【幽冥诅咒】进入手牌时会获得【消耗】（每张消耗 1 层）。")
        : new PowerLoc("Cowardice",
            "The next {Amount} Nether Curse(s) that enter your hand gain Exhaust (1 stack each).",
            "The next {Amount} Nether Curse(s) that enter your hand gain Exhaust (1 stack each).");
}
