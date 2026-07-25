// 黑骑士纯逻辑规则的可执行测试。覆盖 todo 第十三节 16 个测试中与运行时无关的核心规则。
// 运行方式见 tests/README.md。全部通过时退出码 0，任一失败退出码 1。
using NinjaMod.NinjaModCode.Monsters;

int passed = 0, failed = 0;

void Check(string name, bool ok)
{
    Console.ForegroundColor = ok ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write(ok ? "PASS " : "FAIL ");
    Console.ResetColor();
    Console.WriteLine(name);
    if (ok) passed++; else failed++;
}

Console.WriteLine("=== BlackKnight 纯逻辑规则测试 ===\n");

// 测试 1：基础属性 —— 最大/初始生命均为 500。
Check("T1 基础属性: MaxHp == 500", BlackKnightRules.MaxHp == 500);

// 测试 2：状态机顺序 —— 严格固定的完整循环。
string[] expected =
{
    "BK_CURSE", "BK_DIAGONAL_A", "BK_DIAGONAL_B", "BK_HORIZONTAL", "BK_VERTICAL",
    "BK_DARK_ARMOR", "BK_TRUE_FORM", "BK_TF_HORIZONTAL_A", "BK_TF_HORIZONTAL_B", "BK_TF_VERTICAL",
};
Check("T2 状态机顺序: CanonicalSequence 与规范完全一致",
    BlackKnightRules.CanonicalSequence.Length == expected.Length &&
    expected.Zip(BlackKnightRules.CanonicalSequence, (a, b) => a == b).All(x => x));
Check("T2 状态机顺序: NextState 逐步推进正确",
    BlackKnightRules.NextState("BK_CURSE") == "BK_DIAGONAL_A" &&
    BlackKnightRules.NextState("BK_DIAGONAL_A") == "BK_DIAGONAL_B" &&
    BlackKnightRules.NextState("BK_DIAGONAL_B") == "BK_HORIZONTAL" &&
    BlackKnightRules.NextState("BK_HORIZONTAL") == "BK_VERTICAL" &&
    BlackKnightRules.NextState("BK_VERTICAL") == "BK_DARK_ARMOR" &&
    BlackKnightRules.NextState("BK_DARK_ARMOR") == "BK_TRUE_FORM");

// 测试 3：斜劈 —— 25 基础伤害。
Check("T3 斜劈: DiagonalSlashDamage == 25", BlackKnightRules.DiagonalSlashDamage == 25);

// 测试 4/5：横砍 —— 34 伤害；完全格挡不加伤口，未完全格挡加 1 张伤口。
Check("T4 横砍: HorizontalSlashDamage == 34", BlackKnightRules.HorizontalSlashDamage == 34);
Check("T4 横砍完全格挡(实际生命伤害=0): 不加伤口", BlackKnightRules.ShouldAddWound(0) == false);
Check("T5 横砍未完全格挡(实际生命伤害>0): 加伤口",
    BlackKnightRules.ShouldAddWound(1) && BlackKnightRules.ShouldAddWound(34));

// 测试 6：未使用幽冥诅咒的竖劈 —— 43 伤害、不可格挡、不治疗。
Check("T6 竖劈: VerticalSlashDamage == 43", BlackKnightRules.VerticalSlashDamage == 43);
Check("T6 竖劈(未打诅咒/不可格挡): 不治疗",
    BlackKnightRules.VerticalHeal(43, warded: false) == 0 &&
    BlackKnightRules.VerticalHeal(20, warded: false) == 0);

// 测试 7：使用幽冥诅咒后的竖劈 —— 变可格挡，按实际生命伤害治疗。
Check("T7 竖劈(已保护): 治疗 == 实际生命伤害",
    BlackKnightRules.VerticalHeal(43, warded: true) == 43);

// 测试 8：竖劈部分格挡 —— 只恢复实际造成的生命伤害。
Check("T8 竖劈部分格挡: 治疗 == 未被格挡的生命伤害",
    BlackKnightRules.VerticalHeal(15, warded: true) == 15);

// 测试 9：竖劈完全格挡 —— 恢复 0。
Check("T9 竖劈完全格挡: 治疗 == 0", BlackKnightRules.VerticalHeal(0, warded: true) == 0);

