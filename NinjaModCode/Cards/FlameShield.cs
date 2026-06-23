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
/// 火忍：火盾（Fire Ninjutsu: Flame Shield）——能力牌。
/// 1（升级 0）费，施加 <see cref="FlameShieldPower"/>：每当受到攻击时，对攻击者施加 3 层燃烧。
/// </summary>
public class FlameShield : NinjaModCard
{
    // 与 FlameShieldPower.BurningOnHit 保持一致，卡牌描述用此常量显示。
    private const int BurningOnHit = 3;

    public FlameShield() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FlameShieldPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：火盾", $"获得火盾：每当你受到攻击时，对攻击者施加 {BurningOnHit} 层燃烧。")
        : new CardLoc("Fire Ninjutsu: Flame Shield", $"Gain Flame Shield: whenever you are attacked, apply {BurningOnHit} Burning to the attacker.");
}
