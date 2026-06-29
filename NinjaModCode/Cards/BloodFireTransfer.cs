using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 火忍：血火转印（Fire Ninjutsu: Blood-Fire Transfer）——技能牌（稀有），消耗。
/// 2 费，给予目标等同于其当前【燃烧】层数的【流血】，并移除目标身上的【燃烧】。升级后移除消耗。
/// </summary>
public class BloodFireTransfer : NinjaModCard
{
    public BloodFireTransfer() : base(BalanceCost(nameof(BloodFireTransfer), 2), BalanceType(nameof(BloodFireTransfer), CardType.Skill), BalanceRarity(nameof(BloodFireTransfer), CardRarity.Rare), BalanceTarget(nameof(BloodFireTransfer), TargetType.AnyEnemy)) { }

    // 基础：消耗；升级后移除消耗（随 IsUpgraded 实时变化）。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var burning = cardPlay.Target.GetPower<BurningPower>();
        int amount = burning?.Amount ?? 0;
        if (amount <= 0) return;

        await PowerCmd.Apply<BleedPower>(choiceContext, cardPlay.Target, amount, Owner.Creature, this);
        await PowerCmd.Remove(burning!);
    }

    protected override void OnUpgrade() { } // 升级仅移除消耗（由 CanonicalKeywords 处理）

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：血火转印", "对目标施加等同于其当前[gold]燃烧[/gold]层数的[gold]流血[/gold]，然后移除其[gold]燃烧[/gold]。")
        : new CardLoc("Fire Ninjutsu: Blood-Fire Transfer", "Apply [gold]Bleed[/gold] equal to the target's [gold]Burning[/gold], then remove its [gold]Burning[/gold].");
}
