using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using NinjaMod.NinjaModCode.Cards;
using NinjaMod.NinjaModCode.Monsters;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 黑暗骑士进入真身后施加给自己的永久 Buff。
/// 一个 Buff 统一监听所有玩家抽牌，但通过玩家在 CombatState.Players 中的稳定索引，
/// 分别记录每名玩家本回合是否已经抽到第一张幽冥诅咒。
/// </summary>
public class NetherErosionPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// 每一位玩家占一个 bit；属性随战斗存档保存，避免读档后重复触发。
    /// </summary>
    [SavedProperty]
    public int TriggeredPlayerMask { get; set; }

    public override Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        // 只清除本次开始回合的玩家位，其他玩家的独立记录保持不变。
        for (int index = 0; index < combatState.Players.Count && index < 31; index++)
        {
            if (participants.Contains(combatState.Players[index].Creature))
                TriggeredPlayerMask &= ~(1 << index);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        ICombatState? combatState = Owner.CombatState;
        Player? player = card.Owner;
        if (combatState == null || player == null)
            return Task.CompletedTask;

        int playerIndex = FindPlayerIndex(combatState, player);
        if (playerIndex < 0 || playerIndex >= 31)
            return Task.CompletedTask;

        bool triggeredThisTurn = (TriggeredPlayerMask & (1 << playerIndex)) != 0;
        if (!BlackKnightRules.ShouldRemoveNetherCurseExhaust(
                triggeredThisTurn,
                card is NetherCurse,
                oldPileType == PileType.Draw,
                card.Pile?.Type == PileType.Hand))
        {
            return Task.CompletedTask;
        }

        // 第一张同名牌即占用该玩家本回合的触发次数；即使该实例此前已经
        // 失去消耗，也不会让本回合第二张幽冥诅咒再次触发。
        TriggeredPlayerMask |= 1 << playerIndex;
        Flash();
        if (card.Keywords.Contains(CardKeyword.Exhaust))
            card.RemoveKeyword(CardKeyword.Exhaust);

        return Task.CompletedTask;
    }

    private static int FindPlayerIndex(ICombatState combatState, Player player)
    {
        for (int index = 0; index < combatState.Players.Count; index++)
        {
            if (combatState.Players[index] == player)
                return index;
        }

        return -1;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc(
            "幽冥侵蚀",
            "每名玩家每回合第一次抽到的[gold]幽冥诅咒[/gold]失去[gold]消耗[/gold]。",
            "每名玩家每回合第一次抽到的[gold]幽冥诅咒[/gold]失去[gold]消耗[/gold]。")
        : new PowerLoc(
            "Nether Erosion",
            "The first [gold]Nether Curse[/gold] each player draws each turn loses [gold]Exhaust[/gold].",
            "The first [gold]Nether Curse[/gold] each player draws each turn loses [gold]Exhaust[/gold].");
}
