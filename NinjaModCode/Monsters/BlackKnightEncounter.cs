using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士单怪遭遇。用于完整性与未来的非调试接入（例如加入某一幕的遭遇池）。
/// 调试首战替换（<see cref="BlackKnightDebugEncounterOverride"/>）并不依赖本遭遇——
/// 它直接把首场普通战斗的怪物替换为黑骑士，从而保留原遭遇的地图、奖励与存档结构。
/// </summary>
public sealed class BlackKnightEncounter : CustomEncounterModel, ILocalizationProvider
{
    // CustomEncounterModel 需要通过构造函数传入房间类型（普通战斗）。
    public BlackKnightEncounter() : base(RoomType.Monster, false) { }

    // 仅供完整性：不自动加入任何幕的遇到池（调试首战直接替换怪物，不依赖本遇到）。
    public override bool IsValidForAct(ActModel act) => true;

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
