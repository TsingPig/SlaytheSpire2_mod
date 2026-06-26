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
/// 土忍：聚石刺（Earth Ninjutsu: Stone Gather Thrust）——攻击牌。
/// 1 费，造成 6（升级 9）点伤害，获得 6（升级 9）点格挡。
/// </summary>
public class StoneGatherThrust : NinjaModCard
{
    public StoneGatherThrust() : base(BalanceCost(nameof(StoneGatherThrust), 1), BalanceType(nameof(StoneGatherThrust), CardType.Attack), BalanceRarity(nameof(StoneGatherThrust), CardRarity.Common), BalanceTarget(nameof(StoneGatherThrust), TargetType.AnyEnemy)) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BalanceDecimal("BaseDamage", 6m), ValueProp.Move), new BlockVar(BalanceDecimal("BaseBlock", 6m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m));  // 6 -> 9
        DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 3m));   // 6 -> 9
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：聚石刺", "造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡。")
        : new CardLoc("Earth Ninjutsu: Stone Gather Thrust", "Deal {Damage:diff()} damage and gain {Block:diff()} Block.");
}
