using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 火盾（Flame Shield）Power——能力牌“火忍：火盾”施加。
/// 每当本体受到攻击伤害时，对攻击者施加 3 层燃烧。
/// </summary>
public class FlameShieldPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 每次受击对攻击者施加的燃烧层数（常量）。
    private const int BurningOnHit = 3;

    private bool _reacting;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_reacting) return;
        if (target != Owner) return;
        if (dealer == null || dealer == Owner) return;
        if (!props.IsCardOrMonsterMove()) return; // 只对攻击反应

        _reacting = true;
        try
        {
            Flash();
            await PowerCmd.Apply<BurningPower>(choiceContext, dealer, BurningOnHit, Owner, null);
        }
        finally
        {
            _reacting = false;
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("火盾",
            $"每当你受到攻击时，对攻击者施加 {BurningOnHit} 层燃烧。",
            $"每当你受到攻击时，对攻击者施加 {BurningOnHit} 层燃烧。")
        : new PowerLoc("Flame Shield",
            $"Whenever you are attacked, apply {BurningOnHit} Burning to the attacker.",
            $"Whenever you are attacked, apply {BurningOnHit} Burning to the attacker.");
}
