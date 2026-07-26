using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using NinjaMod.NinjaModCode.Monsters;
using NinjaMod.NinjaModCode.Powers;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 黑暗骑士发放的状态牌。保留原有费用、可打出方式和弃牌生命周期，
/// 仅把核心效果替换为移除黑暗骑士的一层噬命诅印。
/// </summary>
public class NetherCurse : NinjaModCard
{
    public NetherCurse()
        : base(1, CardType.Status, CardRarity.Status, TargetType.None) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState;
        if (combatState == null)
            return;

        var sigil = combatState.Enemies
            .Where(creature => creature.IsAlive && creature.Monster is BlackKnightEnemy)
            .Select(creature => creature.GetPower<LifeSiphonSigilPower>())
            .FirstOrDefault(power => power is { Amount: > 0 });

        if (sigil == null)
            return;

        sigil.Flash();
        await PowerCmd.Decrement(sigil);
        BlackKnightLog.Info("幽冥诅咒：移除黑暗骑士 1 层噬命诅印。");
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc(
            "幽冥诅咒",
            "移除黑暗骑士 1 层[gold]噬命诅印[/gold]。")
        : new CardLoc(
            "Nether Curse",
            "Remove 1 [gold]Life-Siphon Sigil[/gold] from the Black Knight.");
}
