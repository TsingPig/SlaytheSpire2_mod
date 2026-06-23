using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Shadow Clone (影分身) Power.
///
/// 活跃期间产生三种效果，模拟克隆体并排战斗：
///   1. 【卡牌结算】每张非影分身卡牌额外结算一次（克隆体同步出招）。
///   2. 【伤害分担】主人受到的攻击伤害减少 40%（克隆体分摊受击，
///      使用 ModifyDamageAdditive 在格挡前作用）。
///   3. 【荆棘继承】主人受到攻击伤害后，若其当前拥有荆棘（Thorns）层数，
///      克隆体以相同荆棘值对攻击者造成无法格挡伤害（模拟克隆体被攻击时触发荆棘）。
///
/// 层数用作回合计数器，每回合结束时递减；为 0 时自动移除。
/// 以 Amount=2 施加时持续本回合与下回合。
/// </summary>
public class ShadowClonePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ── 1. 卡牌结算：每张非影分身卡额外结算一次 ──────────────────────────────
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (Amount <= 0) return playCount;
        if (card.Owner?.Creature != Owner) return playCount;
        if (card is ShadowClone) return playCount; // 影分身本身不递归复制
        return playCount + 1;
    }

    // ── 2. 伤害分担：攻击伤害减少 40%（克隆体承担部分受击）─────────────────────
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return 0m;
        if (dealer == Owner) return 0m;                    // 不减少自身造成的伤害
        if (!props.IsCardOrMonsterMove()) return 0m;        // 只减少攻击伤害，不影响流血/燃烧
        if (Amount <= 0) return 0m;
        // 减少 40%（向下取整）
        return -Math.Floor(amount * 0.4m);
    }

    // ── 3. 荆棘继承：受攻击后克隆体用玩家荆棘值反击 ────────────────────────────
    private bool _retaliating;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (_retaliating) return;
        if (target != Owner) return;
        if (Amount <= 0) return;
        if (!props.IsCardOrMonsterMove()) return;
        if (dealer == null || dealer == Owner) return;
        if (result.UnblockedDamage <= 0 && result.BlockedDamage <= 0) return; // 完全无效化时不触发

        // 查找玩家身上的荆棘层数（兼容基础游戏 Thorns power 的 Entry 名称）
        var thorns = Owner.Powers
            .FirstOrDefault(p => p.Id.Entry.Contains("Thorns") || p.Id.Entry.Contains("thorns"));
        if (thorns == null || thorns.Amount <= 0) return;

        _retaliating = true;
        try
        {
            Flash(); // 让图标闪烁提示克隆体触发
            await CreatureCmd.Damage(choiceContext, dealer, thorns.Amount,
                ValueProp.Unblockable | ValueProp.Unpowered, Owner, null);
        }
        finally
        {
            _retaliating = false;
        }
    }

    // ── 回合计数器：每回合结束递减 ────────────────────────────────────────────
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("影分身",
            "本回合及下回合：①打出的每张非影分身卡牌额外结算一次；②你受到的攻击伤害减少 40%；③你每次受到攻击后，若你有荆棘，克隆体对攻击者造成等量荆棘伤害。",
            "本回合及下回合：①打出的每张非影分身卡牌额外结算一次；②你受到的攻击伤害减少 40%；③你每次受到攻击后，若你有荆棘，克隆体对攻击者造成等量荆棘伤害。")
        : new PowerLoc("Shadow Clone",
            "This turn and next: ① Each non-Shadow-Clone card you play resolves once more. ② Incoming attack damage is reduced by 40%. ③ After each attack hit you receive, if you have Thorns, the clone deals that amount as unblockable damage to the attacker.",
            "This turn and next: ① Each non-Shadow-Clone card resolves once more. ② Incoming attack damage reduced by 40%. ③ After each hit, if you have Thorns, the clone retaliates.");
}
