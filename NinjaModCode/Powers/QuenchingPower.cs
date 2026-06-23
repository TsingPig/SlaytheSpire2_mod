using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 淬火（Quenching）Power——“火忍：淬火术”施加的本回合增益。
/// 本回合内，本体用攻击牌每次造成伤害时，额外给被命中目标附加 6 层燃烧。
/// 借鉴基础游戏 EnvenomPower（攻击附毒）的 <see cref="AfterDamageGiven"/> 钩子。
/// 回合结束自动移除。
/// </summary>
public class QuenchingPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    // 不叠加：重复施加只刷新，不累积燃烧值。
    public override PowerStackType StackType => PowerStackType.Single;

    // 每次攻击附加的燃烧层数（常量，不随升级变化）。
    private const int BurningPerHit = 6;

    // 防止自身递归触发。
    private bool _applying;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_applying) return;
        if (dealer != Owner) return;                 // 只在本体造成伤害时
        if (target == Owner) return;
        if (cardSource == null) return;               // 只在“攻击牌”造成伤害时
        if (!props.IsCardOrMonsterMove()) return;     // 只在攻击伤害时

        _applying = true;
        try
        {
            Flash();
            await PowerCmd.Apply<BurningPower>(choiceContext, target, BurningPerHit, Owner, cardSource);
        }
        finally
        {
            _applying = false;
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 本回合限定：本体回合结束时移除。
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("淬火",
            $"本回合，你用攻击牌每次造成伤害都会额外附加 {BurningPerHit} 层燃烧。",
            $"本回合，你用攻击牌每次造成伤害都会额外附加 {BurningPerHit} 层燃烧。")
        : new PowerLoc("Quenching",
            $"This turn, your attack cards apply {BurningPerHit} Burning on each hit.",
            $"This turn, your attack cards apply {BurningPerHit} Burning on each hit.");
}
