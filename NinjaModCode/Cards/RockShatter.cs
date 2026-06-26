using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：碎石（Earth Ninjutsu: Rock Shatter）——技能牌，消耗。
/// 1 费，获得 8（升级 13）点格挡；自动免费打出手牌中所有“忍者防御”（各结算其格挡）；
/// 随后移除自身 1 层抵挡（Resist）。
/// </summary>
public class RockShatter : NinjaModCard
{
    public RockShatter() : base(BalanceCost(nameof(RockShatter), 1), BalanceType(nameof(RockShatter), CardType.Skill), BalanceRarity(nameof(RockShatter), CardRarity.Common), BalanceTarget(nameof(RockShatter), TargetType.Self)) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(BalanceDecimal("BaseBlock", 8m), ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1) 本牌自身的格挡。
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2) 自动免费打出手牌中的所有“忍者防御”：逐张按其实际格挡值结算并移到弃牌堆。
        var defends = CardPile.GetCards(Owner, new[] { PileType.Hand })
            .OfType<NinjaDefend>()
            .ToList();
        foreach (var defend in defends)
        {
            // Use the real card-play pipeline so Ninja Defend resolves as a free
            // player-played card and then follows its normal destination: Discard.
            await CardCmd.AutoPlay(choiceContext, defend, Owner.Creature);
        }

        // 3) 移除 1 层抵挡。
        var resist = Owner.Creature.GetPower<ResistPower>();
        if (resist != null)
        {
            for (int i = 0; i < BalanceValue("BaseRockShatterResistLoss", 1) && resist.Amount > 0; i++)
            {
                await PowerCmd.Decrement(resist);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(BalanceDelta("BaseBlock", "UpgradeBlock", 5m)); // 8 -> 13

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：碎石", "获得 {Block:diff()} 点格挡。自动免费打出手牌中所有[gold]忍者防御[/gold]，随后移除 1 层[gold]抵挡[/gold]。")
        : new CardLoc("Earth Ninjutsu: Rock Shatter", "Gain {Block:diff()} Block. Auto-play all [gold]Ninja Defends[/gold] in your hand, then remove 1 [gold]Resist[/gold].");
}
