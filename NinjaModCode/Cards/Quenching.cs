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
/// 1 费，施加本回合的 <see cref="QuenchingPower"/>：本回合用攻击牌每次伤害额外附加 6 层燃烧。
/// </summary>
public class Quenching : NinjaModCard
{
    // 与 QuenchingPower.BurningPerHit 保持一致，卡牌描述用此常量显示。
    private const int BurningPerHit = 6;

    public Quenching() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // amount=1 仅表示激活；实际燃烧值在 Power 内部为常量。
        await PowerCmd.Apply<QuenchingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：淡火术", $"本回合，你用攻击牌每次造成伤害都会额外附加 {BurningPerHit} 层燃烧。")
        : new CardLoc("Fire Ninjutsu: Quenching", $"This turn, your attack cards apply {BurningPerHit} Burning on each hit.");
}
