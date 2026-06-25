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
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：燃心（Fire Ninjutsu: Burning Heart）——技能牌，X 费。
/// 消耗抽牌堆顶部最多 X 张牌（实际 K 张），给予所有敌人 K × 3 层燃烧。升级后获得【保留】。
/// </summary>
public class BurningHeart : NinjaModCard
{
    // 每消耗一张牌给予的燃烧层数。
    private const int BurningPerCard = 3;

    public BurningHeart() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override bool HasEnergyCostX => true;

    // 升级后获得保留：随 IsUpgraded 实时变化。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [CardKeyword.Retain] : [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0) return;

        // 进入抽牌堆选牌界面：玩家可选择消耗最多 X 张牌（也可以选择更少）。
        var drawPile = CardPile.Get(PileType.Draw, Owner);
        if (drawPile.IsEmpty) return;

        var prefs = new CardSelectorPrefs(
            new LocString("NINJAMOD_BURNING_HEART_EXHAUST",
                Lang.Zh ? "选择要消耗的牌（最多 X 张）" : "Choose cards to exhaust (up to X)"),
            0, x);

        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, drawPile, Owner, prefs)).ToList();
        int k = selected.Count;
        foreach (var card in selected)
        {
            await CardPileCmd.RemoveFromCombat(card, true);
        }

        if (k <= 0) return;
        int burning = k * BurningPerCard;
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<BurningPower>(choiceContext, enemy, burning, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() { } // 升级仅获得保留（由 CanonicalKeywords 处理）

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：燃心", "进入抽牌堆，消耗最多 X 张牌（K 张），给予所有敌人 K × 3 层[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Burning Heart", "Choose up to X cards from your draw pile to Exhaust (K cards). Apply K x 3 [gold]Burning[/gold] to ALL enemies.");
}
