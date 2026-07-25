using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 调试测试覆盖：当 <see cref="BlackKnightConfig.DebugForceBlackKnightFirstEncounter"/> 为 true 时，
/// 把“第一幕第一场普通战斗”的怪物替换为一名黑骑士，便于立即进入战斗测试。
///
/// 采用最小侵入方式：以 Harmony 后置补丁挂在 <see cref="EncounterModel.GenerateMonstersWithSlots"/> 之后，
/// 直接改写该遭遇实例私有字段 <c>_monstersWithSlots</c> 为单个黑骑士。这样：
///  • 不永久覆盖任何原版怪物/遭遇资源；
///  • 保留原遭遇的房间类型、奖励、地图与存档结构（战斗仍能正常结算）；
///  • 关闭开关即恢复原版逻辑。
///
/// 一次性：以“当前 run 对象引用”为键，确保每一局新游戏只替换其第一幕第一场普通战斗一次，
/// 后续战斗不再替换；开新一局（新的 RunState）会重新允许一次替换。
/// </summary>
[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GenerateMonstersWithSlots))]
internal static class BlackKnightDebugEncounterOverride
{
    // 以 run 对象引用为键的一次性标记（跨局自动重置）。
    private static object? _replacedForRun;

    private static readonly FieldInfo? MonstersField =
        typeof(EncounterModel).GetField("_monstersWithSlots",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    private static void Postfix(EncounterModel __instance, IRunState runState)
    {
        bool alreadyReplaced = ReferenceEquals(_replacedForRun, runState);
        if (!BlackKnightRules.ShouldReplaceFirstEncounter(
                BlackKnightConfig.DebugForceBlackKnightFirstEncounter,
                runState.CurrentActIndex,
                __instance.RoomType == RoomType.Monster,
                alreadyReplaced))
            return;
        if (MonstersField == null) return;

        var bk = ModelDb.Monster<BlackKnightEnemy>().ToMutable();
        var replacement = new List<(MonsterModel, string?)> { (bk, null) };
        MonstersField.SetValue(__instance, replacement);

        _replacedForRun = runState;
        BlackKnightLog.Important("[BlackKnightDebug] Replaced first Act 1 combat with Black Knight.");
    }
}
