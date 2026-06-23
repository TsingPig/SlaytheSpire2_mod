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
/// 隐身（Stealth）Power。
/// 实现“敌人无法攻击你”：当本体受到敌人攻击伤害时，通过 <see cref="ModifyDamageAdditive"/>
/// 将该次攻击伤害完全抵消（等价于敌人攻击落空）。
/// 当本体攻击敌人时（造成攻击伤害后），立即失去隐身。
/// 注：采用伤害抵消的等价实现，避免直接改写敌人意图系统的复杂 patch。
/// </summary>
public class StealthPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ── 敌人攻击伤害归零（等价“无法攻击你”）──
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return 0m;
        if (dealer == Owner) return 0m;                 // 不影响自身造成的伤害
        if (!props.IsCardOrMonsterMove()) return 0m;     // 只抵消攻击，不影响流血/燃烧等
        return -amount;                                   // 抵消全部攻击伤害
    }

    // ── 攻击敌人后失去隐身 ──
    private bool _removing;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_removing) return;
        if (dealer != Owner) return;
        if (target == Owner) return;
        if (!props.IsCardOrMonsterMove()) return; // 只有主动攻击才会脱离隐身

        _removing = true;
        try
        {
            await PowerCmd.Remove(this);
        }
        finally
        {
            _removing = false;
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("隐身",
            "敌人无法攻击你。当你攻击敌人时，失去隐身。",
            "敌人无法攻击你。当你攻击敌人时，失去隐身。")
        : new PowerLoc("Stealth",
            "Enemies cannot attack you. When you attack an enemy, lose Stealth.",
            "Enemies cannot attack you. When you attack an enemy, lose Stealth.");
}
