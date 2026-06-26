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
/// 须佐能乎（Susanoo）——攻击牌（稀有）。
/// 3 费，造成 7（升级 9）点伤害，共 6 段；每段伤害后立即追加 1 层流血。
/// </summary>
public class Susanoo : NinjaModCard
{
    public Susanoo() : base(BalanceCost(nameof(Susanoo), 3), BalanceType(nameof(Susanoo), CardType.Attack), BalanceRarity(nameof(Susanoo), CardRarity.Rare), BalanceTarget(nameof(Susanoo), TargetType.AnyEnemy)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BalanceDecimal("BaseDamage", 7m), ValueProp.Move), new RepeatVar(BalanceValue("BaseRepeat", 6))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 用单个多段攻击指令（WithHitCount）代替 6 次逐个 await 的独立攻击，
        // 各段之间几乎无间隔，出招明显更快；多段结束后统一追加等数量的流血（结果等同于每段 1 层）。
        int hits = DynamicVars.Repeat.IntValue;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(hits)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, hits * BalanceValue("BaseSusanooBleedPerHit", 1), Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(BalanceDelta("BaseDamage", "UpgradeDamage", 2m)); // 7 -> 9

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("须佐能乎", "造成 {Damage:diff()} 点伤害，共 {Repeat:diff()} 段，每段追加 1 层[gold]流血[/gold]。")
        : new CardLoc("Susanoo", "Deal {Damage:diff()} damage {Repeat:diff()} times; each hit applies 1 [gold]Bleed[/gold].");
}
