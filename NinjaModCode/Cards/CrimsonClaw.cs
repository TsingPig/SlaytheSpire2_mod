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
/// 1 费，在手牌中生成 2 张注入手里剑（<see cref="InfusedShuriken"/>：普通手里剑 + 燃烧追加 6 + 保留 + 消耗）。
/// </summary>
public class CrimsonClaw : NinjaModCard
{
    // 生成的注入手里剑数量（常量）。
    private const int Count = 2;

    public CrimsonClaw() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < Count; i++)
        {
            await InfusedShuriken.CreateInHand(Owner, CombatState);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：凤仙花爪红", $"在手牌中生成 {Count} 张手里剑（燃烧追加 6、保留、消耗）。消耗。")
        : new CardLoc("Fire Ninjutsu: Crimson Claw", $"Add {Count} Shurikens (Burning Infusion 6, Retain, Exhaust) to your hand. Exhaust.");
}
