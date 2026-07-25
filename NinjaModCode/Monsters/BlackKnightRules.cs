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
    // ── 关键数值 ─────────────────────────────────────────────────────────
    public const int MaxHp = 500;
    public const int DiagonalSlashDamage = 25;
    public const int HorizontalSlashDamage = 34;
    public const int VerticalSlashDamage = 43;
    public const int DarkArmorBlockPerEntity = 50;
    public const int TrueFormStrength = 5;
    public const int CurseCardCount = 5;
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

    /// <summary>暗鬼铠甲：每名玩家获得的怯懦层数 = N。</summary>
    public static int CowardiceStacks(int playerSideCount) => System.Math.Max(0, playerSideCount);

    /// <summary>横砍：仅当本次实际生命伤害 &gt; 0（未完全格挡）时洗入 1 张伤口。</summary>
    public static bool ShouldAddWound(int hpDamageDealt) => hpDamageDealt > 0;

    /// <summary>
    /// 竖劈+ 的治疗量：仅在被【幽冥诅咒】转为可格挡时，按<b>本次实际生命伤害</b>治疗，
    /// 且不小于 0；不可格挡（未打出诅咒）时不治疗。
    /// </summary>
    public static int VerticalHeal(int hpDamageDealt, bool warded) =>
        warded ? System.Math.Max(0, hpDamageDealt) : 0;

    /// <summary>
    /// 怯懦消耗：拥有 <paramref name="cowardiceStacks"/> 层怯懦时，
    /// 接下来 <paramref name="cursesEnteringHand"/> 张进入手牌的幽冥诅咒中，
    /// 会有 min(层数, 张数) 张获得【消耗】（每张消耗 1 层）。
    /// </summary>
    public static int CowardiceExhaustCount(int cowardiceStacks, int cursesEnteringHand) =>
        System.Math.Min(System.Math.Max(0, cowardiceStacks), System.Math.Max(0, cursesEnteringHand));

    /// <summary>
    /// 调试首战替换判定：仅当开关打开、处于第一幕（0-based act 0）、房间为普通战斗、
    /// 且本局尚未替换过时，才替换为黑骑士。
    /// </summary>
    public static bool ShouldReplaceFirstEncounter(bool debugOn, int actIndex, bool isMonsterRoom, bool alreadyReplacedThisRun) =>
        debugOn && actIndex == 0 && isMonsterRoom && !alreadyReplacedThisRun;
}
