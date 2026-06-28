using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 忍具整理（Ninja Tool Prep）——技能牌（普通）。
/// 0 费，抽 1 张牌；若抽到的是攻击牌，则获得 2（升级 4）点活力。
/// </summary>
public class NinjaToolPrep : NinjaModCard
{
    public NinjaToolPrep() : base(BalanceCost(nameof(NinjaToolPrep), 0), BalanceType(nameof(NinjaToolPrep), CardType.Skill), BalanceRarity(nameof(NinjaToolPrep), CardRarity.Common), BalanceTarget(nameof(NinjaToolPrep), TargetType.Self)) { }

    // 抽到攻击牌时获得的活力层数：2（升级 4）。用 IntVar 让卡面显示升级预览。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Vigor", BalanceValue("BaseVigor", 2))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? drawn = await CardPileCmd.Draw(choiceContext, Owner);
        if (drawn != null && drawn.Type == CardType.Attack)
        {
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, DynamicVars["Vigor"].IntValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Vigor"].UpgradeValueBy(BalanceDelta("BaseVigor", "UpgradeVigor", 2m)); // 2 -> 4

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("忍具整理", "抽 1 张牌。若抽到攻击牌，获得 {Vigor:diff()} 点[gold]活力[/gold]。")
        : new CardLoc("Ninja Tool Prep", "Draw 1 card. If it is an Attack, gain {Vigor:diff()} [gold]Vigor[/gold].");
}
