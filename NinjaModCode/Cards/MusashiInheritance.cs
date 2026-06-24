using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：承袭（Musashi: Inheritance）——技能牌，消耗。
/// 3（升级 2）费，获得 13 点格挡，将各一张【神速】【空明斩】【刺】加入手牌。
/// </summary>
public class MusashiInheritance : NinjaModCard
{
    public MusashiInheritance() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool IsMusashi => true;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(13m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await AddToHand<MusashiGodspeed>();
        await AddToHand<MusashiVoidSlash>();
        await AddToHand<MusashiThrust>();
    }

    private async Task AddToHand<T>() where T : NinjaModCard
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var card = CombatState.CreateCard<T>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1); // 3 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：承袭", "获得 {Block:diff()} 点格挡，将各一张【神速】【空明斩】【刺】加入手牌。")
        : new CardLoc("Musashi: Inheritance", "Gain {Block:diff()} Block. Add a Godspeed, a Void Slash, and a Thrust to your hand.");
}
