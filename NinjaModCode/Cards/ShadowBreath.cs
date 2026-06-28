using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaMod.NinjaModCode.Character;
using NinjaMod.NinjaModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using BaseLib.Abstracts;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 影息术（Shadow Breath）——技能牌（稀有）。
/// 1 费，若你处于【隐身】状态，抽 1（升级 2）张牌并获得 1 点能量。
/// </summary>
public class ShadowBreath : NinjaModCard
{
    public ShadowBreath() : base(BalanceCost(nameof(ShadowBreath), 1), BalanceType(nameof(ShadowBreath), CardType.Skill), BalanceRarity(nameof(ShadowBreath), CardRarity.Rare), BalanceTarget(nameof(ShadowBreath), TargetType.Self)) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(BalanceValue("BaseCards", 1))];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool stealthed = Owner.Creature.GetPower<StealthPower>() is { Amount: > 0 };
        if (!stealthed) return;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, false);
        await PlayerCmd.GainEnergy(BalanceValue("BaseEnergy", 1), Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(BalanceDelta("BaseCards", "UpgradeCards", 1m)); // 1 -> 2

    public override List<(string, string)>? Localization => Lang.Zh
        ? new CardLoc("影息术", "若你处于[gold]隐身[/gold]状态，抽 {Cards:diff()} 张牌并获得 1 点能量。")
        : new CardLoc("Shadow Breath", "If you are [gold]Stealthed[/gold], draw {Cards:diff()} cards and gain 1 Energy.");
}
