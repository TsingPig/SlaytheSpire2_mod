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
/// 2 费，造成 10（升级 15）点伤害；打出后返还 2 点能量（“释放后仍然有能量”），
/// 随后额外造成 5（升级 8）点伤害并附加 3 层流血。
/// 主伤害用 DamageVar，额外伤害用 <see cref="_extra"/> 变量，均随升级改变。
/// </summary>
public class IaiStrike : NinjaModCard
{
    // 额外伤害，升级后提升到 8。
    private int _extra = 5;
    // 额外附加的流血层数。
    private const int Bleed = 3;

    public IaiStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 主伤害。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 返还本牌花费的 2 点能量（“释放后仍然有能量”）。
        await PlayerCmd.GainEnergy(2m, Owner);

        // 额外伤害（作为攻击/Move，可触发已有流血），并附加流血。
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, _extra,
            ValueProp.Move, Owner.Creature, this);
        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, Bleed, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 10 -> 15
        _extra = 8;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("居合", $"造成 {DynamicVars.Damage.BaseValue} 点伤害。返还 2 点能量，随后额外造成 {_extra} 点伤害并附加 {Bleed} 层流血。")
        : new CardLoc("Iai Strike", $"Deal {DynamicVars.Damage.BaseValue} damage. Refund 2 Energy, then deal {_extra} extra damage and apply {Bleed} Bleed.");
}
