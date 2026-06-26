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
/// 武藏：神速（Musashi: Godspeed）——技能牌，消耗。
/// 0 费，造成 3（升级 5）点伤害，获得 8（升级 11）点格挡，并抽 1 张牌。
/// </summary>
public class MusashiGodspeed : NinjaModCard
{
    public MusashiGodspeed() : base(BalanceCost(nameof(MusashiGodspeed), 0), BalanceType(nameof(MusashiGodspeed), CardType.Skill), BalanceRarity(nameof(MusashiGodspeed), CardRarity.Uncommon), BalanceTarget(nameof(MusashiGodspeed), TargetType.AnyEnemy)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiGodspeed), true);

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 3m), ValueProp.Move), new BlockVar(BalanceDecimal("BaseBlock", 8m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 2m)); // 3 -> 5
        DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 3m));  // 8 -> 11
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：神速", "造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡，并抽 1 张牌。")
        : new CardLoc("Musashi: Godspeed", "Deal {Damage:diff()} damage, gain {Block:diff()} Block, and draw 1 card.");
}
