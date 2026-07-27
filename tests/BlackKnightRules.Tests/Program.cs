using NinjaMod.NinjaModCode.Monsters;
using System.Text.Json;

int passed = 0;
int failed = 0;

void Check(string name, bool condition)
{
    Console.ForegroundColor = condition ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write(condition ? "PASS " : "FAIL ");
    Console.ResetColor();
    Console.WriteLine(name);
    if (condition) passed++; else failed++;
}

Console.WriteLine("=== Black Knight rule tests ===");

Check("Max HP is 500", BlackKnightRules.MaxHp == 500);

string[] expected =
{
    "BK_CURSE",
    "BK_DIAGONAL_A",
    "BK_DIAGONAL_B",
    "BK_HORIZONTAL",
    "BK_VERTICAL",
    "BK_DARK_ARMOR",
    "BK_TRUE_FORM",
    "BK_TF_HORIZONTAL_A",
    "BK_TF_HORIZONTAL_B",
    "BK_TF_VERTICAL",
};
Check(
    "Canonical move sequence",
    BlackKnightRules.CanonicalSequence.SequenceEqual(expected));
Check(
    "Move sequence loops back to curse cast",
    BlackKnightRules.NextState("BK_TF_VERTICAL") == "BK_CURSE");
Check(
    "Every canonical state has the expected follow-up",
    BlackKnightRules.CanonicalSequence
        .Select((state, index) =>
            BlackKnightRules.NextState(state)
            == BlackKnightRules.CanonicalSequence[(index + 1) % BlackKnightRules.CanonicalSequence.Length])
        .All(matches => matches));

string intentGraphPath = Path.Combine(AppContext.BaseDirectory, "intentgraph.json");
using (JsonDocument intentGraph = JsonDocument.Parse(File.ReadAllText(intentGraphPath)))
{
    JsonElement stateMachine = intentGraph.RootElement
        .GetProperty("NinjaMod.NinjaModCode.Monsters.BlackKnightEnemy")[0]
        .GetProperty("stateMachine");
    Dictionary<string, string> graphEdges = stateMachine
        .EnumerateArray()
        .ToDictionary(
            state => state.GetProperty("name").GetString()!,
            state => state.GetProperty("followUpState").GetString()!);

    Check(
        "Published intent graph matches every canonical state edge",
        graphEdges.Count == BlackKnightRules.CanonicalSequence.Length
        && BlackKnightRules.CanonicalSequence.All(state =>
            graphEdges.TryGetValue(state, out string? followUp)
            && followUp == BlackKnightRules.NextState(state)));
    Check(
        "Published intent graph keeps curse cast as the only initial state",
        stateMachine.EnumerateArray().Count(state =>
            state.TryGetProperty("isInitialState", out JsonElement initial)
            && initial.GetBoolean()) == 1
        && stateMachine[0].GetProperty("name").GetString() == BlackKnightRules.StateCurse);
}

Check("Diagonal Slash damage", BlackKnightRules.DiagonalSlashDamage == 25);
Check("Horizontal Slash damage", BlackKnightRules.HorizontalSlashDamage == 34);
Check("Vertical Slash damage", BlackKnightRules.VerticalSlashDamage == 43);

Check("Full block does not add Wound", !BlackKnightRules.ShouldAddWound(0));
Check("HP damage adds Wound", BlackKnightRules.ShouldAddWound(1));

Check(
    "No sigil means no healing",
    BlackKnightRules.LifeSiphonHeal(50, 0) == 0);
Check(
    "Sigil heals actual HP damage",
    BlackKnightRules.LifeSiphonHeal(20, 5) == 20);
Check(
    "Blocked damage cannot heal",
    BlackKnightRules.LifeSiphonHeal(0, 5) == 0);
Check(
    "Negative damage cannot heal",
    BlackKnightRules.LifeSiphonHeal(-10, 5) == 0);
Check(
    "One attack consumes exactly one sigil",
    BlackKnightRules.LifeSiphonStacksConsumed(10) == 1);
Check(
    "Zero sigils consumes nothing",
    BlackKnightRules.LifeSiphonStacksConsumed(0) == 0);
Check(
    "Curse package grants sigils for successful adds only",
    BlackKnightRules.LifeSiphonSigilsFromCurseAdds(7) == 7
    && BlackKnightRules.LifeSiphonSigilsFromCurseAdds(-1) == 0);

Check(
    "Nether Curse in hand makes Vertical Slash unblockable",
    BlackKnightRules.VerticalIsUnblockable(handContainsNetherCurse: true));
Check(
    "No Nether Curse leaves Vertical Slash blockable",
    !BlackKnightRules.VerticalIsUnblockable(handContainsNetherCurse: false));
Check(
    "Vertical intent turns unblockable from the live hand",
    BlackKnightRules.VerticalIntentIsUnblockable(
        handContainsNetherCurse: true,
        hasSavedExposure: false));
Check(
    "Vertical intent stays unblockable after the hand is flushed",
    BlackKnightRules.VerticalIntentIsUnblockable(
        handContainsNetherCurse: false,
        hasSavedExposure: true));
Check(
    "Vertical intent is blockable without a curse or saved exposure",
    !BlackKnightRules.VerticalIntentIsUnblockable(
        handContainsNetherCurse: false,
        hasSavedExposure: false));

Check(
    "Nether Erosion affects the first Nether Curse drawn into hand",
    BlackKnightRules.ShouldRemoveNetherCurseExhaust(false, true, true, true));
