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
/// 九字护身法（Kuji Protection）——能力牌，消耗。
/// 2（升级 1）费，获得 3 点抵挡（Resist），并施加 <see cref="KujiProtectionPower"/>：
/// 每回合开始额外获得当前抵挡层数 2 倍的格挡。
/// </summary>
public class KujiProtection : NinjaModCard
{
    // 初始抵挡层数（常量）。
    private const int Resist = 3;

    public KujiProtection() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, Resist, Owner.Creature, this);
        await PowerCmd.Apply<KujiProtectionPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 2 -> 1

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("九字护身法", $"获得 {Resist} 点抵挡。每回合开始时，额外获得当前抵挡层数 2 倍的格挡。消耗。")
        : new CardLoc("Kuji Protection", $"Gain {Resist} Resist. At the start of each turn, gain Block equal to twice your current Resist. Exhaust.");
}
