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
/// 火忍：起爆符（Fire Ninjutsu: Detonation Talisman）——技能牌。
/// 0 费，点燃目标的燃烧（造成燃烧 2 倍的无法格挡伤害并移除）。升级后获得【保留】。
/// </summary>
public class Detonation : NinjaModCard
{
    public Detonation() : base(BalanceCost(nameof(Detonation), 0), BalanceType(nameof(Detonation), CardType.Skill), BalanceRarity(nameof(Detonation), CardRarity.Common), BalanceTarget(nameof(Detonation), TargetType.AnyEnemy)) { }

    // 升级后获得保留：随 IsUpgraded 实时变化（卡面关键词行自动出现）。
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [CardKeyword.Retain] : [];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var burning = cardPlay.Target.GetPower<BurningPower>();
        if (burning != null && burning.Amount > 0)
        {
            await burning.IgniteAsync(choiceContext, Owner.Creature);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级获得保留。CanonicalKeywords 已用 IsUpgraded 控制，但还需要
        // AddKeyword 触发 KeywordsChanged 事件让卡面 UI 刷新关键词行。
        AddKeyword(CardKeyword.Retain);
    }

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("火忍：起爆符", "点燃目标的[gold]燃烧[/gold]（造成燃烧 2 倍的无法格挡伤害并移除）。")
        : new CardLoc("Fire Ninjutsu: Detonation Talisman", "Ignite the target's [gold]Burning[/gold] (deal twice Burning as unblockable damage, then remove).");
}
