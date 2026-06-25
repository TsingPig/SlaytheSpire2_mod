using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 锋刃（Blade Edge）——技能牌（稀有），消耗。
/// 2（升级 1）费，本场战斗中，将所有牌堆里的手里剑（<see cref="Shuriken"/>）、
/// 飞刀（<see cref="Kunai"/>）以及注入手里剑（<see cref="InfusedShuriken"/>）的能量消耗永久降低 1。
/// </summary>
public class BladeEdge : NinjaModCard
{
    // 降低的能量数量（常量）。
    private const int CostReduction = 1;

    public BladeEdge() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 降低当前所有牌堆（手牌、抽牌堆、弃牌堆）中目标牌的本场战斗费用。
        var allCards = CardPile.GetCards(Owner,
            new[] { PileType.Hand, PileType.Draw, PileType.Discard });
        foreach (var card in allCards)
        {
            if (card is Shuriken || card is Kunai || card is InfusedShuriken)
            {
                card.EnergyCost.AddThisCombat(-CostReduction, false);
            }
        }

        // 施加锄刃力量，使本场战斗中后续生成的手里剑、飞刀与火焰手里剑也减费。
        await PowerCmd.Apply<BladeEdgePower>(choiceContext, Owner.Creature, CostReduction, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("锄刃", $"本场战斗中，所有牌堆中以及后续生成的手里剑、飞刀与火焰手里剑的能量消耗降低 {CostReduction}。")
        : new CardLoc("Blade Edge", $"This combat, Shurikens, Kunai, and Flame Shurikens (including ones generated later) cost {CostReduction} less Energy.");
}
