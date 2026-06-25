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

        var drawPile = CardPile.Get(PileType.Draw, Owner);
        if (drawPile.IsEmpty) return;
        var drawCards = drawPile.Cards.ToList();
        int maxSelect = System.Math.Min(x, drawCards.Count);
        if (maxSelect <= 0) return;

        // 这里不用 FromCombatPile：当前抽牌堆 pile screen 对“可少选/0 到 X 张”的显示路径不稳定，
        // 会出现只有 Info text / 不能选牌 / 无法退出的软锁。FromSimpleGrid 使用同一批抽牌堆快照，
        // 确认按钮在 MinSelect = 0 时可用；选完后再对原牌执行原生 Exhaust。
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, maxSelect);

        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, drawCards, Owner, prefs)).ToList();
        int k = 0;
        foreach (var card in selected)
        {
            if (card.Pile == drawPile)
            {
                await CardCmd.Exhaust(choiceContext, card);
                k++;
            }
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
