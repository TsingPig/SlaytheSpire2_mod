using System;
using System.Collections.Generic;
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
/// 苦无（Kunai Throw）——攻击牌。
/// 1 费，造成 6（升级 9）点伤害；若目标当前有流血，则恢复 1 点能量。
/// 与藏刃生成的临时“飞刀”（<see cref="Kunai"/>）是不同的卡。
/// </summary>
public class KunaiThrow : NinjaModCard
{
    public KunaiThrow() : base(BalanceCost(nameof(KunaiThrow), 1), BalanceType(nameof(KunaiThrow), CardType.Attack), BalanceRarity(nameof(KunaiThrow), CardRarity.Common), BalanceTarget(nameof(KunaiThrow), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 6m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 命中前判断目标是否有流血（命中后目标可能死亡导致读取不到）。
        bool hadBleed = cardPlay.Target.GetPower<BleedPower>() != null;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (hadBleed)
        {
            await PlayerCmd.GainEnergy(1m, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m));

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("苦无", "造成 {Damage:diff()} 点伤害。如果目标有[gold]流血[/gold]，恢复 1 点能量。")
        : new CardLoc("Kunai Throw", "Deal {Damage:diff()} damage. If the target has [gold]Bleed[/gold], gain 1 Energy.");
}
