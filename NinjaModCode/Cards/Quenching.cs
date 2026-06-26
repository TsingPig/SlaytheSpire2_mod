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
/// 火忍：淬火术（Fire Ninjutsu: Quenching）——技能牌。
/// 1 费，获得 1 层本回合的 <see cref="QuenchingPower"/>；每层使攻击牌每次伤害额外附加 6 层燃烧。
/// </summary>
public class Quenching : NinjaModCard
{
    public Quenching() : base(BalanceCost(nameof(Quenching), 1), BalanceType(nameof(Quenching), CardType.Skill), BalanceRarity(nameof(Quenching), CardRarity.Common), BalanceTarget(nameof(Quenching), TargetType.Self)) { }

    // 用 PowerVar 表示获得的淬火层数：基础 3（升级 4），卡面 {Quench:diff()} 显示。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<QuenchingPower>("Quench", BalanceDecimal("BaseQuench", 3m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = (int)DynamicVars["Quench"].BaseValue;
        await PowerCmd.Apply<QuenchingPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Quench"].UpgradeValueBy(BalanceDelta("BaseQuench", "UpgradeQuench", 1m)); // 3 -> 4

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：淬火术", "获得 {Quench:diff()} 层[gold]淬火[/gold]。本回合，攻击牌每次造成伤害额外附加等同于淬火层数的[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Quenching", "Gain {Quench:diff()} [gold]Quenching[/gold]. This turn, your attacks apply [gold]Burning[/gold] equal to your Quenching stacks on each hit.");
}
