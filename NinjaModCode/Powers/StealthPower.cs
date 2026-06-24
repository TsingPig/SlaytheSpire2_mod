using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 隐身（Stealth）Power。
/// 敌人的攻击目标会排除隐身玩家；当所有玩家都处于隐身时，攻击意图显示为眩晕并跳过攻击。
/// 当本体主动打出攻击牌后，立即失去隐身并刷新敌人的原始意图。
/// <see cref="ModifyDamageAdditive"/> 仅作为不经过标准攻击指令的怪物伤害的安全兜底。
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

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (cardPlay.Card is NinjaModCard { PreservesStealth: true }) return;

        await PowerCmd.Remove(this);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        return RefreshEnemyIntents(Owner.CombatState);
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        return RefreshEnemyIntents(oldOwner.CombatState);
    }

    private static async Task RefreshEnemyIntents(ICombatState? combatState)
    {
        if (combatState == null || !combatState.IsLiveCombat() || NCombatRoom.Instance == null) return;

        foreach (var enemy in combatState.Enemies.Where(enemy => enemy.IsAlive && enemy.Monster != null))
        {
            var node = NCombatRoom.Instance.GetCreatureNode(enemy);
            if (node != null)
            {
                await node.RefreshIntents();
            }
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
