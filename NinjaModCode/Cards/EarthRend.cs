using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：裂地（Earth Ninjutsu: Earth Rend）——技能牌，消耗。
/// 1 费，对每个敌人造成等同于其当前负面效果（Debuff）总层数的伤害。
/// </summary>
public class EarthRend : NinjaModCard
{
    public EarthRend() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 遍历所有可命中敌人，按各自负面效果层数造成伤害。
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            int debuffStacks = enemy.Powers
                .Where(p => p.Type == PowerType.Debuff)
                .Sum(p => p.Amount);
            if (debuffStacks <= 0) continue;

            await CreatureCmd.Damage(choiceContext, enemy, debuffStacks,
                ValueProp.Move, Owner.Creature, this);
        }
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：裂地", "对每个敌人造成等同于其负面效果层数的伤害。消耗。")
        : new CardLoc("Earth Ninjutsu: Earth Rend", "Deal damage to each enemy equal to its total Debuff stacks. Exhaust.");
}
