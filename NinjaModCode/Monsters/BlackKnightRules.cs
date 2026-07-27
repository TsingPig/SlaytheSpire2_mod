namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士的<b>纯逻辑规则</b>（不依赖 Godot / sts2 / BaseLib 任何运行时类型）。
///
/// 这里集中了状态机顺序、关键数值与可单元测试的规则函数，作为“唯一事实来源”。
/// <see cref="BlackKnightConfig"/> 只是把这些值转发出去（供战斗代码按旧名字引用），
/// 而独立的测试工程只编译本文件即可离线验证核心规则（见 tests/BlackKnightRules.Tests）。
/// </summary>
internal static class BlackKnightRules
{
    /// <summary>游戏幕序号从 0 开始，因此第三幕的索引为 2。</summary>
    public const int ActThreeIndex = 2;

    // ── 关键数值 ─────────────────────────────────────────────────────────
    public const int MaxHp = 500;
    public const int DiagonalSlashDamage = 25;
    public const int HorizontalSlashDamage = 34;
    public const int VerticalSlashDamage = 43;
    public const int DarkArmorBlockPerEntity = 50;
    public const int TrueFormStrength = 5;
    public const int CurseCardCount = 5;
    /// <summary>
    /// 真身强化回合会完整重施一次首回合的诅咒效果。
    /// 单独保留这个规则名，让状态机/意图/测试明确表达“真身也发诅咒”，
    /// 同时复用同一个数量源，避免两处平衡数值漂移。
    /// </summary>
    public const int TrueFormCurseCardCount = CurseCardCount;
    public const int TrueFormActionCount = 3;

    // ── 状态 ID（稳定字符串，用于存档/读档恢复阶段）───────────────────────
    public const string StateCurse            = "BK_CURSE";
    public const string StateDiagonalA        = "BK_DIAGONAL_A";
    public const string StateDiagonalB        = "BK_DIAGONAL_B";
    public const string StateHorizontal       = "BK_HORIZONTAL";
    public const string StateVertical         = "BK_VERTICAL";
    public const string StateDarkArmor        = "BK_DARK_ARMOR";
    public const string StateTrueForm         = "BK_TRUE_FORM";
    public const string StateTrueFormHorizA   = "BK_TF_HORIZONTAL_A";
    public const string StateTrueFormHorizB   = "BK_TF_HORIZONTAL_B";
    public const string StateTrueFormVertical = "BK_TF_VERTICAL";

    /// <summary>
    /// 完整循环顺序（严格固定，不得随机打乱）：
    /// 诅咒 → 斜劈 → 斜劈 → 横砍 → 竖劈+ → 暗鬼铠甲 → 真身
    /// → 横砍 → 横砍 → 竖劈+ →（回到）诅咒。
    /// </summary>
    public static readonly string[] CanonicalSequence =
    {
        StateCurse,
        StateDiagonalA,
        StateDiagonalB,
        StateHorizontal,
        StateVertical,
        StateDarkArmor,
        StateTrueForm,
        StateTrueFormHorizA,
        StateTrueFormHorizB,
        StateTrueFormVertical,
    };

    /// <summary>真身强化后固定执行的三次行动顺序：横砍 → 横砍 → 竖劈+。</summary>
    public static readonly string[] TrueFormSequence =
    {
        StateTrueFormHorizA,
        StateTrueFormHorizB,
        StateTrueFormVertical,
    };

    // ── 状态分类谓词 ─────────────────────────────────────────────────────
    public static bool IsVerticalState(string? stateId) =>
        stateId == StateVertical || stateId == StateTrueFormVertical;

    public static bool IsHorizontalState(string? stateId) =>
        stateId == StateHorizontal || stateId == StateTrueFormHorizA || stateId == StateTrueFormHorizB;

    public static bool IsTrueFormState(string? stateId) =>
        stateId == StateTrueForm || stateId == StateTrueFormHorizA
        || stateId == StateTrueFormHorizB || stateId == StateTrueFormVertical;

    /// <summary>
    /// 会执行“向每名玩家洗入幽冥诅咒，并按成功数量获得噬命诅印”完整效果包的状态。
    /// 这是状态机语义的一部分：首回合与真身强化回合均会执行。
    /// </summary>
    public static bool StateAppliesCursePackage(string? stateId) =>
        stateId == StateCurse || stateId == StateTrueForm;

    /// <summary>给定当前状态，返回循环中的下一个状态 ID（末尾回到诅咒发放）。</summary>
    public static string NextState(string? stateId)
    {
        for (int i = 0; i < CanonicalSequence.Length; i++)
        {
            if (CanonicalSequence[i] == stateId)
                return CanonicalSequence[(i + 1) % CanonicalSequence.Length];
        }
        return StateCurse;
    }