Check(
    "Nether Erosion triggers only once per turn",
    !BlackKnightRules.ShouldRemoveNetherCurseExhaust(true, true, true, true));
Check(
    "Nether Erosion ignores other cards",
    !BlackKnightRules.ShouldRemoveNetherCurseExhaust(false, false, true, true));
Check(
    "Nether Erosion requires an actual draw into hand",
    !BlackKnightRules.ShouldRemoveNetherCurseExhaust(false, true, false, true)
    && !BlackKnightRules.ShouldRemoveNetherCurseExhaust(false, true, true, false));

Check("Dark Armor multiplayer block", BlackKnightRules.DarkArmorBlock(3) == 150);
Check("True Form Strength", BlackKnightRules.TrueFormStrength == 5);
Check(
    "Curse package belongs to both initial and True Form states",
    BlackKnightRules.StateAppliesCursePackage(BlackKnightRules.StateCurse)
    && BlackKnightRules.StateAppliesCursePackage(BlackKnightRules.StateTrueForm)
    && !BlackKnightRules.StateAppliesCursePackage(BlackKnightRules.StateTrueFormHorizA));
Check(
    "True Form repeats the full five-card curse package",
    BlackKnightRules.TrueFormCurseCardCount == BlackKnightRules.CurseCardCount
    && BlackKnightRules.TrueFormCurseCardCount == 5);
Check(
    "True Form move sequence",
    BlackKnightRules.TrueFormSequence.Length == 3
    && BlackKnightRules.IsHorizontalState(BlackKnightRules.TrueFormSequence[0])
    && BlackKnightRules.IsHorizontalState(BlackKnightRules.TrueFormSequence[1])
    && BlackKnightRules.IsVerticalState(BlackKnightRules.TrueFormSequence[2]));

Check(
    "Black Knight is assigned only to Act 3",
    BlackKnightRules.IsActThree(2)
    && !BlackKnightRules.IsActThree(0)
    && !BlackKnightRules.IsActThree(1)
    && !BlackKnightRules.IsActThree(3));
Check(
    "A9 and below use Black Knight as the only boss",
    BlackKnightRules.IsBlackKnightFinalSlot(
        2,
        hasDoubleBoss: false,
        isSecondBossSlot: false)
    && !BlackKnightRules.IsBlackKnightFinalSlot(
        2,
        hasDoubleBoss: false,
        isSecondBossSlot: true));
Check(
    "A10 uses Black Knight as the second boss",
    BlackKnightRules.IsBlackKnightFinalSlot(
        2,
        hasDoubleBoss: true,
        isSecondBossSlot: true)
    && !BlackKnightRules.IsBlackKnightFinalSlot(
        2,
        hasDoubleBoss: true,
        isSecondBossSlot: false)
    && !BlackKnightRules.IsBlackKnightFinalSlot(
        1,
        hasDoubleBoss: true,
        isSecondBossSlot: true));
Check(
    "Old double-boss saves add a missing second boss map point",
    BlackKnightRules.ShouldAddSecondBossMapPoint(
        hasSecondBossEncounter: true,
        hasSecondBossMapPoint: false)
    && !BlackKnightRules.ShouldAddSecondBossMapPoint(
        hasSecondBossEncounter: true,
        hasSecondBossMapPoint: true)
    && !BlackKnightRules.ShouldAddSecondBossMapPoint(
        hasSecondBossEncounter: false,
        hasSecondBossMapPoint: false));
Check(
    "Second boss map point follows the first boss row",
    BlackKnightRules.SecondBossMapRow(16) == 17);
Check(
    "A9 old saves remove an extra second boss map point",
    BlackKnightRules.ShouldRemoveSecondBossMapPoint(
        hasDoubleBoss: false,
        hasSecondBossMapPoint: true)
    && !BlackKnightRules.ShouldRemoveSecondBossMapPoint(
        hasDoubleBoss: true,
        hasSecondBossMapPoint: true)
    && !BlackKnightRules.ShouldRemoveSecondBossMapPoint(
        hasDoubleBoss: false,
        hasSecondBossMapPoint: false));
Check(
    "Act 3 first boss continues to the pending Black Knight",
    BlackKnightRules.ShouldContinueToPendingSecondBoss(
        2,
        currentBossIsBlackKnight: false,
        secondBossIsBlackKnight: true));
Check(
    "Second-boss continuation does not loop or affect other acts",
    !BlackKnightRules.ShouldContinueToPendingSecondBoss(
        2,
        currentBossIsBlackKnight: true,
        secondBossIsBlackKnight: true)
    && !BlackKnightRules.ShouldContinueToPendingSecondBoss(
        2,
        currentBossIsBlackKnight: false,
        secondBossIsBlackKnight: false)
    && !BlackKnightRules.ShouldContinueToPendingSecondBoss(
        1,
        currentBossIsBlackKnight: false,
        secondBossIsBlackKnight: true));

Check(
    "Debug encounter replacement positive case",
    BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, true, false));
Check(
    "Debug encounter replacement disabled",
    !BlackKnightRules.ShouldReplaceFirstEncounter(false, 0, true, false));
Check(
    "Debug encounter replacement wrong act",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 1, true, false));
Check(
    "Debug encounter replacement only normal rooms",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, false, false));
Check(
    "Debug encounter replacement only once",
    !BlackKnightRules.ShouldReplaceFirstEncounter(true, 0, true, true));

Console.WriteLine($"=== Result: {passed} passed, {failed} failed ===");
return failed == 0 ? 0 : 1;
