using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Extensions;
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
/// 注入手里剑（Infused Shuriken）——由“火忍：凤仙花爪红”生成的临时 Token 卡。
/// 本质是一张普通手里剑（造成伤害并施加流血），额外附带【燃烧追加 6】、保留、消耗。
/// 复用手里剑的卡面贴图。
/// </summary>
public class InfusedShuriken : NinjaModCard
{
    // 施加的流血层数（与普通手里剑一致）。
    private const int Bleed = 2;

    // 燃烧追加层数：攻击命中后额外施加 6 层燃烧。
    public override int BurningInfusion => 6;

    public InfusedShuriken() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    // 复用手里剑（Shuriken）的卡面贴图。
    public override string CustomPortraitPath => "shuriken.png".BigCardImagePath();
    public override string PortraitPath => "shuriken.png".CardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx(NinjaConstants.SlashVfx)
            .Execute(choiceContext);

        // 普通手里剑：未被完全格挡则施加流血。
        if (attack.Results.SelectMany(r => r).Sum(r => r.UnblockedDamage) > 0)
        {
            await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, Bleed, Owner.Creature, this);
        }

        // 燃烧追加：额外施加燃烧。
        await ApplyBurningInfusion(choiceContext, cardPlay.Target);
    }

    /// <summary>在指定玩家手牌生成一张注入手里剑（供凤仙花爪红调用）。</summary>
    public static async Task CreateInHand(Player owner, ICombatState combatState)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var card = combatState.CreateCard<InfusedShuriken>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("手里剑", $"造成 {{Damage:diff()}} 点伤害。未被完全格挡施加 {Bleed} 层[gold]流血[/gold]。[gold]燃烧追加[/gold] {BurningInfusion}。[gold]保留[/gold]。[gold]消耗[/gold]。")
        : new CardLoc("Shuriken", $"Deal {{Damage:diff()}} damage. If unblocked, apply {Bleed} [gold]Bleed[/gold]. [gold]Burning Infusion[/gold] {BurningInfusion}. [gold]Retain[/gold]. [gold]Exhaust[/gold].");
}
