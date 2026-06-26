using System;
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
/// 火忍：离火符（Fire Ninjutsu: Parting Flame Talisman）——技能牌。
/// 0 费，给予目标 5（升级 8）层燃烧。打出后该牌回到抽牌堆顶部（不进入弃牌堆），可反复打出。
/// </summary>
public class Lihuo : NinjaModCard
{
    public Lihuo() : base(BalanceCost(nameof(Lihuo), 0), BalanceType(nameof(Lihuo), CardType.Skill), BalanceRarity(nameof(Lihuo), CardRarity.Uncommon), BalanceTarget(nameof(Lihuo), TargetType.AnyEnemy)) { }

    // 用 PowerVar 表示燃烧层数，卡面 {Burning:diff()} 显示 5→8 升级并自动关联燃烧提示。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurningPower>("Burning", BalanceDecimal("BaseBurning", 5m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int amount = (int)DynamicVars["Burning"].BaseValue;
        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Burning"].UpgradeValueBy(BalanceDelta("BaseBurning", "UpgradeBurning", 3m)); // 5 -> 8

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：离火符", "给予目标 {Burning:diff()} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Parting Flame Talisman", "Apply {Burning:diff()} [gold]Burning[/gold] to the target.");
}
