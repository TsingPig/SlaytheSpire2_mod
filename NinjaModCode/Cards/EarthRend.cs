using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    // 动态计算格挡 = 所有敌人负面效果（Debuff）层数之和。卡面用 {CalculatedBlock:diff()} 显示。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedBlock(0, (card, creature) =>
        {
            var cs = card.CombatState;
            if (cs == null) return 0m;
            return cs.HittableEnemies.Sum(e => e.Powers.Where(p => p.Type == PowerType.Debuff).Sum(p => p.Amount));
        }, 0, ValueProp.Move);

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
        ? new CardLoc("土忍：裂地", "获得 {CalculatedBlock:diff()} 点格挡（所有敌人[gold]负面效果[/gold]层数之和）。" + (_exhaust ? "[gold]消耗[/gold]。" : ""))
        : new CardLoc("Earth Ninjutsu: Earth Rend", "Gain {CalculatedBlock:diff()} Block (total [gold]Debuff[/gold] stacks on all enemies)." + (_exhaust ? " [gold]Exhaust[/gold]." : ""));
}
