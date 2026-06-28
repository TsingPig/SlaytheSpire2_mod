using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 燕返（Swallow Return）——攻击牌（普通）。
/// 0 费，造成 4（升级 7）点伤害；若目标拥有【流血】，抽 1（升级 2）张牌。
/// </summary>
public class SwallowReturn : NinjaModCard
{
    public SwallowReturn() : base(BalanceCost(nameof(SwallowReturn), 0), BalanceType(nameof(SwallowReturn), CardType.Attack), BalanceRarity(nameof(SwallowReturn), CardRarity.Common), BalanceTarget(nameof(SwallowReturn), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 4m), ValueProp.Move), new CardsVar(BalanceValue("BaseCards", 1))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 打出前先记录目标是否拥有流血（攻击有可能在结算中改变其状态）。
        bool hadBleed = cardPlay.Target.GetPower<BleedPower>() is { Amount: > 0 };

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (hadBleed)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m)); // 4 -> 7
        DynamicVars.Cards.UpgradeValueBy(BalanceDelta("BaseCards", "UpgradeCards", 1m)); // 1 -> 2
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("燕返", "造成 {Damage:diff()} 点伤害。如果目标拥有[gold]流血[/gold]，抽 {Cards:diff()} 张牌。")
        : new CardLoc("Swallow Return", "Deal {Damage:diff()} damage. If the target has [gold]Bleed[/gold], draw {Cards:diff()} cards.");
}
