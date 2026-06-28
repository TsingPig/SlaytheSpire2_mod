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
/// 轻盈舞步（Nimble Step）——能力牌（稀有）。
/// 2（升级 1）费，获得【轻盈舞步】：每打出一张 0 费牌，抽 1 张牌。
/// </summary>
public class NimbleStep : NinjaModCard
{
    public NimbleStep() : base(BalanceCost(nameof(NimbleStep), 2), BalanceType(nameof(NimbleStep), CardType.Power), BalanceRarity(nameof(NimbleStep), CardRarity.Rare), BalanceTarget(nameof(NimbleStep), TargetType.Self)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NimbleStepPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(NimbleStep), -1)); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("轻盈舞步", "每当你打出一张 0 费牌，抽 1 张牌。")
        : new CardLoc("Nimble Step", "Whenever you play a 0-cost card, draw 1 card.");
}
