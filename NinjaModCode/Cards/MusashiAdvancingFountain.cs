using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：前进喷泉（Musashi: Advancing Fountain）——技能牌，消耗。
/// 1 费，回复 7 点生命，下个回合额外获得 1（升级 2）点能量。
/// </summary>
public class MusashiAdvancingFountain : NinjaModCard
{
    public MusashiAdvancingFountain() : base(BalanceCost(nameof(MusashiAdvancingFountain), 1), BalanceType(nameof(MusashiAdvancingFountain), CardType.Skill), BalanceRarity(nameof(MusashiAdvancingFountain), CardRarity.Uncommon), BalanceTarget(nameof(MusashiAdvancingFountain), TargetType.Self)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiAdvancingFountain), true);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(BalanceDecimal("BaseHeal", 4m)), new IntVar("Energy", BalanceDecimal("BaseEnergy", 1m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue, true);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature,
            DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Energy"].UpgradeValueBy(BalanceDelta("BaseEnergy", "UpgradeEnergy", 1m)); // 1 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：前进喷泉", "回复 {Heal} 点生命，下个回合额外获得 {Energy:diff()} 点能量。")
        : new CardLoc("Musashi: Advancing Fountain", "Heal {Heal} HP. Gain {Energy:diff()} Energy next turn.");
}
