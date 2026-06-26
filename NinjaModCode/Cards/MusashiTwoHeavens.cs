using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：二天一流（Musashi: Two Heavens）——技能牌。
/// 3（升级 2）费，造成 16 点伤害，共 2 段。若目标同时拥有流血和燃烧，则使其眩晕。
/// </summary>
public class MusashiTwoHeavens : NinjaModCard
{
    public MusashiTwoHeavens() : base(BalanceCost(nameof(MusashiTwoHeavens), 3), BalanceType(nameof(MusashiTwoHeavens), CardType.Skill), BalanceRarity(nameof(MusashiTwoHeavens), CardRarity.Rare), BalanceTarget(nameof(MusashiTwoHeavens), TargetType.AnyEnemy)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiTwoHeavens), true);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Concat([StunIntent.GetStaticHoverTip()]);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 16m), ValueProp.Move), new RepeatVar(BalanceValue("BaseRepeat", 2))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        var bleed = cardPlay.Target.GetPower<BleedPower>();
        var burning = cardPlay.Target.GetPower<BurningPower>();
        if (bleed != null && bleed.Amount > 0 && burning != null && burning.Amount > 0)
        {
            await CreatureCmd.Stun(cardPlay.Target);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(MusashiTwoHeavens), -1)); // 3 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：二天一流", "造成 {Damage:diff()} 点伤害，共 {Repeat} 段。若目标同时拥有[gold]流血[/gold]与[gold]燃烧[/gold]，则使其[gold]眩晕[/gold]。")
        : new CardLoc("Musashi: Two Heavens", "Deal {Damage:diff()} damage {Repeat} times. If the target has both [gold]Bleed[/gold] and [gold]Burning[/gold], [gold]Stun[/gold] it.");
}
