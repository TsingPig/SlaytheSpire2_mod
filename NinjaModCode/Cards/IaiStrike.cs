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
/// 居合（Iai Strike）——攻击牌。
/// 2 费，造成 10（升级 15）点伤害；若打出后玩家仍有剩余能量（> 0），
/// 则额外造成 5（升级 8）点伤害并附加 3 层流血。
/// 主伤害用 DamageVar，额外伤害用 <see cref="_extra"/> 变量，均随升级改变。
/// </summary>
public class IaiStrike : NinjaModCard
{
    // 额外附加的流血层数。
    private int Bleed => BalanceConst(nameof(IaiStrike), nameof(Bleed), 3);

    public IaiStrike() : base(BalanceCost(nameof(IaiStrike), 2), BalanceType(nameof(IaiStrike), CardType.Attack), BalanceRarity(nameof(IaiStrike), CardRarity.Common), BalanceTarget(nameof(IaiStrike), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 10m), ValueProp.Move), new ExtraDamageVar(BalanceDecimal("BaseExtraDamage", 5m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 主伤害。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 释放后若仍有剩余能量（> 0），追加伤害与流血。
        if (Owner.PlayerCombatState.Energy > 0)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.ExtraDamage.BaseValue,
                ValueProp.Move, Owner.Creature, this);
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, Bleed, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 5m));       // 10 -> 15
        DynamicVars.ExtraDamage.UpgradeValueBy(BalanceDelta("BaseExtraDamage", "UpgradeExtraDamage", 3m));  // 5 -> 8
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("居合", $"造成 {{Damage:diff()}} 点伤害。若打出后仍有能量，额外造成 {{ExtraDamage:diff()}} 点伤害并附加 {Bleed} 层[gold]流血[/gold]。")
        : new CardLoc("Iai Strike", $"Deal {{Damage:diff()}} damage. If you still have Energy, deal {{ExtraDamage:diff()}} extra damage and apply {Bleed} [gold]Bleed[/gold].");
}
