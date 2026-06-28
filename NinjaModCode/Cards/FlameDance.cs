using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：火焰之舞（Fire Ninjutsu: Flame Dance）——能力牌（稀有）。
/// 1 费，获得【火焰之舞】：每回合第一次打出【火忍】牌时，获得 1（升级 2）点能量。
/// </summary>
public class FlameDance : NinjaModCard
{
    public FlameDance() : base(BalanceCost(nameof(FlameDance), 1), BalanceType(nameof(FlameDance), CardType.Power), BalanceRarity(nameof(FlameDance), CardRarity.Rare), BalanceTarget(nameof(FlameDance), TargetType.Self)) { }

    // 获得的能量：1（升级 2）。用 IntVar 显示升级预览。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Energy", BalanceValue("BaseEnergy", 1))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FlameDancePower>(choiceContext, Owner.Creature, DynamicVars["Energy"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Energy"].UpgradeValueBy(BalanceDelta("BaseEnergy", "UpgradeEnergy", 1m)); // 1 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火焰之舞", "每回合第一次打出[gold]火忍[/gold]牌时，获得 {Energy:diff()} 点能量。")
        : new CardLoc("Fire Ninjutsu: Flame Dance", "The first time you play a Fire Ninjutsu card each turn, gain {Energy:diff()} Energy.");
}
