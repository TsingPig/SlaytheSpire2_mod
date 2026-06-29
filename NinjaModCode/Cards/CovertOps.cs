using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 保密行动（Covert Ops）——技能牌（稀有）。
/// 2 费，获得 1（升级 2）层【隐身】，从抽牌堆中选择 2（升级 3）张牌，给其追加【静默】。
/// </summary>
public class CovertOps : NinjaModCard
{
    public CovertOps() : base(BalanceCost(nameof(CovertOps), 2), BalanceType(nameof(CovertOps), CardType.Skill), BalanceRarity(nameof(CovertOps), CardRarity.Rare), BalanceTarget(nameof(CovertOps), TargetType.Self)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Stealth", BalanceValue("BaseStealth", 1)), new IntVar("Select", BalanceValue("BaseSelect", 2))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StealthPower>(choiceContext, Owner.Creature, DynamicVars["Stealth"].IntValue, Owner.Creature, this);

        var drawPile = CardPile.Get(PileType.Draw, Owner);
        if (drawPile == null) return;
        if (drawPile.IsEmpty) return;

        var selectable = drawPile.Cards
            .OfType<NinjaModCard>()
            .Where(card => !card.HasSilence)
            .ToList();
        if (selectable.Count == 0) return;

        int maxSelect = System.Math.Min(DynamicVars["Select"].IntValue, selectable.Count);
        if (maxSelect <= 0) return;

        var prompt = new LocString("card_selection", "NINJAMOD_COVERTOPS_PROMPT");
        var prefs = new CardSelectorPrefs(prompt, 0, maxSelect)
        {
            RequireManualConfirmation = true,
            Cancelable = true,
        };

        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, selectable, Owner, prefs)).ToList();
        foreach (var card in selected.OfType<NinjaModCard>())
        {
            card.GrantedSilence = true;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Stealth"].UpgradeValueBy(BalanceDelta("BaseStealth", "UpgradeStealth", 1m)); // 1 -> 2
        DynamicVars["Select"].UpgradeValueBy(BalanceDelta("BaseSelect", "UpgradeSelect", 1m));     // 2 -> 3
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("保密行动", "获得 {Stealth:diff()} 层[gold]隐身[/gold]。从抽牌堆中选择最多 {Select:diff()} 张牌，给予它们[gold]静默[/gold]。")
        : new CardLoc("Covert Ops", "Gain {Stealth:diff()} [gold]Stealth[/gold]. Choose up to {Select:diff()} cards from your draw pile and grant them [gold]Silence[/gold].");
}
