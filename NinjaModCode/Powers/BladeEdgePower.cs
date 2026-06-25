using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// 锋刃（Blade Edge）力量：本场战斗中，之后生成的手里剑（<see cref="Shuriken"/>）、
/// 飞刀（<see cref="Kunai"/>）以及注入手里剑（<see cref="InfusedShuriken"/>）的能量消耗降低本力量的层数。
/// 配合 <see cref="Cards.BladeEdge"/> 卡牌：打出时既降低当前牌堆中的目标卡，又施加此力量以覆盖后续生成的牌。
/// </summary>
public class BladeEdgePower : NinjaModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player player)
    {
        if (player == Owner.Player &&
            (card is Shuriken || card is Kunai || card is InfusedShuriken))
        {
            card.EnergyCost.AddThisCombat(-Amount, false);
        }
        await Task.CompletedTask;
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("锋刃",
            "本场战斗中，生成的[gold]手里剑[/gold]、[gold]飞刀[/gold]与[gold]火焰手里剑[/gold]的能量消耗降低本力量的层数。",
            "本场战斗中，生成的[gold]手里剑[/gold]、[gold]飞刀[/gold]与[gold]火焰手里剑[/gold]的能量消耗降低本力量的层数。")
        : new PowerLoc("Blade Edge",
            "This combat, generated Shurikens, Kunai, and Flame Shurikens cost this much less Energy.",
            "This combat, generated Shurikens, Kunai, and Flame Shurikens cost this much less Energy.");
}
