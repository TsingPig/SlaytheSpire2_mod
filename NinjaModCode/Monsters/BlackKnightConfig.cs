using Godot;

namespace NinjaMod.NinjaModCode.Monsters;

/// <summary>
/// 黑骑士（Black Knight）Boss 的集中配置。
///
/// 所有关键数值、资源路径、动画时长、特效颜色与调试开关都集中在这里，
/// 避免在战斗逻辑、状态机、卡牌与特效代码中散落硬编码。调整平衡只需修改本文件。
///
/// 本文件不依赖任何战斗运行时类型，可被自动化测试直接引用。
/// </summary>
internal static class BlackKnightConfig
{
    // ─────────────────────────────────────────────────────────────────────
    // 调试开关
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 已停用的快速测试覆盖：为 true 时，开始一局新游戏并进入第一幕第一场普通战斗时，
    /// 原本的敌人会被替换为一名黑骑士，便于立即测试。
    ///
    /// 发布版本保持 false；黑骑士现在通过正式第三幕最终 Boss 接入。
    /// 该替换只发生一次（见 <see cref="BlackKnightDebugEncounterOverride"/> 的一次性标记）。
    /// </summary>
    public const bool DebugForceBlackKnightFirstEncounter = false;

    /// <summary>是否输出黑骑士状态机 / 伤害 / 卡牌触发的详细日志。</summary>
    public const bool VerboseLogging = true;

    // ─────────────────────────────────────────────────────────────────────
    // 基础属性
    // ─────────────────────────────────────────────────────────────────────

    // 关键数值全部转发自纯逻辑规则 BlackKnightRules（唯一事实来源，可离线单元测试）。

    /// <summary>最大生命值 = 初始生命值。</summary>
    public const int MaxHp = BlackKnightRules.MaxHp;

    /// <summary>斜劈基础伤害。</summary>
    public const int DiagonalSlashDamage = BlackKnightRules.DiagonalSlashDamage;

    /// <summary>横砍基础伤害。</summary>
    public const int HorizontalSlashDamage = BlackKnightRules.HorizontalSlashDamage;

    /// <summary>竖劈+ 基础伤害。</summary>
    public const int VerticalSlashDamage = BlackKnightRules.VerticalSlashDamage;

    /// <summary>暗鬼铠甲：每个存活的玩家方实体提供的格挡。</summary>
    public const int DarkArmorBlockPerEntity = BlackKnightRules.DarkArmorBlockPerEntity;

    /// <summary>真身强化永久获得的力量。</summary>
    public const int TrueFormStrength = BlackKnightRules.TrueFormStrength;

    /// <summary>诅咒发放阶段洗入抽牌堆的【幽冥诅咒】数量。</summary>
    public const int CurseCardCount = BlackKnightRules.CurseCardCount;

    /// <summary>真身强化回合额外向每名玩家洗入的【幽冥诅咒】数量。</summary>
    public const int TrueFormCurseCardCount = BlackKnightRules.TrueFormCurseCardCount;

    /// <summary>真身强化后固定执行的行动数量（横砍→横砍→竖劈）。</summary>
    public const int TrueFormActionCount = BlackKnightRules.TrueFormActionCount;

    // ─────────────────────────────────────────────────────────────────────
    // 状态机的状态 ID（稳定字符串，供存档 / 读取时正确恢复阶段）
    // ─────────────────────────────────────────────────────────────────────

    public const string StateCurse            = BlackKnightRules.StateCurse;
    public const string StateDiagonalA        = BlackKnightRules.StateDiagonalA;
    public const string StateDiagonalB        = BlackKnightRules.StateDiagonalB;
    public const string StateHorizontal       = BlackKnightRules.StateHorizontal;
    public const string StateVertical         = BlackKnightRules.StateVertical;
    public const string StateDarkArmor        = BlackKnightRules.StateDarkArmor;
    public const string StateTrueForm         = BlackKnightRules.StateTrueForm;
    public const string StateTrueFormHorizA   = BlackKnightRules.StateTrueFormHorizA;
    public const string StateTrueFormHorizB   = BlackKnightRules.StateTrueFormHorizB;
    public const string StateTrueFormVertical = BlackKnightRules.StateTrueFormVertical;

    /// <summary>该状态 ID 是否为一次“竖劈+”（普通循环或真身阶段的竖劈）。</summary>
    public static bool IsVerticalState(string? stateId) => BlackKnightRules.IsVerticalState(stateId);

    /// <summary>该状态 ID 是否为一次“横砍”。</summary>
    public static bool IsHorizontalState(string? stateId) => BlackKnightRules.IsHorizontalState(stateId);

    /// <summary>该状态 ID 是否属于真身阶段（真身进入或真身的三次行动）。用于存档/读档后恢复幽影可见性。</summary>
    public static bool IsTrueFormState(string? stateId) => BlackKnightRules.IsTrueFormState(stateId);

