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
/// 残影术（Afterimage Art）——能力牌。
/// 2（升级 1）费，获得 1 层【残影】（<see cref="AfterimagePower"/>）。
/// </summary>
public class AfterimageArt : NinjaModCard
{
    public AfterimageArt() : base(BalanceCost(nameof(AfterimageArt), 2), BalanceType(nameof(AfterimageArt), CardType.Power), BalanceRarity(nameof(AfterimageArt), CardRarity.Rare), BalanceTarget(nameof(AfterimageArt), TargetType.Self)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AfterimagePower>(choiceContext, Owner.Creature, BalanceValue("BaseAfterimage", 1), Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(AfterimageArt), -1)); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("残影术", "获得 1 层[gold]残影[/gold]。")
        : new CardLoc("Afterimage Art", "Gain 1 [gold]Afterimage[/gold].");
}
