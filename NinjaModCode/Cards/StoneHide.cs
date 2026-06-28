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
/// 土忍：石隐术（Earth Ninjutsu: Stone Hide）——能力牌（稀有）。
/// 2 费，获得 1 层【隐身】，并获得 2（升级 3）层【抵挡】。
/// </summary>
public class StoneHide : NinjaModCard
{
    // 获得的隐身层数（常量）。
    private int Stealth => BalanceConst(nameof(StoneHide), nameof(Stealth), 1);

    public StoneHide() : base(BalanceCost(nameof(StoneHide), 2), BalanceType(nameof(StoneHide), CardType.Power), BalanceRarity(nameof(StoneHide), CardRarity.Rare), BalanceTarget(nameof(StoneHide), TargetType.Self)) { }

    // 抵挡层数：2（升级 3）。PowerVar 自动链接抵挡提示与升级预览。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ResistPower>("Resist", BalanceDecimal("BaseResist", 2m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StealthPower>(choiceContext, Owner.Creature, Stealth, Owner.Creature, this);
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, (int)DynamicVars["Resist"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Resist"].UpgradeValueBy(BalanceDelta("BaseResist", "UpgradeResist", 1m)); // 2 -> 3

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：石隐术", $"获得 {Stealth} 层[gold]隐身[/gold]，获得 {{Resist:diff()}} 层[gold]抵挡[/gold]。")
        : new CardLoc("Earth Ninjutsu: Stone Hide", $"Gain {Stealth} [gold]Stealth[/gold] and {{Resist:diff()}} [gold]Resist[/gold].");
}
