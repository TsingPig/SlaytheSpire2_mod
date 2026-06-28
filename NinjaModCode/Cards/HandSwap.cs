using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 换手（Hand Swap）——技能牌（普通）。
/// 1 费，从手牌中选择最多 2 张牌放回抽牌堆顶部，获得 4（升级 7）点格挡。
/// </summary>
public class HandSwap : NinjaModCard
{
    // 一次最多放回的张数（常量）。
    private int MaxCards => BalanceConst(nameof(HandSwap), nameof(MaxCards), 2);

    public HandSwap() : base(BalanceCost(nameof(HandSwap), 1), BalanceType(nameof(HandSwap), CardType.Skill), BalanceRarity(nameof(HandSwap), CardRarity.Common), BalanceTarget(nameof(HandSwap), TargetType.Self)) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BalanceDecimal("BaseBlock", 4m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = CardPile.GetCards(Owner, new[] { PileType.Hand }).ToList();
        if (hand.Count > 0)
        {
            int maxSelect = System.Math.Min(MaxCards, hand.Count);
            var prompt = new LocString("card_selection", "NINJAMOD_HANDSWAP_PROMPT");
            var prefs = new CardSelectorPrefs(prompt, 0, maxSelect)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
            };
            var selected = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, null!)).ToList();
            foreach (var card in selected)
            {
                await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top, this, false);
            }
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 3m)); // 4 -> 7

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("换手", "从手牌中选择最多 2 张牌放回抽牌堆顶部，获得 {Block:diff()} 点格挡。")
        : new CardLoc("Hand Swap", "Put up to 2 cards from your hand on top of your draw pile. Gain {Block:diff()} Block.");
}
