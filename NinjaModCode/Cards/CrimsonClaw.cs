using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：凤仙花爪红（Fire Ninjutsu: Crimson Claw）——技能牌，消耗。
/// 2（升级 1）费，在手牌中生成 3 张火焰手里剑（<see cref="FlameShuriken"/>，附带 6 层燃烧、保留）。
/// </summary>
public class CrimsonClaw : NinjaModCard
{
    // 生成的火焰手里剑数量（常量）。
    private const int Count = 3;

    public CrimsonClaw() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < Count; i++)
        {
            await FlameShuriken.CreateInHand(Owner, CombatState);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：凤仙花爪红", $"在手牌中生成 {Count} 张火焰手里剑（附带 {FlameShuriken.BurningStacks} 层燃烧、保留）。消耗。")
        : new CardLoc("Fire Ninjutsu: Crimson Claw", $"Add {Count} Flame Shurikens ({FlameShuriken.BurningStacks} Burning, Retain) to your hand. Exhaust.");
}
