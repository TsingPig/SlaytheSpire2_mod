using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 武藏：无双（Musashi: Peerless）——技能牌（稀有），消耗。
/// 3（升级 2）费，下回合开始获得 2 点能量，将各一张【武藏：二天一流】【武藏：猩红】【武藏：刺】放入抽牌堆顶部。
/// </summary>
public class MusashiPeerless : NinjaModCard
{
    // 下回合开始获得的能量（常量）。
    private int Energy => BalanceConst(nameof(MusashiPeerless), nameof(Energy), 2);

    public MusashiPeerless() : base(BalanceCost(nameof(MusashiPeerless), 3), BalanceType(nameof(MusashiPeerless), CardType.Skill), BalanceRarity(nameof(MusashiPeerless), CardRarity.Rare), BalanceTarget(nameof(MusashiPeerless), TargetType.Self)) { }

    public override bool IsMusashi => BalanceIsMusashi(nameof(MusashiPeerless), true);

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 悬浮提示：展示将放入抽牌堆顶部的三张牌（二天一流 / 猩红 / 刺）的卡牌预览。
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>();
            var baseTips = base.ExtraHoverTips;
            if (baseTips != null) tips.AddRange(baseTips);
            AddCardTip<MusashiTwoHeavens>(tips);
            AddCardTip<MusashiCrimson>(tips);
            AddCardTip<MusashiThrust>(tips);
            return tips;
        }
    }

    private static void AddCardTip<T>(List<IHoverTip> tips) where T : NinjaModCard
    {
        var card = ModelDb.Card<T>();
        if (card != null) tips.Add(new CardHoverTip(card));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, Energy, Owner.Creature, this);
        await AddToDrawTop<MusashiTwoHeavens>();
        await AddToDrawTop<MusashiCrimson>();
        await AddToDrawTop<MusashiThrust>();
    }

    private async Task AddToDrawTop<T>() where T : NinjaModCard
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        var card = CombatState.CreateCard<T>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, Owner, CardPilePosition.Top);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(BalanceUpgradeCostDelta(nameof(MusashiPeerless), -1)); // 3 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("武藏：无双", "下回合开始获得 2 点能量，将各一张【武藏：二天一流】【武藏：猩红】【武藏：刺】放入抽牌堆顶部。")
        : new CardLoc("Musashi: Peerless", "Gain 2 Energy next turn. Put a Two Heavens, a Crimson, and a Thrust on top of your draw pile.");
}