    /// <summary>该状态是否执行完整诅咒效果包（洗牌 + 按成功数量获得噬命诅印）。</summary>
    public static bool StateAppliesCursePackage(string? stateId) =>
        BlackKnightRules.StateAppliesCursePackage(stateId);

    /// <summary>从唯一状态机规范取得后继，避免运行时代码与 intentgraph/test 各自漂移。</summary>
    public static string NextState(string? stateId) => BlackKnightRules.NextState(stateId);

    // ─────────────────────────────────────────────────────────────────────
    // 资源路径（相对 NinjaMod/，通过扩展方法拼成 res:// 路径）
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>战斗中的黑骑士视觉场景（分层 Sprite2D + AnimationPlayer）。
    /// 相对 <c>NinjaMod/scenes/</c>，通过 <c>.ScenePath()</c> 拼成 res:// 路径。</summary>
    public const string VisualSceneRelPath = "creature_visuals/blackknight.tscn";

    // ─────────────────────────────────────────────────────────────────────
    // 动画状态名（AnimationPlayer 中的动画名）与时长（秒）
    // ─────────────────────────────────────────────────────────────────────

    public const string AnimIdle           = "Idle";
    public const string AnimCurseCast       = "CurseCast";
    public const string AnimDiagonalSlashA  = "DiagonalSlashA";
    public const string AnimDiagonalSlashB  = "DiagonalSlashB";
    public const string AnimHorizontalSlash = "HorizontalSlash";
    public const string AnimVerticalSlash   = "VerticalSlash";
    public const string AnimDarkArmor       = "DarkArmor";
    public const string AnimTrueFormEnter   = "TrueFormEnter";
    public const string AnimTrueFormIdle    = "TrueFormIdle";
    public const string AnimTrueFormExit    = "TrueFormExit";
    public const string AnimHurt            = "Hurt";
    public const string AnimDeath           = "Death";

    // 关键动画时长 / 命中帧时刻（秒）。命中帧用于把伤害结算与挥斧命中同步。
    public const float IdleLoopSeconds        = 3.0f;
    public const float CurseCastSeconds       = 1.1f;
    public const float DiagonalWindupSeconds  = 0.80f; // 斜劈 A：贴近、右上抬斧 → 左下命中
    public const float DiagonalBWindupSeconds = 0.80f; // 斜劈 B：相同核心方向，略短回收
    public const float DiagonalApproachDelaySeconds = 0.08f;
    public const float DiagonalApproachSeconds = 0.18f;
    public const float DiagonalReturnDelaySeconds = 0.64f;
    public const float DiagonalReturnSeconds = 0.25f;
    public const float DiagonalTargetStandoff = 180f;
    public const float DiagonalMaxLungeDistance = 720f;
    public const float HorizontalAnimationSeconds = 1.4f;
    public const float HorizontalWindupSeconds = 0.75f;
    public const float HorizontalRecoverySeconds = 0.45f;
    public const float HorizontalApproachDelaySeconds = 0.22f;
    public const float HorizontalApproachSeconds = 0.24f;
    public const float HorizontalReturnDelaySeconds = 0.48f;
    public const float HorizontalReturnSeconds = 0.44f;
    public const float HorizontalTargetStandoff = 210f;
    public const float HorizontalMaxLungeDistance = 700f;
    public const float VerticalWindupSeconds  = 0.7f;  // 竖劈高举停顿更久
    public const float DarkArmorSeconds       = 1.0f;
    public const float TrueFormEnterSeconds   = 1.2f;
    public const float TrueFormExitSeconds    = 0.9f;
    public const float HitStopSeconds         = 0.12f; // 命中顿帧

    // ─────────────────────────────────────────────────────────────────────
    // 复用游戏内置的攻击特效（不复制任何受版权保护的资源到仓库）
    // ─────────────────────────────────────────────────────────────────────

    public const string SlashVfx = "vfx/vfx_attack_slash";
    public const string HeavyVfx = "vfx/vfx_heavy_blunt";

    // ─────────────────────────────────────────────────────────────────────
    // 特效配色（参考概念设计图：紫色 / 暗红 / 银白 / 黑色 / 幽魂绿）
    // ─────────────────────────────────────────────────────────────────────

    public static readonly Color ColorPurple   = new("6b3f9e");
    public static readonly Color ColorDarkRed   = new("8f1f2e");
    public static readonly Color ColorSilver    = new("c9cdd6");
    public static readonly Color ColorBlack     = new("14121a");
    public static readonly Color ColorSoulGreen = new("5fe08a");
    public static readonly Color ColorFaceGlow  = new("ff3b3b");

    // 屏幕震动强度 / 时长（竖劈重击）。
    public const float ScreenShakeStrength = 14f;
    public const float HorizontalScreenShakeStrength = 30f;
    public const float HorizontalAftershockShakeStrength = 17f;
    public const float HorizontalBladeWaveLength = 1500f;
    public const float HorizontalBladeWaveTiltDegrees = 7f;
    public const float ScreenShakeSeconds  = 0.35f;
}
