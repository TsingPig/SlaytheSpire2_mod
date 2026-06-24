using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 圆明（Enmei）能力——“武藏：圆明流”施加。
/// 每当你打出一张“武藏”系列卡牌（<see cref="NinjaModCard.IsMusashi"/>），回复 1 点生命。
/// </summary>
public class EnmeiPower : NinjaModPower
{
    // 每次触发回复的生命值。
    private const int HealPerMusashi = 1;

    public override PowerType Type => PowerType.Buff;

    // 状态型能力，不叠加层数（拥有即生效）。
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsPlayer) return;
        if (cardPlay.Card is NinjaModCard card && card.IsMusashi && card.Owner?.Creature == Owner)
        {
            Flash();
            await CreatureCmd.Heal(Owner, HealPerMusashi, true);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("圆明",
            "每当你打出一张“武藏”牌，回复 1 点生命。",
            "每当你打出一张“武藏”牌，回复 1 点生命。")
        : new PowerLoc("Enmei",
            "Whenever you play a Musashi card, heal 1 HP.",
            "Whenever you play a Musashi card, heal 1 HP.");
}
