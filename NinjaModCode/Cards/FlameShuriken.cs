using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火焰手里剑（Flame Shuriken）——由“火忍：凤仙花爪红”生成的临时 Token 卡。
/// 造成 4 点伤害并施加 6 层燃烧。保留 + 消耗。
/// </summary>
public class FlameShuriken : NinjaModCard
{
    // 附加的燃烧层数（常量）。
    private const int Burning = 6;

    public FlameShuriken() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        await PowerCmd.Apply<BurningPower>(choiceContext, cardPlay.Target, Burning, Owner.Creature, this);
    }

    /// <summary>在指定玩家手牌生成一张火焰手里剑（供凤仙花爪红调用）。</summary>
    public static async Task CreateInHand(Player owner, ICombatState combatState)
    {
        if (MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsOverOrEnding) return;
        var card = combatState.CreateCard<FlameShuriken>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火焰手里剑", $"造成 {DynamicVars.Damage.BaseValue} 点伤害，施加 {Burning} 层燃烧。保留。消耗。")
        : new CardLoc("Flame Shuriken", $"Deal {DynamicVars.Damage.BaseValue} damage and apply {Burning} Burning. Retain. Exhaust.");
}
