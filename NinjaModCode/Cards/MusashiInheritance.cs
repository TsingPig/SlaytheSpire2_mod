using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：承袭（Musashi: Inheritance）——技能牌，消耗。
/// 3（升级 2）费，将各一张【神速】【空明斩】【刺】加入手牌。下回合开始获得 3 点能量。
/// </summary>
public class MusashiInheritance : NinjaModCard
{
    public MusashiInheritance() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override bool IsMusashi => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 下回合开始获得 3 点能量。
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);
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
        ? new CardLoc("武藏：承袭", "将各一张【神速】【空明斩】【刺】加入手牌。下回合开始获得 3 点能量。")
        : new CardLoc("Musashi: Inheritance", "Add a Godspeed, a Void Slash, and a Thrust to your hand. Gain 3 Energy next turn.");
}
