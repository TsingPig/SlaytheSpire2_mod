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
    // 升级后为 false：移除消耗。
    private bool _exhaust = true;

    public EarthRend() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => _exhaust ? [CardKeyword.Exhaust] : [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 统计所有敌人身上的负面效果（Debuff）总层数。
        int totalDebuff = CombatState.HittableEnemies
            .Sum(enemy => enemy.Powers.Where(p => p.Type == PowerType.Debuff).Sum(p => p.Amount));

        if (totalDebuff > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, totalDebuff, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade() => _exhaust = false;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：裂地", "获得等同于所有敌人负面效果层数之和的格挡。" + (_exhaust ? "消耗。" : ""))
        : new CardLoc("Earth Ninjutsu: Earth Rend", "Gain Block equal to the total Debuff stacks on all enemies." + (_exhaust ? " Exhaust." : ""));
}
