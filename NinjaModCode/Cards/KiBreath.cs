using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 气合（Ki Breath）——技能牌。
/// 1 费，回复 3（升级 5）点生命。数值用 <see cref="_heal"/> 变量表示，升级时改变。
/// </summary>
public class KiBreath : NinjaModCard
{
    // 回血量，升级后提升到 5。
    private int _heal = 3;

    public KiBreath() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对自身回血：Heal(target, amount, playVfx)。
        await CreatureCmd.Heal(Owner.Creature, _heal, true);
    }

    protected override void OnUpgrade() => _heal = 5;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("气合", $"回复 {_heal} 点生命。")
        : new CardLoc("Ki Breath", $"Heal {_heal} HP.");
}
