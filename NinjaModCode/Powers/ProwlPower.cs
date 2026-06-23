using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 潜行（Prowl）Power——“潜行”卡施加。
/// 在本体的下个回合开始时，向手牌生成一张暗杀（<see cref="Assassination"/>），然后移除自身。
/// 借鉴藏刃遗物生成飞刀的方式（CreateCard + AddGeneratedCardToCombat）。
/// </summary>
public class ProwlPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;

        Flash();
        // 生成一张暗杀放入手牌。
        var card = combatState.CreateCard<Assassination>(Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player);
        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("潜行",
            "下个回合开始时，将一张暗杀加入你的手牌。",
            "下个回合开始时，将一张暗杀加入你的手牌。")
        : new PowerLoc("Prowl",
            "At the start of your next turn, add an Assassination to your hand.",
            "At the start of your next turn, add an Assassination to your hand.");
}