    // ── 纯规则函数（被战斗代码与测试共用）────────────────────────────────

    /// <summary>暗鬼铠甲：黑骑士获得的格挡 = 50 × N。</summary>
    public static int DarkArmorBlock(int playerSideCount) =>
        DarkArmorBlockPerEntity * System.Math.Max(0, playerSideCount);

    /// <summary>横砍：仅当本次实际生命伤害 &gt; 0（未完全格挡）时洗入 1 张伤口。</summary>
    public static bool ShouldAddWound(int hpDamageDealt) => hpDamageDealt > 0;

    /// <summary>
    /// 噬命诅印的治疗量：有至少一层时，仅取本次攻击造成的实际生命损失。
    /// </summary>
    public static int LifeSiphonHeal(int actualHpDamage, int sigilStacks) =>
        sigilStacks > 0 ? System.Math.Max(0, actualHpDamage) : 0;

    /// <summary>每次独立攻击最多消耗一层噬命诅印。</summary>
    public static int LifeSiphonStacksConsumed(int sigilStacks) =>
        sigilStacks > 0 ? 1 : 0;

    /// <summary>发牌失败不计层数；噬命诅印严格等于本次实际成功加入的诅咒总数。</summary>
    public static int LifeSiphonSigilsFromCurseAdds(int successfulAdds) =>
        System.Math.Max(0, successfulAdds);

    /// <summary>玩家回合结束时手牌里有幽冥诅咒，则下一次竖劈对该玩家不可格挡。</summary>
    public static bool VerticalIsUnblockable(bool handContainsNetherCurse) => handContainsNetherCurse;

    /// <summary>
    /// 意图 UI 在玩家回合中读取实时手牌，回合结束后读取已保存的竖劈标记。
    /// </summary>
    public static bool VerticalIntentIsUnblockable(
        bool handContainsNetherCurse,
        bool hasSavedExposure) =>
        handContainsNetherCurse || hasSavedExposure;

    /// <summary>
    /// 幽冥侵蚀仅处理本回合第一张真正从抽牌堆进入手牌的幽冥诅咒。
    /// </summary>
    public static bool ShouldRemoveNetherCurseExhaust(
        bool triggeredThisTurn,
        bool isNetherCurse,
        bool movedFromDrawPile,
        bool endedInHand) =>
        !triggeredThisTurn && isNetherCurse && movedFromDrawPile && endedInHand;

    /// <summary>黑骑士只固定接入第三幕。</summary>
    public static bool IsActThree(int actIndex) => actIndex == ActThreeIndex;

    /// <summary>
    /// A10 双 Boss 时黑骑士占第二槽；A9 及以下占唯一的主 Boss 槽。
    /// </summary>
    public static bool IsBlackKnightFinalSlot(
        int actIndex,
        bool hasDoubleBoss,
        bool isSecondBossSlot) =>
        IsActThree(actIndex) && hasDoubleBoss == isSecondBossSlot;

    /// <summary>
    /// 旧存档可能已有第二 Boss 遭遇，却缺少对应地图节点；这种状态必须增量迁移。
    /// </summary>
    public static bool ShouldAddSecondBossMapPoint(
        bool hasSecondBossEncounter,
        bool hasSecondBossMapPoint) =>
        hasSecondBossEncounter && !hasSecondBossMapPoint;

    /// <summary>A9 及以下的旧坏档如果带有第二 Boss 节点，需要将其删除。</summary>
    public static bool ShouldRemoveSecondBossMapPoint(
        bool hasDoubleBoss,
        bool hasSecondBossMapPoint) =>
        !hasDoubleBoss && hasSecondBossMapPoint;

    /// <summary>第二 Boss 节点紧接在第一 Boss 节点的下一行。</summary>
    public static int SecondBossMapRow(int firstBossMapRow) =>
        firstBossMapRow + 1;

    /// <summary>
    /// A10 第一 Boss 结算时，只要黑骑士仍在第二槽待战，就必须绕过
    /// 原版终局/建筑师入口；地图节点是否存在只决定进入地图还是直接进战。
    /// </summary>
    public static bool ShouldContinueToPendingSecondBoss(
        int actIndex,
        bool currentBossIsBlackKnight,
        bool secondBossIsBlackKnight) =>
        IsActThree(actIndex)
        && !currentBossIsBlackKnight
        && secondBossIsBlackKnight;

    /// <summary>
    /// 调试首战替换判定：仅当开关打开、处于第一幕（0-based act 0）、房间为普通战斗、
    /// 且本局尚未替换过时，才替换为黑骑士。
    /// </summary>
    public static bool ShouldReplaceFirstEncounter(bool debugOn, int actIndex, bool isMonsterRoom, bool alreadyReplacedThisRun) =>
        debugOn && actIndex == 0 && isMonsterRoom && !alreadyReplacedThisRun;
}
