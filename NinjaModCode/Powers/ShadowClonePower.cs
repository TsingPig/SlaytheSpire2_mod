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

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Shadow Clone (影分身), implemented as a player power (no separate ally entity).
/// While active, every non-Shadow-Clone card the owner plays resolves one extra time, using
/// the base game's <see cref="ModifyCardPlayCount"/> hook (the same mechanism as Duplication).
///
/// - Shadow Clone cards are excluded, so a copied Shadow Clone does nothing (no recursion).
/// - The play-count hook is queried once per play, so extra resolutions cannot trigger more
///   copies (no recursion).
/// - The amount is a turn counter; it ticks down at the end of each of the owner's turns and is
///   removed at 0. Applied with amount 2 it lasts for the current turn and the next turn.
///
/// Known limitation: there is no visible clone body and the clone does not take damage
/// (no ally/summon API is used). See README.
/// </summary>
public class ShadowClonePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (Amount <= 0) return playCount;
        if (card.Owner?.Creature != Owner) return playCount;
        if (card is ShadowClone) return playCount; // never copy Shadow Clone itself
        return playCount + 1;
    }

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
            "本回合及下回合，你打出的每张非影分身卡牌都会额外结算一次。",
            "本回合及下回合，你打出的每张非影分身卡牌都会额外结算一次。")
        : new PowerLoc("Shadow Clone",
            "This turn and next turn, each non-Shadow-Clone card you play resolves one extra time.",
            "This turn and next turn, each non-Shadow-Clone card you play resolves one extra time.");
}
