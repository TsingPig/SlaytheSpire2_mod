using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：淬火术（Fire Ninjutsu: Quenching）——技能牌。
/// 1 费，获得 1 层本回合的 <see cref="QuenchingPower"/>；每层使攻击牌每次伤害额外附加 6 层燃烧。
/// </summary>
public class Quenching : NinjaModCard
{
    // 与 QuenchingPower.BurningPerHit 保持一致，卡牌描述用此常量显示。
    private const int BurningPerHit = 6;

    public Quenching() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<QuenchingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：淬火术", $"获得 1 层[gold]淬火[/gold]。本回合，每层[gold]淬火[/gold]使攻击牌每次造成伤害额外附加 {BurningPerHit} 层燃烧。")
        : new CardLoc("Fire Ninjutsu: Quenching", $"Gain 1 [gold]Quenching[/gold]. This turn, each stack makes your attack cards apply {BurningPerHit} Burning on each hit.");
}
