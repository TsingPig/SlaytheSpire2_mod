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
/// 火忍：燎原（Fire Ninjutsu: Wildfire）——能力牌（罕见）。
/// 1 费，获得【燎原】：你的回合开始时，给予所有敌人 2（升级 3）层【燃烧】。
/// </summary>
public class Wildfire : NinjaModCard
{
    public Wildfire() : base(BalanceCost(nameof(Wildfire), 1), BalanceType(nameof(Wildfire), CardType.Power), BalanceRarity(nameof(Wildfire), CardRarity.Uncommon), BalanceTarget(nameof(Wildfire), TargetType.Self)) { }

    // 每回合给予所有敌人的燃烧层数：2（升级 3）。PowerVar 自动链接燃烧提示与升级预览。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurningPower>("Burning", BalanceDecimal("BaseBurning", 2m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = (int)DynamicVars["Burning"].BaseValue;
        await PowerCmd.Apply<WildfirePower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Burning"].UpgradeValueBy(BalanceDelta("BaseBurning", "UpgradeBurning", 1m)); // 2 -> 3

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：燎原", "在你的回合开始时，对所有敌人施加 {Burning:diff()} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Wildfire", "At the start of your turn, apply {Burning:diff()} [gold]Burning[/gold] to ALL enemies.");
}
