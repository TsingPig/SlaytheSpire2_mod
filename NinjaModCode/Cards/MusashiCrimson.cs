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
/// 武藏：猩红（Musashi: Crimson）——技能牌（罕见），消耗。
/// 0 费，获得 2 层【淬火】。
/// </summary>
public class MusashiCrimson : NinjaModCard
{
    public MusashiCrimson() : base(BalanceCost(nameof(MusashiCrimson), 0), BalanceType(nameof(MusashiCrimson), CardType.Skill), BalanceRarity(nameof(MusashiCrimson), CardRarity.Uncommon), BalanceTarget(nameof(MusashiCrimson), TargetType.Self)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiCrimson), true);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 获得的淬火层数：2（无升级变化），用 PowerVar 自动链接淬火提示。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<QuenchingPower>("Quench", BalanceDecimal("BaseQuench", 2m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = (int)DynamicVars["Quench"].BaseValue;
        await PowerCmd.Apply<QuenchingPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Quench"].UpgradeValueBy(BalanceDelta("BaseQuench", "UpgradeQuench", 0m));

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：猩红", "获得 {Quench:diff()} 层[gold]淬火[/gold]。本回合，攻击牌每次造成伤害额外附加等同于淬火层数的[gold]燃烧[/gold]。")
        : new CardLoc("Musashi: Crimson", "Gain {Quench:diff()} [gold]Quenching[/gold]. This turn, your attacks apply [gold]Burning[/gold] equal to your Quenching stacks on each hit.");
}
