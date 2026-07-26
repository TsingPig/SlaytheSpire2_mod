using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 将黑骑士确定性地放进第三幕的最终 Boss 槽位。
///
/// 新开局在全部房间（包括高进阶双 Boss）生成完毕后覆盖最终槽位；
/// 读取旧存档时再次执行同一迁移，使尚未进行的第三幕终战也能升级为黑骑士。
/// 两条路径都只写入稳定的 Encounter ModelId，联机端会得到完全相同的房间状态。
/// </summary>
internal static class BlackKnightActThreeBossOverride
{
    private static readonly PropertyInfo? RunStateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    private static void AssignToFinalBossSlot(ActModel act, string source)
    {
        if (!BlackKnightRules.IsActThree(act.Index))
            return;

        EncounterModel encounter = ModelDb.Encounter<BlackKnightEncounter>();
        bool useSecondBossSlot = BlackKnightRules.IsFinalBossSlot(
            act.HasSecondBoss,
            isSecondBossSlot: true);

        if (useSecondBossSlot)
            act.SetSecondBossEncounter(encounter);
        else
            act.SetBossEncounter(encounter);

        string slot = useSecondBossSlot ? "第二 Boss（最终战）" : "Boss（最终战）";
        BlackKnightLog.Important(
            $"[BlackKnight] 已将第三幕{slot}固定为黑骑士；来源={source}。");
    }

    private static bool MigrateSavedMap(SerializableActMap savedMap)
    {
        if (!BlackKnightRules.ShouldAddSecondBossMapPoint(
                hasSecondBossEncounter: true,
                hasSecondBossMapPoint: savedMap.SecondBossPoint != null))
            return false;

        MapCoord secondBossCoord = new(
            savedMap.BossPoint.Coord.col,
            BlackKnightRules.SecondBossMapRow(savedMap.BossPoint.Coord.row));

        savedMap.SecondBossPoint = new SerializableMapPoint
        {
            Coord = secondBossCoord,
            PointType = MapPointType.Boss,
            CanBeModified = false,
        };

        savedMap.BossPoint.ChildCoords ??= [];
        if (!savedMap.BossPoint.ChildCoords.Contains(secondBossCoord))
            savedMap.BossPoint.ChildCoords.Add(secondBossCoord);

        return true;
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    private static class NewRunPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RunManager __instance)
        {
            try
            {
                if (RunStateProperty?.GetValue(__instance) is not RunState runState)
                {
                    BlackKnightLog.Warn("生成房间后无法取得 RunState，未能放置第三幕固定 Boss。");
                    return;
                }

                ActModel? thirdAct = runState.Acts.FirstOrDefault(
                    act => BlackKnightRules.IsActThree(act.Index));
                if (thirdAct != null)
                    AssignToFinalBossSlot(thirdAct, "新开局");
            }
            catch (Exception e)
            {
                BlackKnightLog.Warn($"新开局放置第三幕固定 Boss 失败：{e}");
            }
        }
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad))]
    private static class SavedRunPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ActModel __instance)
        {
            try
            {
                AssignToFinalBossSlot(__instance, "存档迁移");
            }
            catch (Exception e)
            {
                BlackKnightLog.Warn($"存档迁移第三幕固定 Boss 失败：{e}");
            }
        }
    }

    /// <summary>
    /// ValidateRoomsAfterLoad 只恢复遭遇槽；旧存档保存的地图拓扑还可能没有第二 Boss 节点。
    /// InitializeSavedRun 结束时 SavedMapsToLoad 已经建立，此处为第三幕保存地图补齐
    /// Boss1 → Boss2 的节点与连线，原版奖励界面随后才会正确进入第二战。
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "InitializeSavedRun")]
    private static class SavedMapPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RunManager __instance)
        {
            try
            {
                if (RunStateProperty?.GetValue(__instance) is not RunState runState)
                    return;

                ActModel? thirdAct = runState.Acts.FirstOrDefault(
                    act => BlackKnightRules.IsActThree(act.Index));
                if (thirdAct == null || !thirdAct.HasSecondBoss)
                    return;

                if (__instance.SavedMapsToLoad == null
                    || !__instance.SavedMapsToLoad.TryGetValue(
                        BlackKnightRules.ActThreeIndex,
                        out SerializableActMap? savedMap))
                    return;

                if (MigrateSavedMap(savedMap))
                {
                    BlackKnightLog.Important(
                        "[BlackKnight] 已为旧存档补齐第三幕第二 Boss 地图节点与连线。");
                }
            }
            catch (Exception e)
            {
                BlackKnightLog.Warn($"旧存档第二 Boss 地图迁移失败：{e}");
            }
        }
    }
}
