using System.Collections.Generic;
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
/// Earth Ninjutsu: Earth Escape (土忍：土遁) - Power. Gain 1 (2 upgraded) Resist.
/// </summary>
public class EarthEscape : NinjaModCard
{
    public EarthEscape() : base(BalanceCost(nameof(EarthEscape), 0), BalanceType(nameof(EarthEscape), CardType.Power), BalanceRarity(nameof(EarthEscape), CardRarity.Common), BalanceTarget(nameof(EarthEscape), TargetType.Self)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ResistPower>("Resist", BalanceDecimal("BaseResist", 1m))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = (int)DynamicVars["Resist"].BaseValue;
        await PowerCmd.Apply<ResistPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Resist"].UpgradeValueBy(BalanceDelta("BaseResist", "UpgradeResist", 1m));

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：土遁", "获得 {Resist:diff()} 层[gold]抵挡[/gold]。")
        : new CardLoc("Earth Ninjutsu: Earth Escape", "Gain {Resist:diff()} [gold]Resist[/gold].");
}
