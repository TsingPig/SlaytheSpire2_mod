using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Relics;

/// <summary>
/// Hidden Blade (藏刃) - the Ninja's starting relic. It also carries the Ninja core passive:
/// at the start of each of your turns, add a temporary Kunai to your hand.
///
/// Implemented on the relic via <see cref="BeforeHandDraw"/> - the exact hook and pattern the base
/// game uses for Infinite Blades / Shiv. Relics are reliably part of the combat hook loop.
/// </summary>
public class HiddenBlade : NinjaModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner) return; // only react for the relic's owning player
        Flash();
        await Kunai.CreateInHand(player, combatState);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new RelicLoc("藏刃",
            "在你的每个回合开始时，将一张飞刀加入你的手牌。",
            "袖中藏刃，伺机而动。")
        : new RelicLoc("Hidden Blade",
            "At the start of each of your turns, add a Kunai to your hand.",
            "A blade hidden in the sleeve, ready to strike.");
}
