using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace NinjaMod.NinjaModCode.Powers;

/// <summary>
/// Burning (燃烧): at the start of the affected creature's next turn, deal unblockable damage
/// equal to the Burning amount, then remove it. Can also be "ignited" early (see
/// <see cref="IgniteAsync"/>) for double damage.
///
/// Burning deals Unblockable|Unpowered damage (not a move), so per the v0.1 rules it does NOT
/// trigger Bleed.
/// </summary>
public class BurningPower : NinjaModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner)) return;
        if (Amount <= 0) return;

        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, Amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await PowerCmd.Remove(this);
    }

    /// <summary>
    /// Immediately deal (Burning * 2) unblockable damage to the owner, then remove Burning.
    /// Used by Demon Flame Burst's ignite effect.
    /// </summary>
    public async Task IgniteAsync(PlayerChoiceContext choiceContext, Creature? dealer)
    {
        if (Amount <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, Amount * 2,
            ValueProp.Unblockable | ValueProp.Unpowered, dealer, null);
        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new PowerLoc("燃烧",
            "在此生物下个回合开始时，受到等同于燃烧层数的无法格挡伤害，然后移除燃烧。",
            "在此生物下个回合开始时，受到等同于燃烧层数的无法格挡伤害，然后移除燃烧。")
        : new PowerLoc("Burning",
            "At the start of this creature's next turn, lose HP equal to Burning, then remove it.",
            "At the start of this creature's next turn, lose HP equal to Burning, then remove it.");
}
