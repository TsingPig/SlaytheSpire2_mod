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
/// 残影攻击牌（Afterimage Attack）——由【残影】能力生成的 0 费 Token 攻击牌。
/// 造成由生成时确定的固定伤害（原攻击伤害的一半）。打出后消耗，且不会再触发残影。
/// 伤害值通过 <see cref="SetDamage"/> 在生成时写入；卡面用动态计算变量实时显示。
/// </summary>
public class AfterimageAttack : NinjaModCard
{
    // 生成时写入的伤害值（原攻击伤害的一半）。
    private int _damage;

    public AfterimageAttack() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡面伤害 = 生成时写入的 _damage，每次显示/结算时实时读取该实例字段。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedDamage(0, (card, creature) => ((AfterimageAttack)card)._damage, 0, ValueProp.Move);

    /// <summary>由【残影】能力在生成时写入本张残影攻击牌的伤害值。</summary>
    public void SetDamage(int damage) => _damage = damage < 0 ? 0 : damage;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(_damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("残影", "造成 {CalculatedDamage:diff()} 点伤害。")
        : new CardLoc("Afterimage", "Deal {CalculatedDamage:diff()} damage.");
}
