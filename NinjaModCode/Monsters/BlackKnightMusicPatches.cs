using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// Prevents the base run-music controller from starting a native track while
/// the Black Knight controller owns the combat room. The guard becomes false
/// before normal music restoration, so no other encounter is affected.
/// </summary>
[HarmonyPatch]
internal static class BlackKnightMusicPatches
{
    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusic))]
    [HarmonyPrefix]
    private static bool SuppressRunMusicUpdate() => !BlackKnightMusicController.IsActive;

    [HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.PlayCustomMusic))]
    [HarmonyPrefix]
    private static bool SuppressOtherCustomMusic() => !BlackKnightMusicController.IsActive;

    [HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayMusic))]
    [HarmonyPrefix]
    private static bool SuppressDirectNativeMusic() => !BlackKnightMusicController.IsActive;
}
