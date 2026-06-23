using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Relics;

/// <summary>
/// Hidden Blade (藏刃) - the Ninja's starting relic. It also carries the Ninja core passive:
/// at the start of each of your turns, add a temporary Kunai to your hand.
///
/// The passive lives on the relic (rather than a separate invisible power) because relics are
/// reliably part of the combat hook loop; this keeps the implementation simple and robust.
/// </summary>
public class HiddenBlade : NinjaModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var kunai = ModelDb.Card<Kunai>().ToMutable();
        await CardPileCmd.AddGeneratedCardToCombat(kunai, PileType.Hand, player);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new RelicLoc("藏刃",
            "在你的每个回合开始时，将一张飞刀加入你的手牌。",
            "袖中藏刃，伺机而动。")
        : new RelicLoc("Hidden Blade",
            "At the start of each of your turns, add a Kunai to your hand.",
            "A blade hidden in the sleeve, ready to strike.");
}
