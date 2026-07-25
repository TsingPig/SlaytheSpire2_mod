using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NinjaMod.NinjaModCode.Monsters;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 幽冥诅咒（Nether Curse）——黑骑士【诅咒发放】洗入玩家抽牌堆的状态牌。
///
/// 卡牌规则：状态牌、费用 1、可主动打出、不升级、默认不消耗/不虚无/不保留，
/// 打出后正常进入弃牌堆（除非该实例已被【怯懦】<see cref="CowardicePower"/> 赋予消耗）。
///
/// 出牌效果：若黑骑士当前显示的下一意图为【竖劈+】，则为该黑骑士施加一次性的
/// 竖劈保护 <see cref="VerticalSlashProtectionPower"/>：
///  • 使该次竖劈从“不可格挡”变为“普通可格挡”，并让黑骑士恢复等同实际生命伤害的生命。
///  • 保护为 Single 叠层，连续打出多张不会叠加。
///  • 绑定到“具有竖劈意图的具体黑骑士”，不依赖全局单例；场上有多个黑骑士时分别判断。
///  • 若下一意图不是竖劈，则不获得保护，但卡牌仍正常消耗 1 点能量并进入弃牌堆。
/// </summary>
public class NetherCurse : NinjaModCard
{
    public NetherCurse()
        : base(1, CardType.Status, CardRarity.Status, TargetType.None) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 扫描场上所有“下一意图为竖劈+”的黑骑士，为它们绑定一次性竖劈保护。
        var targets = CombatState.Enemies
            .Where(c => c.IsAlive
                        && c.Monster is BlackKnightEnemy bk
                        && BlackKnightConfig.IsVerticalState(bk.NextMove?.StateId))
            .ToList();

        foreach (Creature bkCreature in targets)
        {
            await PowerCmd.Apply<VerticalSlashProtectionPower>(
                choiceContext, bkCreature, 1, Owner.Creature, this);
        }

        if (targets.Count > 0)
        {
            BlackKnightLog.Info($"NetherCurse 生效：为 {targets.Count} 名黑骑士的下一次竖劈附加保护。");
        }
        // 若没有匹配目标：无事发生，卡牌照常消耗能量并进入弃牌堆（由游戏结算处理）。
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("幽冥诅咒",
            "若黑骑士的下一个意图为竖劈+，使该次竖劈伤害可以被格挡。")
        : new CardLoc("Nether Curse",
            "If the Black Knight's next intent is Vertical Slash+, that Vertical Slash becomes blockable.");
}
