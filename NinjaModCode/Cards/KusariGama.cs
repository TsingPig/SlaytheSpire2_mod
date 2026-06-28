using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 锁镰（Kusari-Gama）——攻击牌。
/// 1 费，造成 8（升级 11）点伤害；若目标拥有【流血】，额外给予 2 层虚弱。
/// </summary>
public class KusariGama : NinjaModCard
{
    // 流血时附加的虚弱层数（常量）。
    private int Weak => BalanceConst(nameof(KusariGama), nameof(Weak), 2);

    public KusariGama() : base(BalanceCost(nameof(KusariGama), 1), BalanceType(nameof(KusariGama), CardType.Attack), BalanceRarity(nameof(KusariGama), CardRarity.Uncommon), BalanceTarget(nameof(KusariGama), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 8m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hadBleed = cardPlay.Target.GetPower<BleedPower>() is { Amount: > 0 };

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        if (hadBleed)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, Weak, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 3m)); // 8 -> 11

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("锁镰", $"造成 {{Damage:diff()}} 点伤害。如果目标拥有[gold]流血[/gold]，额外给予 {Weak} 层[gold]虚弱[/gold]。")
        : new CardLoc("Kusari-Gama", $"Deal {{Damage:diff()}} damage. If the target has [gold]Bleed[/gold], apply {Weak} [gold]Weak[/gold].");
}
