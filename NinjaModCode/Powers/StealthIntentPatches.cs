using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace NinjaMod.NinjaModCode.Powers;

internal static class StealthIntentRules
{
    internal static bool IsStealthed(Creature creature)
    {
        return creature.IsAlive && creature.HasPower<StealthPower>();
    }

    internal static bool AllLivingPlayersAreStealthed(IEnumerable<Creature> creatures)
    {
        var livingPlayers = creatures.Where(creature => creature.IsAlive && creature.IsPlayer).ToList();
        return livingPlayers.Count > 0 && livingPlayers.All(IsStealthed);
    }

    internal static bool IsAttackIntent(AbstractIntent intent)
    {
        return intent.IntentType is IntentType.Attack or IntentType.DeathBlow;
    }
}

/// <summary>
/// Monster attacks cannot select a living player who currently has Stealth.
/// The existing damage-cancellation hook remains as a safety net for monster effects
/// that deal move damage without using AttackCommand.
/// </summary>
[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class StealthAttackTargetPatch
{
    private static void Postfix(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
    {
        var attacker = __instance.Attacker;
        if (attacker?.Monster == null || attacker.Side != CombatSide.Enemy) return;

        __result = __result.Where(target => !StealthIntentRules.IsStealthed(target)).ToList();
    }
}

/// <summary>
/// When every living player is hidden, replace attack/deathblow intent visuals with
/// the game's stun intent. The underlying move may still perform non-attack side effects,
/// but its AttackCommand has no valid stealthed target and therefore deals no attack damage.
/// </summary>
[HarmonyPatch(typeof(NIntent), nameof(NIntent.UpdateIntent))]
internal static class StealthIntentVisualPatch
{
    private static void Prefix(ref AbstractIntent intent, IEnumerable<Creature> targets)
    {
        if (StealthIntentRules.IsAttackIntent(intent) &&
            StealthIntentRules.AllLivingPlayersAreStealthed(targets))
        {
            intent = new StunIntent();
        }
    }
}
