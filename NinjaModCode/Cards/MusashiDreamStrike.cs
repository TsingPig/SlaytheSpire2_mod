using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：神梦一击（Musashi: Dream Strike）——攻击牌（罕见），消耗。
/// 4 费，造成 46（升级 62）点伤害。
/// </summary>
public class MusashiDreamStrike : NinjaModCard
{
    public MusashiDreamStrike() : base(BalanceCost(nameof(MusashiDreamStrike), 4), BalanceType(nameof(MusashiDreamStrike), CardType.Attack), BalanceRarity(nameof(MusashiDreamStrike), CardRarity.Uncommon), BalanceTarget(nameof(MusashiDreamStrike), TargetType.AnyEnemy)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiDreamStrike), true);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 46m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 16m)); // 46 -> 62

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：神梦一击", "造成 {Damage:diff()} 点伤害。")
        : new CardLoc("Musashi: Dream Strike", "Deal {Damage:diff()} damage.");
}