// 测试 10：怯懦 —— N 层影响接下来 N 张进入手牌的幽冥诅咒，每张消耗 1 层。
Check("T10 怯懦: 3 层 + 5 张诅咒 → 3 张获得消耗",
    BlackKnightRules.CowardiceExhaustCount(3, 5) == 3);
Check("T10 怯懦: 2 层 + 1 张诅咒 → 1 张获得消耗（每张最多 1 层）",
    BlackKnightRules.CowardiceExhaustCount(2, 1) == 1);
Check("T10 怯懦: 0 层 → 不消耗", BlackKnightRules.CowardiceExhaustCount(0, 5) == 0);

// 测试 11：暗鬼铠甲 —— N=3 时黑骑士 +150 格挡，玩家 +3 层怯懦。
Check("T11 暗鬼铠甲: DarkArmorBlock(3) == 150", BlackKnightRules.DarkArmorBlock(3) == 150);
Check("T11 暗鬼铠甲: CowardiceStacks(3) == 3", BlackKnightRules.CowardiceStacks(3) == 3);
Check("T11 暗鬼铠甲: DarkArmorBlock(1) == 50", BlackKnightRules.DarkArmorBlock(1) == 50);

// 测试 12：真身 —— +5 力量；后续顺序 横砍→横砍→竖劈；三次结束回到诅咒发放。
Check("T12 真身: TrueFormStrength == 5", BlackKnightRules.TrueFormStrength == 5);
Check("T12 真身: TrueFormActionCount == 3", BlackKnightRules.TrueFormActionCount == 3);
Check("T12 真身序列: 横砍 → 横砍 → 竖劈+",
    BlackKnightRules.TrueFormSequence.Length == 3 &&
    BlackKnightRules.IsHorizontalState(BlackKnightRules.TrueFormSequence[0]) &&
    BlackKnightRules.IsHorizontalState(BlackKnightRules.TrueFormSequence[1]) &&
    BlackKnightRules.IsVerticalState(BlackKnightRules.TrueFormSequence[2]));
Check("T12 真身结束后回到诅咒发放",
    BlackKnightRules.NextState("BK_TF_VERTICAL") == "BK_CURSE");

// 测试 13：多循环 —— 连续两整圈无错位。
{
    string s = "BK_CURSE";
    bool ok = true;
    for (int cycle = 0; cycle < 2 && ok; cycle++)
        foreach (var exp in expected)
        {
            if (s != exp) { ok = false; break; }
            s = BlackKnightRules.NextState(s);
        }
    Check("T13 多循环: 连续两整圈严格无错位", ok && s == "BK_CURSE");
}

// 测试 14：第一战覆盖 —— Debug 开时仅替换第一幕第一场普通战斗；关时不替换。
Check("T14 首战覆盖: Debug 开 + 第一幕 + 普通战斗 + 未替换过 → 替换",
    BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, true, false));
Check("T14 首战覆盖: Debug 关 → 不替换",
    !BlackKnightRules.ShouldReplaceFirstEncounter(false, 0, true, false));
Check("T14 首战覆盖: 非第一幕 → 不替换",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 1, true, false));
Check("T14 首战覆盖: 非普通战斗(精英/Boss) → 不替换",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, false, false));
Check("T14 首战覆盖: 本局已替换过 → 后续不再替换",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, true, true));

// 测试 15/16：存档恢复用的状态分类谓词（真身可见性 / 竖劈保护绑定的正确判定）。
Check("T15/16 谓词: IsTrueFormState 正确识别真身阶段的 4 个状态",
    BlackKnightRules.IsTrueFormState("BK_TRUE_FORM") &&
    BlackKnightRules.IsTrueFormState("BK_TF_HORIZONTAL_A") &&
    BlackKnightRules.IsTrueFormState("BK_TF_HORIZONTAL_B") &&
    BlackKnightRules.IsTrueFormState("BK_TF_VERTICAL") &&
    !BlackKnightRules.IsTrueFormState("BK_CURSE"));
Check("T15/16 谓词: IsVerticalState 仅识别两个竖劈状态",
    BlackKnightRules.IsVerticalState("BK_VERTICAL") &&
    BlackKnightRules.IsVerticalState("BK_TF_VERTICAL") &&
    !BlackKnightRules.IsVerticalState("BK_HORIZONTAL"));

Console.WriteLine($"\n=== 结果: {passed} 通过, {failed} 失败 ===");
return failed == 0 ? 0 : 1;
