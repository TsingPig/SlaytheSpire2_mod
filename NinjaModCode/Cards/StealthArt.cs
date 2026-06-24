using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 隐身法（Stealth Art）——能力牌（稀有）。
/// 2（升级 1）费，获得 1 点活力（敏捷），并进入隐身状态（<see cref="StealthPower"/>）。
/// </summary>
public class StealthArt : NinjaModCard
{
    // 活力层数（常量）。
    private const int Vigor = 1;

    // 隐身层数：每回合失去 1 层，或攻击后立即结束。
    private const int Stealth = 3;

    public StealthArt() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, Vigor, Owner.Creature, this);
        await PowerCmd.Apply<StealthPower>(choiceContext, Owner.Creature, Stealth, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("隐身法", $"获得 {Vigor} 点活力，并获得 {Stealth} 层[gold]隐身[/gold]（敌人无法攻击你；每回合失去 1 层，攻击后立即结束）。")
        : new CardLoc("Stealth Art", $"Gain {Vigor} Vigor and {Stealth} [gold]Stealth[/gold] (enemies can't attack you; lose 1 per turn, or all when you attack).");
}
