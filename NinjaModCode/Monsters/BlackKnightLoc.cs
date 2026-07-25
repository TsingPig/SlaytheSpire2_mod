using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 将黑骑士自定义意图的本地化文本注入游戏内置的 "intents" 本地化表。
///
/// 意图（<see cref="MegaCrit.Sts2.Core.MonsterMoves.Intents.AbstractIntent"/>）的标题/描述固定从
/// "intents" 表按 <c>&lt;IntentPrefix&gt;.title</c> / <c>.description</c> 读取，且不带模型前缀。
/// 因此这里用 <see cref="LocTable.MergeWith"/> 公共 API 把我们的键并入该表（无需反射）。
///
/// 懒加载：<see cref="EnsureInjected"/> 通过哨兵键判断是否已注入；语言切换会重建 "intents" 表并清掉我们的键，
/// 此时下次渲染意图会重新注入当前语言的文本。
/// </summary>
internal static class BlackKnightLoc
{
    private const string SentinelKey = BlackKnightConfig.StateCurse + ".__bk_injected";

    public static void EnsureInjected()
    {
        var mgr = LocManager.Instance;
        if (mgr == null) return;

        LocTable table;
        try { table = mgr.GetTable("intents"); }
        catch { return; }
        if (table == null) return;

        // 已注入且哨兵仍在 → 跳过。
        if (table.HasEntry(SentinelKey)) return;

        var dict = new Dictionary<string, string>();
        foreach (var kv in Entries())
            dict[kv.Key] = kv.Value;
        dict[SentinelKey] = "1";

        table.MergeWith(dict);
    }

    private const string MonsterSentinelSuffix = ".__bk_name";

    /// <summary>
    /// 兜底：把黑骑士的名字与招式标题直接注入 "monsters" 表
    /// （键 <c>&lt;entry&gt;.name</c> 与 <c>&lt;entry&gt;.moves.&lt;id&gt;.title</c>）。
    ///
    /// <see cref="MegaCrit.Sts2.Core.Models.MonsterModel.Title"/> 固定读取 "monsters" 表的
    /// <c>&lt;Id.Entry&gt;.name</c>。若 BaseLib 的 ILocalizationProvider 自动注册未对本自定义
    /// 怪物生效，名字会回退显示为来源 mod 名（“Ninja mod”）。这里用运行时真实 <paramref name="entry"/>
    /// 主动注入，确保名字与招式标题正确显示，与语言切换后自动重注入兼容。
    /// </summary>
    public static void EnsureMonsterLoc(string? entry)
    {
        if (string.IsNullOrEmpty(entry)) return;
        var mgr = LocManager.Instance;
        if (mgr == null) return;

        LocTable table;
        try { table = mgr.GetTable("monsters"); }
        catch { return; }
        if (table == null) return;

        string sentinel = entry + MonsterSentinelSuffix;
        if (table.HasEntry(sentinel)) return;

        bool zh = Lang.Zh;
        var dict = new Dictionary<string, string>
        {
            [entry + ".name"] = zh ? "黑骑士" : "Black Knight",
        };
        foreach (var (id, zhT, enT) in MoveTitles())
            dict[$"{entry}.moves.{id}.title"] = zh ? zhT : enT;
        dict[sentinel] = "1";

        table.MergeWith(dict);
    }

    private static IEnumerable<(string id, string zh, string en)> MoveTitles() =>
    [
        (BlackKnightConfig.StateCurse, "诅咒发放", "Curse Cast"),
        (BlackKnightConfig.StateDiagonalA, "斜劈", "Diagonal Slash"),
        (BlackKnightConfig.StateDiagonalB, "斜劈", "Diagonal Slash"),
        (BlackKnightConfig.StateHorizontal, "横砍", "Horizontal Slash"),
        (BlackKnightConfig.StateVertical, "竖劈+", "Vertical Slash+"),
        (BlackKnightConfig.StateDarkArmor, "暗鬼铠甲", "Dark Armor"),
        (BlackKnightConfig.StateTrueForm, "强化——真身", "Empower - True Form"),
        (BlackKnightConfig.StateTrueFormHorizA, "横砍", "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormHorizB, "横砍", "Horizontal Slash"),
        (BlackKnightConfig.StateTrueFormVertical, "竖劈+", "Vertical Slash+"),
    ];

    private static IEnumerable<KeyValuePair<string, string>> Entries()
    {
        bool zh = Lang.Zh;

        // 诅咒发放
        yield return New("BK_CURSE.title", zh ? "诅咒发放" : "Curse Cast");
        yield return New("BK_CURSE.description",
            zh ? "将 5 张【幽冥诅咒】洗入你的抽牌堆。不造成伤害。"
               : "Shuffle 5 Nether Curses into your draw pile. Deals no damage.");

        // 斜劈（斜劈 A/B 共用）
        yield return New("BK_DIAGONAL.title", zh ? "斜劈" : "Diagonal Slash");
        yield return New("BK_DIAGONAL.description",
            zh ? "造成 {Damage} 点伤害。"
               : "Deal {Damage} damage.");

        // 横砍
        yield return New("BK_HORIZONTAL.title", zh ? "横砍" : "Horizontal Slash");
        yield return New("BK_HORIZONTAL.description",
            zh ? "造成 {Damage} 点伤害。未完全格挡时，将 1 张【伤口】洗入你的抽牌堆。"
               : "Deal {Damage} damage. If not fully blocked, shuffle 1 Wound into your draw pile.");

        // 竖劈+（标题固定，描述按保护状态动态切换）
        yield return New("BK_VERTICAL.title", zh ? "竖劈+" : "Vertical Slash+");

        // 竖劈+（不可格挡）
        yield return New("BK_VERTICAL_UNBLOCK.description",
            zh ? "造成 {Damage} 点伤害。[无法被格挡]。打出【幽冥诅咒】可使其变为可格挡。"
               : "Deal {Damage} damage. [Unblockable]. Play a Nether Curse to make it blockable.");

        // 竖劈+（已被诅咒转化，可格挡+吸血）
        yield return New("BK_VERTICAL_BLOCK.description",
            zh ? "造成 {Damage} 点伤害。[可以被格挡]。黑骑士将恢复等同于本次实际生命伤害的生命。"
               : "Deal {Damage} damage. [Blockable]. The Black Knight heals equal to the HP damage dealt.");

        // 暗鬼铠甲
        yield return New("BK_DARK_ARMOR.title", zh ? "暗鬼铠甲" : "Dark Armor");
        yield return New("BK_DARK_ARMOR.description",
            zh ? "按玩家方存活实体数 N：黑骑士获得 50×N 点格挡，并使每名玩家获得 N 层【怯懦】。"
               : "Let N = living player-side entities. The Black Knight gains 50×N Block; each player gains N Cowardice.");

        // 强化——真身
        yield return New("BK_TRUE_FORM.title", zh ? "强化——真身" : "Empower — True Form");
        yield return New("BK_TRUE_FORM.description",
            zh ? "黑骑士显现真身，永久获得 5 点力量，接下来固定执行 横砍 → 横砍 → 竖劈+。"
               : "The Black Knight reveals its True Form, permanently gaining 5 Strength, then performs Horizontal → Horizontal → Vertical Slash+.");
    }

    private static KeyValuePair<string, string> New(string key, string value) => new(key, value);
}
