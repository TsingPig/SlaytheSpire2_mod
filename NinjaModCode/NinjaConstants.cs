using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace NinjaMod.NinjaModCode;

/// <summary>
/// Shared constants and tiny helpers for the Ninja mod.
/// All gameplay IDs are the model class names themselves (stable, namespaced under the
/// "NinjaMod" mod prefix that BaseLib applies). They are listed here for reference only.
/// </summary>
internal static class NinjaConstants
{
    // VFX reused from the base game (no copyrighted assets copied into the repo).
    public const string SlashVfx = "vfx/vfx_attack_slash";
    public const string HeavyVfx = "vfx/vfx_heavy_blunt";

    // Stable string ids (entry names) -- these equal the model class names.
    public const string CharacterId = "Ninja";
}

/// <summary>
/// Minimal localization language switch so models can return English or Simplified Chinese
/// strings from their in-code <c>Localization</c> provider. "zhs" is the game's 3-letter
/// code for Simplified Chinese (verified against the game's LocManager mapping).
/// </summary>
internal static class Lang
{
    public static bool Zh => LocManager.Instance?.Language == "zhs";
}
