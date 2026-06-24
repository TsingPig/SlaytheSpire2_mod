using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：豪炎（Fire Ninjutsu: Blaze Inferno）——技能牌。
/// 0 费，对所有敌人追加 7（升级 9）层燃烧。
/// </summary>
public class BlazeInferno : NinjaModCard
{
    public BlazeInferno() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    // 用 PowerVar 表示燃烧层数：既让卡面 {Burning:diff()} 在锻造时预览 7→9 升级，
    // 又自动关联燃烧效果的悬浮提示。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BurningPower>("Burning", 7m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = (int)DynamicVars["Burning"].BaseValue;
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<BurningPower>(choiceContext, enemy, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Burning"].UpgradeValueBy(2m); // 7 -> 9

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：豪炎", "对所有敌人施加 {Burning:diff()} 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Blaze Inferno", "Apply {Burning:diff()} [gold]Burning[/gold] to ALL enemies.");
}
