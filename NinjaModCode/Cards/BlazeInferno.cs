using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：豪炎（Fire Ninjutsu: Blaze Inferno）——技能牌。
/// 0 费，对所有敌人追加 7（升级 9）层燃烧。
/// </summary>
public class BlazeInferno : NinjaModCard
{
    // 施加的燃烧层数，升级后提升到 9。
    private int _burning = 7;

    public BlazeInferno() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<BurningPower>(choiceContext, enemy, _burning, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => _burning = 9;

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：豪炎", $"对所有敌人施加 {_burning} 层燃烧。")
        : new CardLoc("Fire Ninjutsu: Blaze Inferno", $"Apply {_burning} Burning to ALL enemies.");
}
