using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// Routes the Black Knight's native boss UI lookups to mod-owned artwork and
/// guards old/in-progress maps that were created without the second boss node.
/// </summary>
internal static class BlackKnightBossUiPatches
{
    private const string TopBarIconPath =
        "res://NinjaMod/images/ui/blackknight_topbar.png";

    private const string TopBarOutlinePath =
        "res://NinjaMod/images/ui/blackknight_topbar_outline.png";

    private static bool IsBlackKnight(ModelId? modelId) =>
        modelId != null
        && modelId.Entry == ModelDb.Encounter<BlackKnightEncounter>().Id.Entry;

    private static readonly FieldInfo BossIconField =
        AccessTools.Field(typeof(NTopBarBossIcon), "_bossIcon");

    private static readonly FieldInfo SecondBossIconField =
        AccessTools.Field(typeof(NTopBarBossIcon), "_secondBossIcon");

    private static void PreserveFullColor(TextureRect? icon)
    {
        if (icon?.Texture == null)
            return;

        Texture2D expected =
            PreloadManager.Cache.GetTexture2D(TopBarIconPath);
        if (icon.Texture != expected)
            return;

        // Native boss icons are monochrome masks and use a tint shader.
        // The Black Knight top-bar portrait is already fully colored.
        icon.UseParentMaterial = false;
        icon.Material = null;
        icon.Modulate = Colors.White;
        icon.SelfModulate = Colors.White;
    }

    private static void RefreshBlackKnightPortraitColor(
        NTopBarBossIcon instance)
    {
        PreserveFullColor(BossIconField.GetValue(instance) as TextureRect);
        PreserveFullColor(
            SecondBossIconField.GetValue(instance) as TextureRect);
    }

    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath))]
    private static class RoomIconPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ModelId? modelId, ref string? __result)
        {
            if (IsBlackKnight(modelId))
                __result = TopBarIconPath;
        }
    }

    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath))]
    private static class RoomIconOutlinePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ModelId? modelId, ref string? __result)
        {
            if (IsBlackKnight(modelId))
                __result = TopBarOutlinePath;
        }
    }

    [HarmonyPatch(
        typeof(NTopBarBossIcon),
        nameof(NTopBarBossIcon.RefreshBossIcon))]
    private static class TopBarPortraitColorPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NTopBarBossIcon __instance) =>
            RefreshBlackKnightPortraitColor(__instance);
    }

    [HarmonyPatch(
        typeof(NTopBarBossIcon),
        nameof(NTopBarBossIcon.RefreshSecondBossIconColor))]
    private static class TopBarSecondPortraitColorPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NTopBarBossIcon __instance) =>
            RefreshBlackKnightPortraitColor(__instance);
    }

    /// <summary>
    /// Versions prior to 1.4.1 could reach the first Act 3 boss with a Black
    /// Knight second encounter but no second map point.  The vanilla rewards
    /// button interprets that topology as the end of the run.  Intercept only
    /// that invalid state and enter the pending second boss directly.
    /// </summary>
    [HarmonyPatch(typeof(NRewardsScreen), "OnProceedButtonPressed")]
    private static class PendingSecondBossRewardsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            RunManager manager = RunManager.Instance;
            RunState? state = manager.State;
            if (state == null
                || state.CurrentRoom is not CombatRoom room
                || room.RoomType != RoomType.Boss
                || !BlackKnightRules.ShouldContinueToPendingSecondBoss(
                    state.Act.Index,
                    currentBossIsBlackKnight: IsBlackKnight(room.Encounter.Id),
                    secondBossIsBlackKnight:
                        IsBlackKnight(state.Act.SecondBossEncounter?.Id)))
            {
                return true;
            }

            if (state.Map.SecondBossMapPoint != null)
            {
                BlackKnightLog.Important(
                    "[BlackKnight] 第一 Boss 结算完成，返回地图进入黑骑士终战。");
                TaskHelper.RunSafely(
                    manager.ProceedFromTerminalRewardsScreen());
            }
            else
            {
                BlackKnightLog.Important(
                    "[BlackKnight] 旧地图缺少第二 Boss 节点，直接进入黑骑士终战。");
                TaskHelper.RunSafely(manager.EnterMapPointInternal(
                    state.ActFloor + 1,
                    MapPointType.Boss,
                    preFinishedRoom: null,
                    saveGame: true));
            }
            return false;
        }
    }
}
