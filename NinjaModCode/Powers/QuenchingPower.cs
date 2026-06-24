using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 淬火（Quenching）Power——“火忍：淬火术”施加的本回合增益。
/// 本回合内，本体用攻击牌每次造成伤害时，每层淬火额外给被命中目标附加 6 层燃烧。
/// 借鉴基础游戏 EnvenomPower（攻击附毒）的 <see cref="AfterDamageGiven"/> 钩子。
/// 回合结束自动移除。
/// </summary>
public class QuenchingPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 每次攻击附加的燃烧层数（常量，不随升级变化）。
    private const int BurningPerHit = 6;

    // 防止自身递归触发。
    private bool _applying;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_applying) return;
        if (dealer != Owner) return;                 // 只在本体造成伤害时
        if (target == Owner) return;
        if (cardSource?.Type != CardType.Attack) return;
        if (!props.IsPoweredAttack()) return;

        _applying = true;
        try
        {
            Flash();
            await PowerCmd.Apply<BurningPower>(choiceContext, target, BurningPerHit * Amount, Owner, cardSource);
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
            $"本回合，每层[gold]淬火[/gold]使攻击牌每次造成伤害额外附加 {BurningPerHit} 层燃烧。",
            $"本回合，每层[gold]淬火[/gold]使攻击牌每次造成伤害额外附加 {BurningPerHit} 层燃烧。")
        : new PowerLoc("Quenching",
            $"This turn, each [gold]Quenching[/gold] stack makes your attack cards apply {BurningPerHit} Burning on each hit.",
            $"This turn, each [gold]Quenching[/gold] stack makes your attack cards apply {BurningPerHit} Burning on each hit.");
}
