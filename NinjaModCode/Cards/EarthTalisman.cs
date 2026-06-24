using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 土忍：土护符（Earth Ninjutsu: Earth Guard Talisman）——技能牌，消耗。
/// 1（升级 0）费，获得等同于当前消耗牌堆中牌数量的格挡。
/// </summary>
public class EarthTalisman : NinjaModCard
{
    public EarthTalisman() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 动态格挡 = 当前消耗牌堆中的牌数量。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        MakeCalculatedBlock(0, (card, creature) =>
        {
            // 卡面预览时 card.Owner 可能为空，用 CombatState 的玩家可靠读取消耗牌堆数量。
            var players = card.CombatState?.Players;
            var owner = (players != null && players.Count > 0) ? players[0] : card.Owner;
            if (owner == null) return 0m;
            return CardPile.GetCards(owner, new[] { PileType.Exhaust }).Count();
        }, 0, ValueProp.Move);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = CardPile.GetCards(Owner, new[] { PileType.Exhaust }).Count();
        if (count > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, count, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 1 -> 0

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("土忍：土护符", "获得 {CalculatedBlock:diff()} 点格挡（等同于当前消耗牌堆中的牌数）。")
        : new CardLoc("Earth Ninjutsu: Earth Guard Talisman", "Gain {CalculatedBlock:diff()} Block (equal to the number of cards in your exhaust pile).");
}
