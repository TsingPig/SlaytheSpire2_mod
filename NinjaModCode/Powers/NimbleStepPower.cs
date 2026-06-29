using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 轻盈舞步（Nimble Step）能力——“轻盈舞步”施加。
/// 每当你打出一张 0 费牌，抽 1 张牌。
/// </summary>
public class NimbleStepPower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsPlayer) return;
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        if (cardPlay.Card.EnergyCost.GetResolved() != 0) return; // 仅 0 费牌触发
        if (CombatManager.Instance == null || CombatManager.Instance.IsOverOrEnding) return;
        var player = Owner.Player;
        if (player == null) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, player);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("轻盈舞步",
            "每当你打出一张 0 费牌，抽 1 张牌。",
            "每当你打出一张 0 费牌，抽 1 张牌。")
        : new PowerLoc("Nimble Step",
            "Whenever you play a 0-cost card, draw 1 card.",
            "Whenever you play a 0-cost card, draw 1 card.");
}
