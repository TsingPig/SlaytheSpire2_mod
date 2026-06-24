using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 气合（Ki Breath）——技能牌。
/// 1 费，回复 3（升级 5）点生命。
/// </summary>
public class KiBreath : NinjaModCard
{
    public KiBreath() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(3m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 对自身回血：Heal(target, amount, playVfx)。
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.IntValue, true);
    }

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(2m);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("气合", "回复 {Heal:diff()} 点生命。")
        : new CardLoc("Ki Breath", "Heal {Heal:diff()} HP.");
}
