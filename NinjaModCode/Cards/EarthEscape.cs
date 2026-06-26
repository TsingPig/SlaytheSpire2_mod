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
/// Earth Ninjutsu: Earth Escape (土忍：土遁) - Power. Gain 1 (2 upgraded) Resist.
/// </summary>
public class EarthEscape : NinjaModCard
{
    private int _resist = BalanceValue(nameof(EarthEscape), "BaseResist", 1);

    public EarthEscape() : base(BalanceCost(nameof(EarthEscape), 0), BalanceType(nameof(EarthEscape), CardType.Power), BalanceRarity(nameof(EarthEscape), CardRarity.Common), BalanceTarget(nameof(EarthEscape), TargetType.Self)) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, _resist, Owner.Creature, this);
    }

    protected override void OnUpgrade() => _resist = BalanceValue("UpgradeResist", 2);

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：土遁", $"获得 {_resist} 层抵挡。")
        : new CardLoc("Earth Ninjutsu: Earth Escape", $"Gain {_resist} Resist.");
}
