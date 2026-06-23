using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 锋刃（Blade Edge）——技能牌（稀有），消耗。
/// 2（升级 1）费，本场战斗中，将所有牌堆里的手里剑（<see cref="Shuriken"/>）、
/// 飞刀（<see cref="Kunai"/>）以及火焰手里剑（<see cref="FlameShuriken"/>）的能量消耗永久降低 1。
/// </summary>
public class BladeEdge : NinjaModCard
{
    public BladeEdge() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 遍历所有牌堆（手牌、抽牌堆、弃牌堆），降低目标卡的本场战斗费用。
        var allCards = CardPile.GetCards(Owner,
            new[] { PileType.Hand, PileType.Draw, PileType.Discard });
        foreach (var card in allCards)
        {
            if (card is Shuriken || card is Kunai || card is FlameShuriken)
            {
                card.EnergyCost.AddThisCombat(-1, false);
            }
        }
        await Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("锋刃", "本场战斗中，所有牌堆里的手里剑与飞刀的能量消耗降低 1。消耗。")
        : new CardLoc("Blade Edge", "This combat, Shurikens and Kunai in all piles cost 1 less Energy. Exhaust.");
}
