using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：圆明流（Musashi: Enmei Style）——能力牌。
/// 1 费，获得 1 层【圆明】（<see cref="EnmeiPower"/>）：每打出一张“武藏”牌回复等同于圆明层数的生命。
/// </summary>
public class MusashiEnmeiStyle : NinjaModCard
{
    public MusashiEnmeiStyle() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override bool IsMusashi => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnmeiPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：圆明流", "获得 1 层[gold]圆明[/gold]：每当你打出一张“武藏”牌，回复等同于圆明层数的生命。")
        : new CardLoc("Musashi: Enmei Style", "Gain 1 [gold]Enmei[/gold]: whenever you play a Musashi card, heal HP equal to your Enmei stacks.");
}
