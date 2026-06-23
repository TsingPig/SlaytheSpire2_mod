using HarmonyLib;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Models;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// Harmony 补丁：让“影心刺”（<see cref="ShadowPierce"/>）只能在隐身（<see cref="StealthPower"/>）
/// 状态下打出。借鉴杀戮尖塔 2 的卡牌可玩性机制——拦截 <c>IsPlayable</c>，
/// 当卡是影心刺且本体没有隐身时，强制判定为不可打出（在手牌中变灰）。
/// </summary>
[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class ShadowPierceStealthPatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result) return; // 本就不可打出则无需处理
        if (__instance is not ShadowPierce) return;

        bool stealthed = __instance.Owner?.Creature?.HasPower<StealthPower>() ?? false;
        if (!stealthed)
        {
            __result = false;
        }
    }
}
