using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士 Boss 遭遇。由 <see cref="BlackKnightActThreeBossOverride"/>
/// 固定放置在第三幕的最终 Boss 槽位中。
/// </summary>
public sealed class BlackKnightEncounter : CustomEncounterModel, ILocalizationProvider
{
    private const string MapNodeBasePath =
        "res://NinjaMod/images/map/blackknight_map_node";

    // 必须是 Boss 房间，才能写入 ActModel 的 Boss / SecondBoss 槽位。
    public BlackKnightEncounter() : base(RoomType.Boss, false) { }

    /// <summary>
    /// The native map node appends .png and _outline.png to this base path.
    /// </summary>
    public override string BossNodePath => MapNodeBasePath;

    // 只允许第三幕；实际固定放置由房间生成/读档迁移补丁负责。
    public override bool IsValidForAct(ActModel act) =>
        BlackKnightRules.IsActThree(act.Index);

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<BlackKnightEnemy>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<BlackKnightEnemy>().ToMutable(), null),
    ];

    public List<(string, string)>? Localization => Lang.Zh
        ? new EncounterLoc("黑骑士", "你被黑骑士击败了。")
        : new EncounterLoc("Black Knight", "You were slain by the Black Knight.");
}
