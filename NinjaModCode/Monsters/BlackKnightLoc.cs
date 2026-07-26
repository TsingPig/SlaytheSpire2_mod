using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 把黑暗骑士的自定义意图文本注入游戏的 intents 表，
/// 并为怪物名与招式标题提供运行时兜底。
/// </summary>
internal static class BlackKnightLoc
{
    private const string SentinelKey = BlackKnightConfig.StateCurse + ".__bk_injected";
    private const string MonsterSentinelSuffix = ".__bk_name";

    public static void EnsureInjected()
    {
        var manager = LocManager.Instance;
        if (manager == null)
            return;

        LocTable table;
        try { table = manager.GetTable("intents"); }
        catch { return; }

        if (table == null || table.HasEntry(SentinelKey))
            return;

        var entries = new Dictionary<string, string>();
        foreach ((string key, string value) in Entries())
            entries[key] = value;
        entries[SentinelKey] = "1";
        table.MergeWith(entries);
    }

    public static void EnsureMonsterLoc(string? entry)
    {
        if (string.IsNullOrEmpty(entry))
            return;

        var manager = LocManager.Instance;
        if (manager == null)
            return;

        LocTable table;
        try { table = manager.GetTable("monsters"); }
        catch { return; }

        if (table == null)
            return;

        string sentinel = entry + MonsterSentinelSuffix;
        if (table.HasEntry(sentinel))
            return;

        bool zh = Lang.Zh;
        var entries = new Dictionary<string, string>
        {
            [entry + ".name"] = zh ? "黑暗骑士" : "Black Knight",
        };
        foreach ((string id, string zhTitle, string enTitle) in MoveTitles())
            entries[$"{entry}.moves.{id}.title"] = zh ? zhTitle : enTitle;
        entries[sentinel] = "1";
        table.MergeWith(entries);
    }

    private static IEnumerable<(string id, string zh, string en)> MoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "诅咒发放", "Curse Cast"),
        (BlackKnightConfig.StateDiagonalA, "斜劈", "Diagonal Slash"),
        (BlackKnightConfig.StateDiagonalB, "斜劈", "Diagonal Slash"),
        (BlackKnightConfig.StateHorizontal, "横劈", "Horizontal Slash"),
        (BlackKnightConfig.StateVertical, "竖劈", "Vertical Slash"),
        (BlackKnightConfig.StateDarkArmor, "暗魂铠甲", "Dark Armor"),
        (BlackKnightConfig.StateTrueForm, "强化——真身", "Empower — True Form"),
        (BlackKnightConfig.StateTrueFormHorizA, "横劈", "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormHorizB, "横劈", "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormVertical, "竖劈", "Vertical Slash"),
    ];

    private static IEnumerable<KeyValuePair<string, string>> Entries()
    {
        bool zh = Lang.Zh;

        yield return New("BK_CURSE.title", zh ? "诅咒发放" : "Curse Cast");
        yield return New(
            "BK_CURSE.description",
            zh
                ? "将 5 张[gold]幽冥诅咒[/gold]洗入每名玩家的抽牌堆。每洗入 1 张，获得 1 层[gold]噬命诅印[/gold]。"
                : "Shuffle 5 [gold]Nether Curses[/gold] into each player's draw pile. Gain 1 [gold]Life-Siphon Sigil[/gold] for each card shuffled.");

        yield return New("BK_DIAGONAL.title", zh ? "斜劈" : "Diagonal Slash");
        yield return New(
            "BK_DIAGONAL.description",
            zh ? "造成 {Damage} 点伤害。" : "Deal {Damage} damage.");

        yield return New("BK_HORIZONTAL.title", zh ? "横劈" : "Horizontal Slash");
        yield return New(
            "BK_HORIZONTAL.description",
            zh
                ? "造成 {Damage} 点伤害。若造成生命伤害，将 1 张[gold]伤口[/gold]洗入你的抽牌堆。"
                : "Deal {Damage} damage. If it deals HP damage, shuffle 1 [gold]Wound[/gold] into your draw pile.");

        yield return New("BK_VERTICAL.title", zh ? "竖劈" : "Vertical Slash");
        yield return New(
            "BK_VERTICAL.damage.blockable",
            "[color=#FF5555]{Damage}[/color]");
        yield return New(
            "BK_VERTICAL.damage.unblockable",
            "[b][color=#FFFFFF]{Damage}[/color][/b]");
        yield return New(
            "BK_VERTICAL.description",
            zh
                ? "造成 {Damage} 点伤害。若你回合结束时手牌中有[gold]幽冥诅咒[/gold]，此次攻击[gold]无法被格挡[/gold]。"
                : "Deal {Damage} damage. If you end your turn with a [gold]Nether Curse[/gold] in hand, this attack is [gold]unblockable[/gold].");

        yield return New("BK_DARK_ARMOR.title", zh ? "暗魂铠甲" : "Dark Armor");
        yield return New(
            "BK_DARK_ARMOR.description",
            zh
                ? "玩家方每存在 1 个实体，获得 50 点[gold]格挡[/gold]。"
                : "Gain 50 [gold]Block[/gold] for each player-side entity.");

        yield return New("BK_TRUE_FORM.title", zh ? "强化——真身" : "Empower — True Form");
        yield return New(
            "BK_TRUE_FORM.description",
            zh
                ? "永久获得 5 点[gold]力量[/gold]和[gold]幽冥侵蚀[/gold]；再向每名玩家的抽牌堆洗入 5 张[gold]幽冥诅咒[/gold]，每成功洗入 1 张便获得 1 层[gold]噬命诅印[/gold]。随后依次使用[gold]横劈[/gold]、[gold]横劈[/gold]和[gold]竖劈[/gold]。"
                : "Permanently gain 5 [gold]Strength[/gold] and [gold]Nether Erosion[/gold]. Then shuffle 5 [gold]Nether Curses[/gold] into each player's draw pile and gain 1 [gold]Life-Siphon Sigil[/gold] for each successfully added card. Then use [gold]Horizontal Slash[/gold], [gold]Horizontal Slash[/gold], and [gold]Vertical Slash[/gold].");
    }

    private static KeyValuePair<string, string> New(string key, string value) => new(key, value);
}
